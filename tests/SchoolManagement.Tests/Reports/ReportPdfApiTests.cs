using System.Net;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Fees;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Tests.Common;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests.Reports;

public sealed class ReportPdfApiTests : IClassFixture<SchoolManagementApiFactory>
{
    private readonly SchoolManagementApiFactory _factory;

    public ReportPdfApiTests(SchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_CanDownloadStudentsPdf()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        await adminClient.CreateStudentAsync(BuildStudentRequest("reports.students@school.com", "RP-100"));

        var response = await adminClient.GetAsync("/api/reports/students/pdf");

        await AssertPdfResponseAsync(response);
    }

    [Fact]
    public async Task Admin_CanDownloadAttendancePdf()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync("/api/reports/attendance/pdf");

        await AssertPdfResponseAsync(response);
    }

    [Fact]
    public async Task Admin_CanDownloadFeesPdf()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(BuildStudentRequest("reports.fees@school.com", "RP-101"));
        await adminClient.CreateFeeAsync(
            new CreateFeeRequest(student.Id, "Tuition", 250m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), FeeStatus.Pending));

        var response = await adminClient.GetAsync("/api/reports/fees/pdf");

        await AssertPdfResponseAsync(response);
    }

    [Fact]
    public async Task UnsupportedReportType_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync("/api/reports/unknown/pdf");

        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var payload = await response.ReadApiResponseAsync<object>();
        Assert.Equal("Unsupported report type.", payload.Message);
    }

    [Fact]
    public async Task InvalidReportDateRange_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync("/api/reports/fees/pdf?dateFrom=2026-01-10&dateTo=2026-01-05");

        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var payload = await response.ReadApiResponseAsync<object>();
        Assert.Equal("Validation failed", payload.Message);
        Assert.Contains(string.Join(" ", payload.Errors!.SelectMany(x => x.Value)), "DateFrom cannot be later than DateTo.");
    }

    [Fact]
    public async Task UnauthenticatedReportRequest_ReturnsUnauthorized()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/reports/students/pdf");

        await response.AssertStatusCodeAsync(HttpStatusCode.Unauthorized);
        var payload = await response.ReadApiResponseAsync<object>();
        Assert.Equal("Authentication is required to access this resource.", payload.Message);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
    }

    private static async Task AssertPdfResponseAsync(HttpResponseMessage response)
    {
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 4);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    private static SchoolManagement.Application.Students.CreateStudentRequest BuildStudentRequest(string email, string studentCode)
        => new(
            "Report Student",
            email,
            "Student@123",
            null,
            null,
            studentCode,
            new DateOnly(2011, 3, 3),
            Gender.Male,
            new DateOnly(2024, 9, 1),
            null,
            null);
}
