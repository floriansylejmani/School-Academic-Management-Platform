using System.Net;
using System.Net.Http.Json;
using SchoolManagement.Application.Attendance;
using SchoolManagement.Application.Classes;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Exams;
using SchoolManagement.Application.Results;
using SchoolManagement.Application.Students;
using SchoolManagement.Application.Subjects;
using SchoolManagement.Application.Teachers;
using SchoolManagement.Application.Timetable;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Tests.Common;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests.Teachers;

public sealed class TeacherWorkspaceApiTests : IClassFixture<SchoolManagementApiFactory>
{
    private readonly SchoolManagementApiFactory _factory;

    public TeacherWorkspaceApiTests(SchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Teacher_ReadEndpoints_ReturnOnlyTeacherScopedData()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var data = await SeedTeacherWorkspaceAsync(adminClient);

        using var teacherClient = await _factory.CreateAuthenticatedClientAsync(data.Teacher.Email, data.TeacherPassword);

        var teacherProfileResponse = await teacherClient.GetAsync("/api/teachers/me");
        teacherProfileResponse.EnsureSuccessStatusCode();
        var teacherProfile = await teacherProfileResponse.ReadApiResponseAsync<TeacherResponse>();
        Assert.Equal(data.Teacher.Id, teacherProfile.Data!.Id);

        var classesResponse = await teacherClient.GetAsync("/api/classes?pageNumber=1&pageSize=20");
        classesResponse.EnsureSuccessStatusCode();
        var classes = await classesResponse.ReadApiResponseAsync<PagedResponse<ClassResponse>>();
        Assert.Equal(data.Class.Id, Assert.Single(classes.Data!.Items).Id);

        var studentsResponse = await teacherClient.GetAsync("/api/students?pageNumber=1&pageSize=20");
        studentsResponse.EnsureSuccessStatusCode();
        var students = await studentsResponse.ReadApiResponseAsync<PagedResponse<StudentResponse>>();
        Assert.Equal(data.Student.Id, Assert.Single(students.Data!.Items).Id);

        var subjectsResponse = await teacherClient.GetAsync("/api/subjects?pageNumber=1&pageSize=20");
        subjectsResponse.EnsureSuccessStatusCode();
        var subjects = await subjectsResponse.ReadApiResponseAsync<PagedResponse<SubjectResponse>>();
        Assert.Equal(data.Subject.Id, Assert.Single(subjects.Data!.Items).Id);

        var timetableResponse = await teacherClient.GetAsync("/api/timetable?pageNumber=1&pageSize=20");
        timetableResponse.EnsureSuccessStatusCode();
        var timetable = await timetableResponse.ReadApiResponseAsync<PagedResponse<TimetableEntryResponse>>();
        Assert.Equal(data.TimetableEntry.Id, Assert.Single(timetable.Data!.Items).Id);

        var attendanceResponse = await teacherClient.GetAsync("/api/attendance?pageNumber=1&pageSize=20");
        attendanceResponse.EnsureSuccessStatusCode();
        var attendance = await attendanceResponse.ReadApiResponseAsync<PagedResponse<AttendanceResponse>>();
        Assert.Equal(data.Attendance.Id, Assert.Single(attendance.Data!.Items).Id);

        var examsResponse = await teacherClient.GetAsync("/api/exams?pageNumber=1&pageSize=20");
        examsResponse.EnsureSuccessStatusCode();
        var exams = await examsResponse.ReadApiResponseAsync<PagedResponse<ExamResponse>>();
        Assert.Equal(data.Exam.Id, Assert.Single(exams.Data!.Items).Id);

        var resultsResponse = await teacherClient.GetAsync("/api/results?pageNumber=1&pageSize=20");
        resultsResponse.EnsureSuccessStatusCode();
        var results = await resultsResponse.ReadApiResponseAsync<PagedResponse<ResultResponse>>();
        Assert.Equal(data.Result.Id, Assert.Single(results.Data!.Items).Id);
    }

