using System.Net;
using System.Net.Http.Json;
using SchoolManagement.Application.Fees;
using SchoolManagement.Application.Parents;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Tests.Common;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests.Fees;

public sealed class FeePaymentApiTests : IClassFixture<SchoolManagementApiFactory>
{
    private readonly SchoolManagementApiFactory _factory;

    public FeePaymentApiTests(SchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_CanCreateFee()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(BuildStudentRequest("fees.create@school.com", "ST-100"));

        var fee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(student.Id, "Tuition", 250m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), FeeStatus.Pending));

        Assert.Equal("Tuition", fee.FeeType);
        Assert.Equal("Pending", fee.Status);
    }

    [Fact]
    public async Task Payment_CanBeAddedToFee()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(BuildStudentRequest("fees.payment@school.com", "ST-101"));
        var fee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(student.Id, "Transport", 120m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), FeeStatus.Pending));

        var payment = await adminClient.AddPaymentAsync(
            new CreatePaymentRequest(fee.Id, 50m, DateTime.UtcNow, PaymentMethod.Card, "PAY-101"));

        Assert.Equal(50m, payment.AmountPaid);
    }

    [Fact]
    public async Task FeeStatus_ChangesCorrectlyAfterPayment()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(BuildStudentRequest("fees.status@school.com", "ST-102"));
        var fee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(student.Id, "Exam", 200m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), FeeStatus.Pending));

        await adminClient.AddPaymentAsync(new CreatePaymentRequest(fee.Id, 75m, DateTime.UtcNow, PaymentMethod.Cash, "PAY-102A"));
        var partialResponse = await adminClient.GetAsync($"/api/fees/{fee.Id}");
        partialResponse.EnsureSuccessStatusCode();
        var partialFee = await partialResponse.ReadApiResponseAsync<FeeResponse>();
        Assert.Equal("PartiallyPaid", partialFee.Data!.Status);

        await adminClient.AddPaymentAsync(new CreatePaymentRequest(fee.Id, 125m, DateTime.UtcNow, PaymentMethod.BankTransfer, "PAY-102B"));
        var paidResponse = await adminClient.GetAsync($"/api/fees/{fee.Id}");
        paidResponse.EnsureSuccessStatusCode();
        var paidFee = await paidResponse.ReadApiResponseAsync<FeeResponse>();
        Assert.Equal("Paid", paidFee.Data!.Status);
    }

    [Fact]
    public async Task Overpayment_IsRejected()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(BuildStudentRequest("fees.overpay@school.com", "ST-103"));
        var fee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(student.Id, "Library", 90m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), FeeStatus.Pending));

        var response = await adminClient.PostAsJsonAsync(
            "/api/payments",
            new CreatePaymentRequest(fee.Id, 100m, DateTime.UtcNow, PaymentMethod.Online, "PAY-103"));

        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeletingFeeWithPayments_IsRejected()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(BuildStudentRequest("fees.delete@school.com", "ST-104"));
        var fee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(student.Id, "Hostel", 300m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), FeeStatus.Pending));
        await adminClient.AddPaymentAsync(new CreatePaymentRequest(fee.Id, 50m, DateTime.UtcNow, PaymentMethod.Cash, "PAY-104"));

        var response = await adminClient.DeleteAsync($"/api/fees/{fee.Id}");

        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Parent_CanOnlyAccessFeeDataForOwnChild()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var parent = await adminClient.CreateParentAsync(
            new CreateParentRequest("Linked Parent", "linked.parent@school.com", "Parent@123", "12345", "Street 1", "Engineer"));
        var otherParent = await adminClient.CreateParentAsync(
            new CreateParentRequest("Other Parent", "other.parent@school.com", "Parent@123", "67890", "Street 2", "Doctor"));

        var ownChild = await adminClient.CreateStudentAsync(BuildStudentRequest("own.child@school.com", "ST-105", parent.Id));
        var otherChild = await adminClient.CreateStudentAsync(BuildStudentRequest("other.child@school.com", "ST-106", otherParent.Id));

        var ownFee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(ownChild.Id, "Meal", 70m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), FeeStatus.Pending));
        var otherFee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(otherChild.Id, "Sports", 95m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), FeeStatus.Pending));

        using var parentClient = await _factory.CreateAuthenticatedClientAsync("linked.parent@school.com", "Parent@123");

        var ownListResponse = await parentClient.GetAsync($"/api/fees/student/{ownChild.Id}");
        ownListResponse.EnsureSuccessStatusCode();

        var ownFeeResponse = await parentClient.GetAsync($"/api/fees/{ownFee.Id}");
        ownFeeResponse.EnsureSuccessStatusCode();

        var forbiddenListResponse = await parentClient.GetAsync($"/api/fees/student/{otherChild.Id}");
        await forbiddenListResponse.AssertStatusCodeAsync(HttpStatusCode.Forbidden);

        var forbiddenFeeResponse = await parentClient.GetAsync($"/api/fees/{otherFee.Id}");
        await forbiddenFeeResponse.AssertStatusCodeAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnauthorizedFeeAccess_ReturnsUnauthorized()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/fees");

        await response.AssertStatusCodeAsync(HttpStatusCode.Unauthorized);
    }

    private static SchoolManagement.Application.Students.CreateStudentRequest BuildStudentRequest(string email, string studentCode, Guid? parentId = null)
        => new(
            "Fee Student",
            email,
            "Student@123",
            null,
            null,
            studentCode,
            new DateOnly(2011, 2, 2),
            Gender.Male,
            new DateOnly(2024, 9, 1),
            parentId,
            null);
}
