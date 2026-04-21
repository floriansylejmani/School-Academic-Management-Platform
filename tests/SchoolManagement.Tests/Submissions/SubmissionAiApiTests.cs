using System.Net;
using System.Net.Http.Json;
using SchoolManagement.Application.Classes;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Exams;
using SchoolManagement.Application.Students;
using SchoolManagement.Application.Submissions;
using SchoolManagement.Application.Subjects;
using SchoolManagement.Application.Teachers;
using SchoolManagement.Application.Timetable;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Tests.Common;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests.Submissions;

public sealed class SubmissionAiApiTests : IClassFixture<SchoolManagementApiFactory>
{
    private readonly SchoolManagementApiFactory _factory;

    public SubmissionAiApiTests(SchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Teacher_CanGenerateSmartGrade_ForScopedSubmission()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var data = await SeedSubmissionWorkspaceAsync(adminClient);

        using var studentClient = await _factory.CreateAuthenticatedClientAsync(data.StudentEmail, StudentPassword);
        using var teacherClient = await _factory.CreateAuthenticatedClientAsync(data.Teacher.Email, TeacherPassword);

        var createResponse = await studentClient.PostAsJsonAsync(
            "/api/submissions",
            new CreateSubmissionRequest(data.Exam.Id, null, "Discuss the main theme.", "This essay response explains the theme with supporting evidence."));
        createResponse.EnsureSuccessStatusCode();
        var createdSubmission = (await createResponse.ReadApiResponseAsync<SubmissionResponse>()).Data!;

        var smartGradeResponse = await teacherClient.PostAsJsonAsync(
            $"/api/submissions/{createdSubmission.Id}/smart-grade",
            new RequestSubmissionAIRequest("Use the literature rubric out of 20.", "Focus on structure and evidence."));
        await smartGradeResponse.AssertStatusCodeAsync(HttpStatusCode.OK);

        var payload = await smartGradeResponse.ReadApiResponseAsync<SubmissionResponse>();
        Assert.NotNull(payload.Data);
        Assert.NotNull(payload.Data!.AIReview);
        Assert.Equal("SmartGrade", payload.Data.AIReview!.Mode);
        Assert.Equal(createdSubmission.Id, _factory.AIGradingService.LastRequest?.SubmissionId);
        Assert.Equal("SmartGrade", _factory.AIGradingService.LastRequest?.Mode);
    }

    [Fact]
    public async Task Student_CannotRequestAiFeedback()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var data = await SeedSubmissionWorkspaceAsync(adminClient);

        using var studentClient = await _factory.CreateAuthenticatedClientAsync(data.StudentEmail, StudentPassword);

        var createResponse = await studentClient.PostAsJsonAsync(
            "/api/submissions",
            new CreateSubmissionRequest(data.Exam.Id, null, null, "Student answer for unauthorized AI request test."));
        createResponse.EnsureSuccessStatusCode();
        var createdSubmission = (await createResponse.ReadApiResponseAsync<SubmissionResponse>()).Data!;

        var aiResponse = await studentClient.PostAsJsonAsync(
            $"/api/submissions/{createdSubmission.Id}/ai-feedback",
            new RequestSubmissionAIRequest(null, null));

        await aiResponse.AssertStatusCodeAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Student_DoesNotSeeUnreleasedAiFeedback()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var data = await SeedSubmissionWorkspaceAsync(adminClient);

        using var studentClient = await _factory.CreateAuthenticatedClientAsync(data.StudentEmail, StudentPassword);

        var createResponse = await studentClient.PostAsJsonAsync(
            "/api/submissions",
            new CreateSubmissionRequest(data.Exam.Id, null, "Essay prompt", "Student answer awaiting release."));
        createResponse.EnsureSuccessStatusCode();
        var createdSubmission = (await createResponse.ReadApiResponseAsync<SubmissionResponse>()).Data!;

        var aiResponse = await adminClient.PostAsJsonAsync(
            $"/api/submissions/{createdSubmission.Id}/ai-feedback",
            new RequestSubmissionAIRequest("Use the class rubric.", null));
        await aiResponse.AssertStatusCodeAsync(HttpStatusCode.OK);

        var studentGetResponse = await studentClient.GetAsync($"/api/submissions/{createdSubmission.Id}");
        studentGetResponse.EnsureSuccessStatusCode();
        var studentPayload = await studentGetResponse.ReadApiResponseAsync<SubmissionResponse>();

