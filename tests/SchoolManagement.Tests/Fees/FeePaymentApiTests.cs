using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common.Models;
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
    public async Task Payment_WithIdempotencyKey_CreatesPayment()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(BuildStudentRequest("fees.idempotent.create@school.com", "ST-IDEM-100"));
        var fee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(student.Id, "Technology", 100m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), FeeStatus.Pending));
        var paymentDate = DateTime.UtcNow;

        var payment = await adminClient.AddPaymentAsync(
            new CreatePaymentRequest(fee.Id, 40m, paymentDate, PaymentMethod.Card, "PAY-IDEM-100", "idem-create-100"));

        Assert.Equal(40m, payment.AmountPaid);
        Assert.Equal("PAY-IDEM-100", payment.TransactionReference);
        Assert.Equal("idem-create-100", payment.IdempotencyKey);

        var updatedFee = await GetFeeAsync(adminClient, fee.Id);
        Assert.Equal("PartiallyPaid", updatedFee.Status);
        Assert.Equal(40m, Assert.Single(updatedFee.Payments).AmountPaid);
    }

    [Fact]
    public async Task Payment_RetryWithSameIdempotencyKey_ReturnsExistingPaymentWithoutDuplicating()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(BuildStudentRequest("fees.idempotent.retry@school.com", "ST-IDEM-101"));
        var fee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(student.Id, "Tuition Retry", 100m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), FeeStatus.Pending));
        var request = new CreatePaymentRequest(fee.Id, 50m, DateTime.UtcNow, PaymentMethod.Online, "PAY-IDEM-101", "idem-retry-101");

        var first = await adminClient.AddPaymentAsync(request);
        var second = await adminClient.AddPaymentAsync(request);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.AmountPaid, second.AmountPaid);

        var storedPayments = await _factory.ExecuteDbContextAsync(async db =>
            await db.Payments.Where(x => x.FeeId == fee.Id).ToListAsync());
        var storedPayment = Assert.Single(storedPayments);
        Assert.Equal("idem-retry-101", storedPayment.IdempotencyKey);

        var updatedFee = await GetFeeAsync(adminClient, fee.Id);
        Assert.Equal("PartiallyPaid", updatedFee.Status);
        Assert.Equal(50m, updatedFee.Payments.Sum(x => x.AmountPaid));
    }

    [Fact]
    public async Task Payment_SameIdempotencyKeyWithDifferentAmount_ReturnsConflictAndKeepsOriginalPayment()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(BuildStudentRequest("fees.idempotent.conflict.amount@school.com", "ST-IDEM-102"));
        var fee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(student.Id, "Books", 100m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), FeeStatus.Pending));
        var paymentDate = DateTime.UtcNow;

        var first = await adminClient.AddPaymentAsync(
            new CreatePaymentRequest(fee.Id, 40m, paymentDate, PaymentMethod.Card, "PAY-IDEM-102", "idem-conflict-amount"));
        var conflict = await adminClient.PostAsJsonAsync(
            "/api/payments",
            new CreatePaymentRequest(fee.Id, 45m, paymentDate, PaymentMethod.Card, "PAY-IDEM-102", "idem-conflict-amount"));

        await conflict.AssertStatusCodeAsync(HttpStatusCode.Conflict);
        var payload = await conflict.ReadApiResponseAsync<object>();
        Assert.False(payload.Success);
        Assert.Equal("Idempotency key is already associated with a different payment request.", payload.Message);

        var payments = await _factory.ExecuteDbContextAsync(async db =>
            await db.Payments.Where(x => x.FeeId == fee.Id).ToListAsync());
        Assert.Equal(first.Id, Assert.Single(payments).Id);
    }

    [Fact]
    public async Task Payment_SameIdempotencyKeyForDifferentFee_ReturnsConflict()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var firstStudent = await adminClient.CreateStudentAsync(BuildStudentRequest("fees.idempotent.scope.1@school.com", "ST-IDEM-103"));
        var secondStudent = await adminClient.CreateStudentAsync(BuildStudentRequest("fees.idempotent.scope.2@school.com", "ST-IDEM-104"));
        var firstFee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(firstStudent.Id, "Scope One", 100m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), FeeStatus.Pending));
        var secondFee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(secondStudent.Id, "Scope Two", 100m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), FeeStatus.Pending));
        var paymentDate = DateTime.UtcNow;

        await adminClient.AddPaymentAsync(
            new CreatePaymentRequest(firstFee.Id, 30m, paymentDate, PaymentMethod.Cash, "PAY-IDEM-103", "idem-global-scope"));
        var conflict = await adminClient.PostAsJsonAsync(
            "/api/payments",
            new CreatePaymentRequest(secondFee.Id, 30m, paymentDate, PaymentMethod.Cash, "PAY-IDEM-104", "idem-global-scope"));

        await conflict.AssertStatusCodeAsync(HttpStatusCode.Conflict);
        var paymentCount = await _factory.ExecuteDbContextAsync(async db =>
            await db.Payments.CountAsync(x => x.IdempotencyKey == "idem-global-scope"));
        Assert.Equal(1, paymentCount);
    }

    [Fact]
    public async Task Payment_MissingIdempotencyKey_RemainsSupportedForLegacyClients()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(BuildStudentRequest("fees.idempotent.legacy@school.com", "ST-IDEM-105"));
        var fee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(student.Id, "Legacy", 100m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), FeeStatus.Pending));

        var payment = await adminClient.AddPaymentAsync(
            new CreatePaymentRequest(fee.Id, 25m, DateTime.UtcNow, PaymentMethod.BankTransfer, "PAY-IDEM-105"));

        Assert.Equal(25m, payment.AmountPaid);
        Assert.Null(payment.IdempotencyKey);
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

    private static async Task<FeeResponse> GetFeeAsync(HttpClient client, Guid feeId)
    {
        var response = await client.GetAsync($"/api/fees/{feeId}");
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<FeeResponse>();
        return payload.Data ?? throw new InvalidOperationException("Fee response did not include data.");
    }
}