    [Fact]
    public async Task Teacher_CannotCreateAttendanceOrResultsOutsideOwnedScope()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var data = await SeedTeacherWorkspaceAsync(adminClient);

        using var teacherClient = await _factory.CreateAuthenticatedClientAsync(data.Teacher.Email, data.TeacherPassword);

        var attendanceResponse = await teacherClient.PostAsJsonAsync(
            "/api/attendance",
            new CreateAttendanceRequest(
                data.OtherStudent.Id,
                data.OtherClass.Id,
                data.OtherSubject.Id,
                data.OtherTeacher.Id,
                new DateOnly(2026, 4, 10),
                "Absent",
                "Out of scope"));

        await attendanceResponse.AssertStatusCodeAsync(HttpStatusCode.Forbidden);

        var resultResponse = await teacherClient.PostAsJsonAsync(
            "/api/results",
            new CreateResultRequest(data.OtherExam.Id, data.OtherStudent.Id, 55m, "C", "Out of scope"));

        await resultResponse.AssertStatusCodeAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_CreateAttendance_UsesCurrentTeacherIdentity()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var data = await SeedTeacherWorkspaceAsync(adminClient);

        using var teacherClient = await _factory.CreateAuthenticatedClientAsync(data.Teacher.Email, data.TeacherPassword);