        Assert.NotNull(studentPayload.Data);
        Assert.False(studentPayload.Data!.HasAIReview);
        Assert.Null(studentPayload.Data.AIReview);
        Assert.Null(studentPayload.Data.TeacherFinalScore);
        Assert.False(studentPayload.Data.IsAiFeedbackReleasedToStudent);
    }

    [Fact]
    public async Task TeacherReviewRelease_AllowsStudentToSeeAiFeedbackAndTeacherGrade()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var data = await SeedSubmissionWorkspaceAsync(adminClient);

        using var studentClient = await _factory.CreateAuthenticatedClientAsync(data.StudentEmail, StudentPassword);

        var createResponse = await studentClient.PostAsJsonAsync(
            "/api/submissions",
            new CreateSubmissionRequest(data.Exam.Id, null, "Essay prompt", "Student answer ready for release."));
        createResponse.EnsureSuccessStatusCode();
        var createdSubmission = (await createResponse.ReadApiResponseAsync<SubmissionResponse>()).Data!;

        var aiResponse = await adminClient.PostAsJsonAsync(
            $"/api/submissions/{createdSubmission.Id}/smart-grade",
            new RequestSubmissionAIRequest("Use the essay rubric.", "Return balanced guidance."));
        await aiResponse.AssertStatusCodeAsync(HttpStatusCode.OK);

        var reviewResponse = await adminClient.PutAsJsonAsync(
            $"/api/submissions/{createdSubmission.Id}/teacher-review",
            new UpdateSubmissionTeacherReviewRequest(18m, "A", "Strong reasoning and clear evidence.", true));
        await reviewResponse.AssertStatusCodeAsync(HttpStatusCode.OK);

        var studentGetResponse = await studentClient.GetAsync($"/api/submissions/{createdSubmission.Id}");
        studentGetResponse.EnsureSuccessStatusCode();
        var studentPayload = await studentGetResponse.ReadApiResponseAsync<SubmissionResponse>();

        Assert.NotNull(studentPayload.Data);
        Assert.True(studentPayload.Data!.HasAIReview);
        Assert.NotNull(studentPayload.Data.AIReview);
        Assert.Equal(18m, studentPayload.Data.TeacherFinalScore);
        Assert.Equal("A", studentPayload.Data.TeacherFinalGrade);
        Assert.Equal("Strong reasoning and clear evidence.", studentPayload.Data.TeacherReviewNotes);
        Assert.True(studentPayload.Data.IsAiFeedbackReleasedToStudent);
    }

    private async Task<SubmissionSeedData> SeedSubmissionWorkspaceAsync(HttpClient adminClient)
    {
        var teacher = await CreateTeacherAsync(
            adminClient,
            new CreateTeacherRequest("Essay Teacher", "essay.teacher@school.com", TeacherPassword, null, null, "T-SUB-1", "Literature", new DateOnly(2023, 9, 1)));
        var subject = await CreateSubjectAsync(adminClient, new CreateSubjectRequest("Literature", "LIT-10", "Essay writing"));
        var academicClass = await CreateClassAsync(adminClient, new CreateClassRequest("Grade 10", "A", "2025/2026", teacher.Id));

        var student = await adminClient.CreateStudentAsync(
            new CreateStudentRequest("Essay Student", StudentEmail, StudentPassword, null, null, "ST-SUB-1", new DateOnly(2010, 1, 1), Gender.Female, new DateOnly(2024, 9, 1), null, academicClass.Id));

        await CreateTimetableEntryAsync(
            adminClient,
            new CreateTimetableEntryRequest(academicClass.Id, subject.Id, teacher.Id, "Monday", new TimeOnly(8, 0), new TimeOnly(9, 0), "R-101"));

        var exam = await CreateExamAsync(
            adminClient,
            new CreateExamRequest(academicClass.Id, subject.Id, "Essay Midterm", new DateOnly(2026, 5, 20), 20m));

        return new SubmissionSeedData(teacher, academicClass, subject, student, exam, StudentEmail);
    }

    private static async Task<TeacherResponse> CreateTeacherAsync(HttpClient client, CreateTeacherRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/teachers", request);
        response.EnsureSuccessStatusCode();
        return (await response.ReadApiResponseAsync<TeacherResponse>()).Data
            ?? throw new InvalidOperationException("Teacher creation did not return data.");
    }

    private static async Task<SubjectResponse> CreateSubjectAsync(HttpClient client, CreateSubjectRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/subjects", request);
        response.EnsureSuccessStatusCode();
        return (await response.ReadApiResponseAsync<SubjectResponse>()).Data
            ?? throw new InvalidOperationException("Subject creation did not return data.");
    }

    private static async Task<ClassResponse> CreateClassAsync(HttpClient client, CreateClassRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/classes", request);
        response.EnsureSuccessStatusCode();
        return (await response.ReadApiResponseAsync<ClassResponse>()).Data
            ?? throw new InvalidOperationException("Class creation did not return data.");
    }

    private static async Task<TimetableEntryResponse> CreateTimetableEntryAsync(HttpClient client, CreateTimetableEntryRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/timetable", request);
        response.EnsureSuccessStatusCode();
        return (await response.ReadApiResponseAsync<TimetableEntryResponse>()).Data
            ?? throw new InvalidOperationException("Timetable creation did not return data.");
    }

    private static async Task<ExamResponse> CreateExamAsync(HttpClient client, CreateExamRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/exams", request);
        response.EnsureSuccessStatusCode();
        return (await response.ReadApiResponseAsync<ExamResponse>()).Data
            ?? throw new InvalidOperationException("Exam creation did not return data.");
    }

    private const string TeacherPassword = "Teacher@123";
    private const string StudentPassword = "Student@123";
    private const string StudentEmail = "essay.student@school.com";

    private sealed record SubmissionSeedData(
        TeacherResponse Teacher,
        ClassResponse Class,
        SubjectResponse Subject,
        StudentResponse Student,
        ExamResponse Exam,
        string StudentEmail);
}
