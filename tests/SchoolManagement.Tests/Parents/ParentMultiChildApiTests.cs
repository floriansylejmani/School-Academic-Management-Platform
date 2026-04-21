using System.Net;
using System.Net.Http.Json;
using SchoolManagement.Application.Attendance;
using SchoolManagement.Application.Classes;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Exams;
using SchoolManagement.Application.Fees;
using SchoolManagement.Application.Notifications;
using SchoolManagement.Application.Parents;
using SchoolManagement.Application.Results;
using SchoolManagement.Application.Students;
using SchoolManagement.Application.Subjects;
using SchoolManagement.Application.Teachers;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Tests.Common;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests.Parents;

public sealed class ParentMultiChildApiTests : IClassFixture<SchoolManagementApiFactory>
{
    private readonly SchoolManagementApiFactory _factory;

    public ParentMultiChildApiTests(SchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Parent_CanAccessRecordsForEachLinkedChild()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var parent = await adminClient.CreateParentAsync(
            new CreateParentRequest("Family Parent", "family.parent@school.com", "Parent@123", null, null, null));
        var otherParent = await adminClient.CreateParentAsync(
            new CreateParentRequest("Other Parent", "other.family.parent@school.com", "Parent@123", null, null, null));

        var teacher = await CreateTeacherAsync(adminClient, "parent.multi.teacher@school.com", "T-PM1");
        var subject = await CreateSubjectAsync(adminClient, "Science", "SCI-PM1");
        var classOne = await CreateClassAsync(adminClient, "Grade 6 PM", teacher.Id);
        var classTwo = await CreateClassAsync(adminClient, "Grade 7 PM", teacher.Id);
        var otherClass = await CreateClassAsync(adminClient, "Grade 8 PM", teacher.Id);

        var childOne = await adminClient.CreateStudentAsync(
            BuildStudentRequest("Child One", "family.child.one@school.com", "ST-PM1", parent.Id, classOne.Id));
        var childTwo = await adminClient.CreateStudentAsync(
            BuildStudentRequest("Child Two", "family.child.two@school.com", "ST-PM2", parent.Id, classTwo.Id));
        var outsider = await adminClient.CreateStudentAsync(
            BuildStudentRequest("Other Child", "family.child.other@school.com", "ST-PM3", otherParent.Id, otherClass.Id));

        var attendanceCreateResponse = await adminClient.PostAsJsonAsync(
            "/api/attendance",
            new CreateAttendanceRequest(childTwo.Id, classTwo.Id, subject.Id, teacher.Id, new DateOnly(2026, 4, 5), "Present", null));
        attendanceCreateResponse.EnsureSuccessStatusCode();

        var exam = await CreateExamAsync(adminClient, classTwo.Id, subject.Id, "Science Midterm PM");
        var resultCreateResponse = await adminClient.PostAsJsonAsync(
            "/api/results",
            new CreateResultRequest(exam.Id, childTwo.Id, 92m, "A", null));
        resultCreateResponse.EnsureSuccessStatusCode();

        await adminClient.CreateFeeAsync(
            new CreateFeeRequest(childTwo.Id, "Transport Fee", 75m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), FeeStatus.Pending));

        using var parentClient = await _factory.CreateAuthenticatedClientAsync("family.parent@school.com", "Parent@123");

        var childrenResponse = await parentClient.GetAsync("/api/students/parent/me?pageNumber=1&pageSize=10");
        childrenResponse.EnsureSuccessStatusCode();
        var childrenPayload = await childrenResponse.ReadApiResponseAsync<PagedResponse<StudentResponse>>();
        Assert.Equal(2, childrenPayload.Data!.TotalCount);
        Assert.Contains(childrenPayload.Data.Items, student => student.Id == childOne.Id);
        Assert.Contains(childrenPayload.Data.Items, student => student.Id == childTwo.Id);

        var attendanceResponse = await parentClient.GetAsync($"/api/attendance/student/{childTwo.Id}");
        attendanceResponse.EnsureSuccessStatusCode();
        var attendancePayload = await attendanceResponse.ReadApiResponseAsync<IReadOnlyCollection<AttendanceResponse>>();
        Assert.Contains(attendancePayload.Data!, record => record.StudentId == childTwo.Id);

        var resultResponse = await parentClient.GetAsync($"/api/results/student/{childTwo.Id}");
        resultResponse.EnsureSuccessStatusCode();
        var resultPayload = await resultResponse.ReadApiResponseAsync<IReadOnlyCollection<ResultResponse>>();
        Assert.Contains(resultPayload.Data!, result => result.StudentId == childTwo.Id);

        var feeResponse = await parentClient.GetAsync($"/api/fees/student/{childTwo.Id}");
        feeResponse.EnsureSuccessStatusCode();
        var feePayload = await feeResponse.ReadApiResponseAsync<IReadOnlyCollection<FeeResponse>>();
        Assert.Contains(feePayload.Data!, fee => fee.StudentId == childTwo.Id);

        var outsiderAttendanceResponse = await parentClient.GetAsync($"/api/attendance/student/{outsider.Id}");
        await outsiderAttendanceResponse.AssertStatusCodeAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Parent_CanAccessExamSchedulesForOwnChildrenClassesOnly()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var parent = await adminClient.CreateParentAsync(
            new CreateParentRequest("Exam Parent", "exam.parent@school.com", "Parent@123", null, null, null));
        var otherParent = await adminClient.CreateParentAsync(
            new CreateParentRequest("Exam Other Parent", "exam.other.parent@school.com", "Parent@123", null, null, null));

