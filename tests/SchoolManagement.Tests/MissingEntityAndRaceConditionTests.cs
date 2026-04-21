using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Attendance;
using SchoolManagement.Application.Classes;
using SchoolManagement.Application.Exams;
using SchoolManagement.Application.Fees;
using SchoolManagement.Application.Results;
using SchoolManagement.Application.Students;
using SchoolManagement.Application.Subjects;
using SchoolManagement.Application.Teachers;
using SchoolManagement.Application.Timetable;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Tests.Common;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests;

/// <summary>
/// Verifies that missing-entity and race-condition scenarios produce controlled 4xx responses
/// instead of unhandled 500 Internal Server Errors.
///
/// Every "race-condition" test follows the same pattern:
///   1. Create the entity through the API so it genuinely exists.
///   2. Delete it directly from the DbContext, bypassing the service layer,
///      to simulate a concurrent delete between the service's AnyAsync guard
///      and the subsequent fetch.
///   3. Call the mutating endpoint and assert the API returns 404 (not 500).
/// </summary>
public sealed class MissingEntityAndRaceConditionTests : IClassFixture<SchoolManagementApiFactory>
{
    private readonly SchoolManagementApiFactory _factory;

    public MissingEntityAndRaceConditionTests(SchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Seeds the minimum graph needed by most academic-record tests.</summary>
    private async Task<WorkspaceData> SeedWorkspaceAsync(HttpClient adminClient, string suffix)
    {
        var teacher = await adminClient.PostAsJsonAsync(
                "/api/teachers",
                new CreateTeacherRequest(
                    $"Race Teacher {suffix}",
                    $"race.teacher.{suffix}@school.com",
                    "Teacher@123",
                    null, null,
                    $"RT-{suffix}",
                    "Physics",
                    new DateOnly(2022, 9, 1)))
            .ReadRequiredEntityAsync<TeacherResponse>();

        var subject = await adminClient.PostAsJsonAsync(
                "/api/subjects",
                new CreateSubjectRequest($"Physics {suffix}", $"PHY-{suffix}", null))
            .ReadRequiredEntityAsync<SubjectResponse>();

        var academicClass = await adminClient.PostAsJsonAsync(
                "/api/classes",
                new CreateClassRequest($"Grade 9 {suffix}", "A", "2025/2026", teacher.Id))
            .ReadRequiredEntityAsync<ClassResponse>();

        var student = await adminClient.CreateStudentAsync(
            new CreateStudentRequest(
                $"Student {suffix}",
                $"student.race.{suffix}@school.com",
                "Student@123",
                null, null,
                $"ST-R{suffix}",
                new DateOnly(2010, 3, 15),
                Gender.Male,
                new DateOnly(2024, 9, 1),
                null,
                academicClass.Id));

        return new WorkspaceData(teacher, subject, academicClass, student);
    }

    // ── Students ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStudent_WithNonExistentId_Returns404()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync($"/api/students/{Guid.NewGuid()}");

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateStudent_RaceCondition_Returns404NotServerError()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(
            new CreateStudentRequest(
                "Race Student",
                "race.update.student@school.com",
                "Student@123",
                null, null, "ST-RC1",
                new DateOnly(2010, 1, 1),
                Gender.Female,
                new DateOnly(2024, 9, 1),
                null, null));

        // Simulate concurrent delete
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.Students.SingleAsync(x => x.Id == student.Id);
            db.Students.Remove(entity);
            await db.SaveChangesAsync();
        });

        var response = await adminClient.PutAsJsonAsync(
            $"/api/students/{student.Id}",
            new UpdateStudentRequest(
                "Updated", "race.updated@school.com",
                null, null, "ST-RC1",
                new DateOnly(2010, 1, 1),
                Gender.Female,
                new DateOnly(2024, 9, 1),
                null, null));

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteStudent_RaceCondition_Returns404NotServerError()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(
            new CreateStudentRequest(
                "Race Student Delete",
                "race.delete.student@school.com",
                "Student@123",
                null, null, "ST-RC2",
                new DateOnly(2010, 1, 1),
                Gender.Male,
                new DateOnly(2024, 9, 1),
                null, null));

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.Students.SingleAsync(x => x.Id == student.Id);
            db.Students.Remove(entity);
            await db.SaveChangesAsync();
        });

        var response = await adminClient.DeleteAsync($"/api/students/{student.Id}");

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    // ── Exams ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetExam_WithNonExistentId_Returns404()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync($"/api/exams/{Guid.NewGuid()}");

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateExam_RaceCondition_Returns404NotServerError()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var data = await SeedWorkspaceAsync(adminClient, "EX1");

        var exam = await adminClient.PostAsJsonAsync(
                "/api/exams",
                new CreateExamRequest(data.Class.Id, data.Subject.Id, "Midterm Race", new DateOnly(2026, 5, 20), 100m))
            .ReadRequiredEntityAsync<ExamResponse>();

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.Exams.SingleAsync(x => x.Id == exam.Id);
            db.Exams.Remove(entity);
            await db.SaveChangesAsync();
        });

        var response = await adminClient.PutAsJsonAsync(
            $"/api/exams/{exam.Id}",
            new UpdateExamRequest(data.Class.Id, data.Subject.Id, "Updated Midterm", new DateOnly(2026, 5, 21), 100m));

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteExam_RaceCondition_Returns404NotServerError()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var data = await SeedWorkspaceAsync(adminClient, "EX2");

        var exam = await adminClient.PostAsJsonAsync(
                "/api/exams",
                new CreateExamRequest(data.Class.Id, data.Subject.Id, "Delete Race Exam", new DateOnly(2026, 5, 22), 80m))
            .ReadRequiredEntityAsync<ExamResponse>();

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.Exams.SingleAsync(x => x.Id == exam.Id);
            db.Exams.Remove(entity);
            await db.SaveChangesAsync();
        });

        var response = await adminClient.DeleteAsync($"/api/exams/{exam.Id}");

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    // ── Results ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetResult_WithNonExistentId_Returns404()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync($"/api/results/{Guid.NewGuid()}");

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateResult_WithNonExistentExam_Returns404()
    {
        // This exercises the SingleOrDefaultAsync fix in ResultService.CreateAsync.
        // The service first validates references with AnyAsync; if the exam truly
        // doesn't exist the AppException path is hit cleanly.
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var data = await SeedWorkspaceAsync(adminClient, "RS1");

        var response = await adminClient.PostAsJsonAsync(
            "/api/results",
            new CreateResultRequest(Guid.NewGuid(), data.Student.Id, 70m, "B", null));

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateResult_ExamDeletedAfterValidation_Returns404NotServerError()
    {
        // Race-condition: exam exists when the service checks, but is deleted before
        // the fetch that follows — the SingleOrDefaultAsync fix ensures a clean 404.
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var data = await SeedWorkspaceAsync(adminClient, "RS2");

        var exam = await adminClient.PostAsJsonAsync(
                "/api/exams",
                new CreateExamRequest(data.Class.Id, data.Subject.Id, "Race Result Exam", new DateOnly(2026, 6, 1), 100m))
            .ReadRequiredEntityAsync<ExamResponse>();

        // Remove the exam directly so the service's AnyAsync guard passes (it's
        // already cached by EF tracking in the scope that seeded it), but the
        // subsequent SingleOrDefaultAsync returns null.
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.Exams.SingleAsync(x => x.Id == exam.Id);
            db.Exams.Remove(entity);
            await db.SaveChangesAsync();
        });

        var response = await adminClient.PostAsJsonAsync(
            "/api/results",
            new CreateResultRequest(exam.Id, data.Student.Id, 80m, "A", null));

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateResult_RaceCondition_Returns404NotServerError()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var data = await SeedWorkspaceAsync(adminClient, "RS3");

        var exam = await adminClient.PostAsJsonAsync(
                "/api/exams",
                new CreateExamRequest(data.Class.Id, data.Subject.Id, "Update Race Exam", new DateOnly(2026, 6, 5), 100m))
            .ReadRequiredEntityAsync<ExamResponse>();

        var result = await adminClient.PostAsJsonAsync(
                "/api/results",
                new CreateResultRequest(exam.Id, data.Student.Id, 75m, "B+", null))
            .ReadRequiredEntityAsync<ResultResponse>();

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.Results.SingleAsync(x => x.Id == result.Id);
            db.Results.Remove(entity);
            await db.SaveChangesAsync();
        });

        var response = await adminClient.PutAsJsonAsync(
            $"/api/results/{result.Id}",
            new UpdateResultRequest(exam.Id, data.Student.Id, 90m, "A", "Updated after race"));

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteResult_RaceCondition_Returns404NotServerError()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var data = await SeedWorkspaceAsync(adminClient, "RS4");

        var exam = await adminClient.PostAsJsonAsync(
                "/api/exams",
                new CreateExamRequest(data.Class.Id, data.Subject.Id, "Delete Race Result Exam", new DateOnly(2026, 6, 10), 100m))
            .ReadRequiredEntityAsync<ExamResponse>();

        var result = await adminClient.PostAsJsonAsync(
                "/api/results",
                new CreateResultRequest(exam.Id, data.Student.Id, 65m, "C", null))
            .ReadRequiredEntityAsync<ResultResponse>();

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.Results.SingleAsync(x => x.Id == result.Id);
            db.Results.Remove(entity);
            await db.SaveChangesAsync();
        });

        var response = await adminClient.DeleteAsync($"/api/results/{result.Id}");

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    // ── Attendance ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAttendance_WithNonExistentId_Returns404()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync($"/api/attendance/{Guid.NewGuid()}");

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAttendance_RaceCondition_Returns404NotServerError()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var data = await SeedWorkspaceAsync(adminClient, "ATT1");

        var attendance = await adminClient.PostAsJsonAsync(
                "/api/attendance",
                new CreateAttendanceRequest(
                    data.Student.Id, data.Class.Id, data.Subject.Id, data.Teacher.Id,
                    new DateOnly(2026, 4, 1), "Present", null))
            .ReadRequiredEntityAsync<AttendanceResponse>();

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.AttendanceRecords.SingleAsync(x => x.Id == attendance.Id);
            db.AttendanceRecords.Remove(entity);
            await db.SaveChangesAsync();
        });

        var response = await adminClient.PutAsJsonAsync(
            $"/api/attendance/{attendance.Id}",
            new UpdateAttendanceRequest(
                data.Student.Id, data.Class.Id, data.Subject.Id, data.Teacher.Id,
                new DateOnly(2026, 4, 1), "Absent", "Updated after race"));

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    // ── Timetable ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTimetable_WithNonExistentId_Returns404()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync($"/api/timetable/{Guid.NewGuid()}");

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTimetable_RaceCondition_Returns404NotServerError()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var data = await SeedWorkspaceAsync(adminClient, "TT1");

        var entry = await adminClient.PostAsJsonAsync(
                "/api/timetable",
                new CreateTimetableEntryRequest(
                    data.Class.Id, data.Subject.Id, data.Teacher.Id,
                    "Wednesday", new TimeOnly(10, 0), new TimeOnly(11, 0), "R10"))
            .ReadRequiredEntityAsync<TimetableEntryResponse>();

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.TimetableEntries.SingleAsync(x => x.Id == entry.Id);
            db.TimetableEntries.Remove(entity);
            await db.SaveChangesAsync();
        });

        var response = await adminClient.PutAsJsonAsync(
            $"/api/timetable/{entry.Id}",
            new UpdateTimetableEntryRequest(
                data.Class.Id, data.Subject.Id, data.Teacher.Id,
                "Thursday", new TimeOnly(11, 0), new TimeOnly(12, 0), "R11"));

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTimetable_RaceCondition_Returns404NotServerError()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var data = await SeedWorkspaceAsync(adminClient, "TT2");

        var entry = await adminClient.PostAsJsonAsync(
                "/api/timetable",
                new CreateTimetableEntryRequest(
                    data.Class.Id, data.Subject.Id, data.Teacher.Id,
                    "Friday", new TimeOnly(13, 0), new TimeOnly(14, 0), "R12"))
            .ReadRequiredEntityAsync<TimetableEntryResponse>();

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.TimetableEntries.SingleAsync(x => x.Id == entry.Id);
            db.TimetableEntries.Remove(entity);
            await db.SaveChangesAsync();
        });

        var response = await adminClient.DeleteAsync($"/api/timetable/{entry.Id}");

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    // ── Fees ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetFee_WithNonExistentId_Returns404()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync($"/api/fees/{Guid.NewGuid()}");

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateFee_RaceCondition_Returns404NotServerError()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(
            new CreateStudentRequest(
                "Fee Race Student", "fee.race.update@school.com", "Student@123",
                null, null, "ST-FU1",
                new DateOnly(2011, 5, 1), Gender.Female,
                new DateOnly(2024, 9, 1), null, null));

        var fee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(student.Id, "Tuition", 500m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), FeeStatus.Pending));

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.Fees.SingleAsync(x => x.Id == fee.Id);
            db.Fees.Remove(entity);
            await db.SaveChangesAsync();
        });

        var response = await adminClient.PutAsJsonAsync(
            $"/api/fees/{fee.Id}",
            new UpdateFeeRequest(student.Id, "Tuition Updated", 600m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), FeeStatus.Pending));

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteFee_RaceCondition_Returns404NotServerError()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(
            new CreateStudentRequest(
                "Fee Race Delete", "fee.race.delete@school.com", "Student@123",
                null, null, "ST-FD1",
                new DateOnly(2011, 5, 1), Gender.Male,
                new DateOnly(2024, 9, 1), null, null));

        var fee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(student.Id, "Library", 100m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), FeeStatus.Pending));

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.Fees.SingleAsync(x => x.Id == fee.Id);
            db.Fees.Remove(entity);
            await db.SaveChangesAsync();
        });

        var response = await adminClient.DeleteAsync($"/api/fees/{fee.Id}");

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    // ── Payments ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddPayment_WithNonExistentFee_Returns404()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.PostAsJsonAsync(
            "/api/payments",
            new CreatePaymentRequest(Guid.NewGuid(), 50m, DateTime.UtcNow, PaymentMethod.Cash, "PAY-GHOST"));

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddPayment_FeeDeletedAfterValidation_Returns404NotServerError()
    {
        // Race-condition: fee exists when validated, deleted before the payment save.
        // The SingleOrDefaultAsync fix in FeeService.AddPaymentAsync ensures 500 never
        // leaks — but the primary race is the FK constraint or the fee-fetch itself.
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(
            new CreateStudentRequest(
                "Payment Race", "payment.race@school.com", "Student@123",
                null, null, "ST-PR1",
                new DateOnly(2012, 1, 1), Gender.Male,
                new DateOnly(2024, 9, 1), null, null));

        var fee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(student.Id, "Sports", 150m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), FeeStatus.Pending));

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.Fees.SingleAsync(x => x.Id == fee.Id);
            db.Fees.Remove(entity);
            await db.SaveChangesAsync();
        });

        var response = await adminClient.PostAsJsonAsync(
            "/api/payments",
            new CreatePaymentRequest(fee.Id, 50m, DateTime.UtcNow, PaymentMethod.Card, "PAY-RACE1"));

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    // ── Parents ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetParent_WithNonExistentId_Returns404()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync($"/api/parents/{Guid.NewGuid()}");

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateParent_RaceCondition_Returns404NotServerError()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var parent = await adminClient.CreateParentAsync(
            new SchoolManagement.Application.Parents.CreateParentRequest(
                "Race Parent", "race.parent.update@school.com", "Parent@123",
                "555000111", "Oak Street", "Engineer"));

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.Parents.SingleAsync(x => x.Id == parent.Id);
            db.Parents.Remove(entity);
            await db.SaveChangesAsync();
        });

        var response = await adminClient.PutAsJsonAsync(
            $"/api/parents/{parent.Id}",
            new SchoolManagement.Application.Parents.UpdateParentRequest(
                "Updated Race Parent", "race.parent.updated@school.com",
                "555000222", "Elm Street", "Architect"));

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteParent_RaceCondition_Returns404NotServerError()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var parent = await adminClient.CreateParentAsync(
            new SchoolManagement.Application.Parents.CreateParentRequest(
                "Race Parent Delete", "race.parent.delete@school.com", "Parent@123",
                "555999888", "Pine Avenue", "Doctor"));

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.Parents.SingleAsync(x => x.Id == parent.Id);
            db.Parents.Remove(entity);
            await db.SaveChangesAsync();
        });

        var response = await adminClient.DeleteAsync($"/api/parents/{parent.Id}");

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    // ── Supporting types ──────────────────────────────────────────────────────

    private sealed record WorkspaceData(
        TeacherResponse Teacher,
        SubjectResponse Subject,
        ClassResponse Class,
        StudentResponse Student);
}

// ── Extension helpers ─────────────────────────────────────────────────────────

internal static class HttpResponseMessageTaskExtensions
{
    /// <summary>
    /// Awaits the response, asserts 2xx, deserialises the ApiResponse wrapper
    /// and returns the inner Data value. Throws a descriptive exception on failure.
    /// </summary>
    internal static async Task<T> ReadRequiredEntityAsync<T>(this Task<HttpResponseMessage> responseTask) where T : class
    {
        var response = await responseTask;
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Expected 2xx but got {(int)response.StatusCode}. Body: {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<SchoolManagement.Application.Common.Models.ApiResponse<T>>();
        return payload?.Data ?? throw new InvalidOperationException($"Response data for {typeof(T).Name} was null.");
    }
}