        var response = await teacherClient.PostAsJsonAsync(
            "/api/attendance",
            new CreateAttendanceRequest(
                data.Student.Id,
                data.Class.Id,
                data.Subject.Id,
                data.OtherTeacher.Id,
                new DateOnly(2026, 4, 11),
                "Present",
                "Teacher identity should be scoped"));

        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<AttendanceResponse>();
        Assert.Equal(data.Teacher.Id, payload.Data!.TeacherId);
    }

    private async Task<TeacherWorkspaceSeedData> SeedTeacherWorkspaceAsync(HttpClient adminClient)
    {
        var teacher = await CreateTeacherAsync(
            adminClient,
            new CreateTeacherRequest("Teacher One", "teacher.one.scope@school.com", "Teacher@123", null, null, "T-100", "Mathematics", new DateOnly(2022, 9, 1)));
        var otherTeacher = await CreateTeacherAsync(
            adminClient,
            new CreateTeacherRequest("Teacher Two", "teacher.two.scope@school.com", "Teacher@123", null, null, "T-101", "Science", new DateOnly(2022, 9, 1)));

        var subject = await CreateSubjectAsync(adminClient, new CreateSubjectRequest("Mathematics", "MATH-10", null));
        var otherSubject = await CreateSubjectAsync(adminClient, new CreateSubjectRequest("Science", "SCI-10", null));

        var academicClass = await CreateClassAsync(adminClient, new CreateClassRequest("Grade 10", "A", "2025/2026", teacher.Id));
        var otherClass = await CreateClassAsync(adminClient, new CreateClassRequest("Grade 10", "B", "2025/2026", otherTeacher.Id));

        var student = await adminClient.CreateStudentAsync(
            new CreateStudentRequest("Teacher Scoped Student", "teacher.scoped.student@school.com", "Student@123", null, null, "TS-100", new DateOnly(2010, 1, 1), Gender.Male, new DateOnly(2024, 9, 1), null, academicClass.Id));
        var otherStudent = await adminClient.CreateStudentAsync(
            new CreateStudentRequest("Other Scoped Student", "other.scoped.student@school.com", "Student@123", null, null, "TS-101", new DateOnly(2010, 1, 1), Gender.Female, new DateOnly(2024, 9, 1), null, otherClass.Id));

        var timetableEntry = await CreateTimetableEntryAsync(
            adminClient,
            new CreateTimetableEntryRequest(academicClass.Id, subject.Id, teacher.Id, "Monday", new TimeOnly(8, 0), new TimeOnly(9, 0), "R1"));
        await CreateTimetableEntryAsync(
            adminClient,
            new CreateTimetableEntryRequest(otherClass.Id, otherSubject.Id, otherTeacher.Id, "Tuesday", new TimeOnly(9, 0), new TimeOnly(10, 0), "R2"));

        var attendance = await CreateAttendanceAsync(
            adminClient,
            new CreateAttendanceRequest(student.Id, academicClass.Id, subject.Id, teacher.Id, new DateOnly(2026, 4, 9), "Present", "Scoped"));
        await CreateAttendanceAsync(
            adminClient,
            new CreateAttendanceRequest(otherStudent.Id, otherClass.Id, otherSubject.Id, otherTeacher.Id, new DateOnly(2026, 4, 9), "Absent", "Other"));

        var exam = await CreateExamAsync(
            adminClient,
            new CreateExamRequest(academicClass.Id, subject.Id, "Teacher Midterm", new DateOnly(2026, 5, 10), 100m));
        var otherExam = await CreateExamAsync(
            adminClient,
            new CreateExamRequest(otherClass.Id, otherSubject.Id, "Other Midterm", new DateOnly(2026, 5, 12), 100m));

        var result = await CreateResultAsync(
            adminClient,
            new CreateResultRequest(exam.Id, student.Id, 88m, "A", "Scoped"));
        await CreateResultAsync(
            adminClient,
            new CreateResultRequest(otherExam.Id, otherStudent.Id, 72m, "B", "Other"));

        return new TeacherWorkspaceSeedData(
            teacher,
            "Teacher@123",
            otherTeacher,
            academicClass,
            otherClass,
            subject,
            otherSubject,
            student,
            otherStudent,
            timetableEntry,
            attendance,
            exam,
            otherExam,
            result);
    }

    private static async Task<TeacherResponse> CreateTeacherAsync(HttpClient client, CreateTeacherRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/teachers", request);
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<TeacherResponse>();
        return payload.Data ?? throw new InvalidOperationException("Teacher creation did not return data.");
    }

    private static async Task<ClassResponse> CreateClassAsync(HttpClient client, CreateClassRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/classes", request);
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<ClassResponse>();
        return payload.Data ?? throw new InvalidOperationException("Class creation did not return data.");
    }

    private static async Task<SubjectResponse> CreateSubjectAsync(HttpClient client, CreateSubjectRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/subjects", request);
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<SubjectResponse>();
        return payload.Data ?? throw new InvalidOperationException("Subject creation did not return data.");
    }

    private static async Task<TimetableEntryResponse> CreateTimetableEntryAsync(HttpClient client, CreateTimetableEntryRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/timetable", request);
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<TimetableEntryResponse>();
        return payload.Data ?? throw new InvalidOperationException("Timetable creation did not return data.");
    }

    private static async Task<AttendanceResponse> CreateAttendanceAsync(HttpClient client, CreateAttendanceRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/attendance", request);
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<AttendanceResponse>();
        return payload.Data ?? throw new InvalidOperationException("Attendance creation did not return data.");
    }

    private static async Task<ExamResponse> CreateExamAsync(HttpClient client, CreateExamRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/exams", request);
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<ExamResponse>();
        return payload.Data ?? throw new InvalidOperationException("Exam creation did not return data.");
    }

    private static async Task<ResultResponse> CreateResultAsync(HttpClient client, CreateResultRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/results", request);
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<ResultResponse>();
        return payload.Data ?? throw new InvalidOperationException("Result creation did not return data.");
    }

    private sealed record TeacherWorkspaceSeedData(
        TeacherResponse Teacher,
        string TeacherPassword,
        TeacherResponse OtherTeacher,
        ClassResponse Class,
        ClassResponse OtherClass,
        SubjectResponse Subject,
        SubjectResponse OtherSubject,
        StudentResponse Student,
        StudentResponse OtherStudent,
        TimetableEntryResponse TimetableEntry,
        AttendanceResponse Attendance,
        ExamResponse Exam,
        ExamResponse OtherExam,
        ResultResponse Result);
}