        var teacher = await CreateTeacherAsync(adminClient, "exam.parent.teacher@school.com", "T-PM2");
        var subject = await CreateSubjectAsync(adminClient, "Mathematics", "MATH-PM1");
        var classOne = await CreateClassAsync(adminClient, "Grade 5 PM", teacher.Id);
        var classTwo = await CreateClassAsync(adminClient, "Grade 9 PM", teacher.Id);
        var outsiderClass = await CreateClassAsync(adminClient, "Grade 10 PM", teacher.Id);

        await adminClient.CreateStudentAsync(BuildStudentRequest("Exam Child One", "exam.child.one@school.com", "ST-PM4", parent.Id, classOne.Id));
        await adminClient.CreateStudentAsync(BuildStudentRequest("Exam Child Two", "exam.child.two@school.com", "ST-PM5", parent.Id, classTwo.Id));
        await adminClient.CreateStudentAsync(BuildStudentRequest("Exam Outsider", "exam.child.out@school.com", "ST-PM6", otherParent.Id, outsiderClass.Id));

        await CreateExamAsync(adminClient, classOne.Id, subject.Id, "Math Test 1");
        await CreateExamAsync(adminClient, classTwo.Id, subject.Id, "Math Test 2");
        await CreateExamAsync(adminClient, outsiderClass.Id, subject.Id, "Math Test 3");

        using var parentClient = await _factory.CreateAuthenticatedClientAsync("exam.parent@school.com", "Parent@123");

        var classOneResponse = await parentClient.GetAsync($"/api/exams/class/{classOne.Id}");
        classOneResponse.EnsureSuccessStatusCode();
        var classOnePayload = await classOneResponse.ReadApiResponseAsync<IReadOnlyCollection<ExamResponse>>();
        Assert.Contains(classOnePayload.Data!, exam => exam.ClassId == classOne.Id);

        var classTwoResponse = await parentClient.GetAsync($"/api/exams/class/{classTwo.Id}");
        classTwoResponse.EnsureSuccessStatusCode();
        var classTwoPayload = await classTwoResponse.ReadApiResponseAsync<IReadOnlyCollection<ExamResponse>>();
        Assert.Contains(classTwoPayload.Data!, exam => exam.ClassId == classTwo.Id);

        var outsiderResponse = await parentClient.GetAsync($"/api/exams/class/{outsiderClass.Id}");
        await outsiderResponse.AssertStatusCodeAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Parent_Notifications_IncludeEventsForMultipleLinkedChildren()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var parent = await adminClient.CreateParentAsync(
            new CreateParentRequest("Notification Parent", "notif.multi.parent@school.com", "Parent@123", null, null, null));

        var childOne = await adminClient.CreateStudentAsync(
            BuildStudentRequest("Nina Parent", "notif.multi.child.one@school.com", "ST-PM7", parent.Id, null));
        var childTwo = await adminClient.CreateStudentAsync(
            BuildStudentRequest("Omar Parent", "notif.multi.child.two@school.com", "ST-PM8", parent.Id, null));

        await adminClient.CreateFeeAsync(
            new CreateFeeRequest(childOne.Id, "Library Fee", 40m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), FeeStatus.Pending));
        await adminClient.CreateFeeAsync(
            new CreateFeeRequest(childTwo.Id, "Lab Fee", 55m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), FeeStatus.Pending));

        using var parentClient = await _factory.CreateAuthenticatedClientAsync("notif.multi.parent@school.com", "Parent@123");

        var notificationsResponse = await parentClient.GetAsync("/api/notifications?pageNumber=1&pageSize=20");
        notificationsResponse.EnsureSuccessStatusCode();
        var notificationsPayload = await notificationsResponse.ReadApiResponseAsync<PagedResponse<NotificationResponse>>();

        Assert.Contains(notificationsPayload.Data!.Items, notification => notification.Message.Contains("Nina Parent"));
        Assert.Contains(notificationsPayload.Data.Items, notification => notification.Message.Contains("Omar Parent"));
    }

    private static CreateStudentRequest BuildStudentRequest(string fullName, string email, string studentCode, Guid? parentId, Guid? classId)
    {
        return new CreateStudentRequest(
            fullName,
            email,
            "Student@123",
            null,
            null,
            studentCode,
            new DateOnly(2010, 1, 1),
            Gender.Male,
            new DateOnly(2024, 9, 1),
            parentId,
            classId);
    }

    private static async Task<TeacherResponse> CreateTeacherAsync(HttpClient client, string email, string code)
    {
        var response = await client.PostAsJsonAsync(
            "/api/teachers",
            new CreateTeacherRequest($"Teacher {code}", email, "Teacher@123", null, null, code, "Science", new DateOnly(2022, 9, 1)));
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<TeacherResponse>();
        return payload.Data!;
    }

    private static async Task<SubjectResponse> CreateSubjectAsync(HttpClient client, string name, string code)
    {
        var response = await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest(name, code, null));
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<SubjectResponse>();
        return payload.Data!;
    }

    private static async Task<ClassResponse> CreateClassAsync(HttpClient client, string name, Guid teacherId)
    {
        var response = await client.PostAsJsonAsync("/api/classes", new CreateClassRequest(name, "A", "2025/2026", teacherId));
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<ClassResponse>();
        return payload.Data!;
    }

    private static async Task<ExamResponse> CreateExamAsync(HttpClient client, Guid classId, Guid subjectId, string title)
    {
        var response = await client.PostAsJsonAsync(
            "/api/exams",
            new CreateExamRequest(classId, subjectId, title, new DateOnly(2026, 6, 15), 100m));
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<ExamResponse>();
        return payload.Data!;
    }
}
