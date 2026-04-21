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
using SchoolManagement.Application.Timetable;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Tests.Common;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests.Notifications;

public sealed class NotificationApiTests : IClassFixture<SchoolManagementApiFactory>
{
    private readonly SchoolManagementApiFactory _factory;

    public NotificationApiTests(SchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Unauthenticated_CannotReadNotifications()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/notifications");

        await response.AssertStatusCodeAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Student_CanOnlyReadOwnNotifications()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, studentClient, student) = await SeedStudentAsync("OWN");
        using var _ = adminClient;
        using var __ = studentClient;

        var sendResponse = await adminClient.PostAsJsonAsync(
            "/api/notifications/send",
            new SendNotificationRequest("Test", "Hello student", student.UserId, null));
        sendResponse.EnsureSuccessStatusCode();

        var response = await studentClient.GetAsync("/api/notifications?pageNumber=1&pageSize=10");
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<PagedResponse<NotificationResponse>>();

        Assert.True(payload.Data!.TotalCount >= 1);
        Assert.All(payload.Data.Items, notification => Assert.Equal(student.UserId, notification.UserId));
    }

    [Fact]
    public async Task NonAdmin_CannotSendNotifications()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, studentClient, student) = await SeedStudentAsync("DENY");
        using var _ = adminClient;
        using var __ = studentClient;

        var response = await studentClient.PostAsJsonAsync(
            "/api/notifications/send",
            new SendNotificationRequest("Hack", "Not allowed", student.UserId, null));

        await response.AssertStatusCodeAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_CanSendNotificationToSpecificUser()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, studentClient, student) = await SeedStudentAsync("DIRECT");
        using var _ = adminClient;
        using var __ = studentClient;

        var sendResponse = await adminClient.PostAsJsonAsync(
            "/api/notifications/send",
            new SendNotificationRequest("Important update", "Please check your schedule.", student.UserId, null));
        sendResponse.EnsureSuccessStatusCode();

        var notifications = await GetNotificationsAsync(studentClient);
        Assert.Contains(notifications, notification => notification.Title == "Important update");
    }

    [Fact]
    public async Task Admin_CanBroadcastNotificationToRole()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, studentClient1, _) = await SeedStudentAsync("BCAST1");
        var (_, studentClient2, _) = await SeedStudentAsync("BCAST2");
        using var _ = adminClient;
        using var __ = studentClient1;
        using var ___ = studentClient2;

        var sendResponse = await adminClient.PostAsJsonAsync(
            "/api/notifications/send",
            new SendNotificationRequest("Broadcast message", "This goes to all students.", null, "Student"));
        sendResponse.EnsureSuccessStatusCode();

        var notifications1 = await GetNotificationsAsync(studentClient1);
        var notifications2 = await GetNotificationsAsync(studentClient2);

        Assert.Contains(notifications1, notification => notification.Title == "Broadcast message");
        Assert.Contains(notifications2, notification => notification.Title == "Broadcast message");
    }

    [Fact]
    public async Task SendNotification_ToNonExistentUser_Returns404()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.PostAsJsonAsync(
            "/api/notifications/send",
            new SendNotificationRequest("Ghost", "Nobody receives this.", Guid.NewGuid(), null));

        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SendNotification_BothUserIdAndRoleName_Returns400()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, _, student) = await SeedStudentAsync("BOTH");
        using var _ = adminClient;

        var response = await adminClient.PostAsJsonAsync(
            "/api/notifications/send",
            new SendNotificationRequest("Bad", "Both fields set", student.UserId, "Student"));

        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnreadCount_ReflectsUnreadNotifications()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, studentClient, student) = await SeedStudentAsync("COUNT");
        using var _ = adminClient;
        using var __ = studentClient;

        var initialResponse = await studentClient.GetAsync("/api/notifications/unread-count");
        initialResponse.EnsureSuccessStatusCode();
        var initialCount = await initialResponse.ReadApiResponseAsync<UnreadCountResponse>();
        Assert.Equal(0, initialCount.Data!.Count);

        await adminClient.PostAsJsonAsync(
            "/api/notifications/send",
            new SendNotificationRequest("First", "Message 1", student.UserId, null));
        await adminClient.PostAsJsonAsync(
            "/api/notifications/send",
            new SendNotificationRequest("Second", "Message 2", student.UserId, null));

        var updatedResponse = await studentClient.GetAsync("/api/notifications/unread-count");
        updatedResponse.EnsureSuccessStatusCode();
        var updatedCount = await updatedResponse.ReadApiResponseAsync<UnreadCountResponse>();
        Assert.Equal(2, updatedCount.Data!.Count);
    }

    [Fact]
    public async Task GetNotifications_UnreadOnlyFilter_ReturnsOnlyUnreadNotifications()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, studentClient, student) = await SeedStudentAsync("UNREAD");
        using var _ = adminClient;
        using var __ = studentClient;

        await adminClient.PostAsJsonAsync(
            "/api/notifications/send",
            new SendNotificationRequest("Read me", "This will be marked as read.", student.UserId, null));
        await adminClient.PostAsJsonAsync(
            "/api/notifications/send",
            new SendNotificationRequest("Keep unread", "This should stay unread.", student.UserId, null));

        var allNotifications = await GetNotificationsAsync(studentClient);
        var readTarget = allNotifications.First(notification => notification.Title == "Read me");

        var markResponse = await studentClient.PatchAsync($"/api/notifications/{readTarget.Id}/read", null);
        markResponse.EnsureSuccessStatusCode();

        var unreadResponse = await studentClient.GetAsync("/api/notifications?pageNumber=1&pageSize=10&unreadOnly=true");
        unreadResponse.EnsureSuccessStatusCode();
        var unreadPayload = await unreadResponse.ReadApiResponseAsync<PagedResponse<NotificationResponse>>();

        Assert.NotEmpty(unreadPayload.Data!.Items);
        Assert.All(unreadPayload.Data.Items, notification => Assert.False(notification.IsRead));
        Assert.Contains(unreadPayload.Data.Items, notification => notification.Title == "Keep unread");
        Assert.DoesNotContain(unreadPayload.Data.Items, notification => notification.Title == "Read me");
    }

    [Fact]
    public async Task MarkAsRead_UpdatesIsRead()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, studentClient, student) = await SeedStudentAsync("READ");
        using var _ = adminClient;
        using var __ = studentClient;

        await adminClient.PostAsJsonAsync(
            "/api/notifications/send",
            new SendNotificationRequest("Read test", "Mark this.", student.UserId, null));

        var notifications = await GetNotificationsAsync(studentClient);
        var notification = notifications.First(item => item.Title == "Read test");
        Assert.False(notification.IsRead);

        var markResponse = await studentClient.PatchAsync($"/api/notifications/{notification.Id}/read", null);
        markResponse.EnsureSuccessStatusCode();

        var updatedNotifications = await GetNotificationsAsync(studentClient);
        var updated = updatedNotifications.First(item => item.Id == notification.Id);
        Assert.True(updated.IsRead);
    }

    [Fact]
    public async Task MarkAllAsRead_ClearsAllUnread()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, studentClient, student) = await SeedStudentAsync("READALL");
        using var _ = adminClient;
        using var __ = studentClient;

        await adminClient.PostAsJsonAsync(
            "/api/notifications/send",
            new SendNotificationRequest("A", "First", student.UserId, null));
        await adminClient.PostAsJsonAsync(
            "/api/notifications/send",
            new SendNotificationRequest("B", "Second", student.UserId, null));

        var patchResponse = await studentClient.PatchAsync("/api/notifications/read-all", null);
        patchResponse.EnsureSuccessStatusCode();

        var countResponse = await studentClient.GetAsync("/api/notifications/unread-count");
        countResponse.EnsureSuccessStatusCode();
        var count = await countResponse.ReadApiResponseAsync<UnreadCountResponse>();
        Assert.Equal(0, count.Data!.Count);
    }

    [Fact]
    public async Task MarkAsRead_CannotMarkAnotherUsersNotification()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, studentClient1, student1) = await SeedStudentAsync("CROSS1");
        var (_, studentClient2, _) = await SeedStudentAsync("CROSS2");
        using var _ = adminClient;
        using var __ = studentClient1;
        using var ___ = studentClient2;

        await adminClient.PostAsJsonAsync(
            "/api/notifications/send",
            new SendNotificationRequest("Private", "Only student1", student1.UserId, null));

        var notifications = await GetNotificationsAsync(studentClient1);
        var notification = notifications.First(item => item.Title == "Private");

        var response = await studentClient2.PatchAsync($"/api/notifications/{notification.Id}/read", null);
        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResultPublished_SendsNotificationToStudentAndParent()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, parentClient, studentClient, student) = await SeedParentAndStudentAsync("RESULT");
        using var _ = adminClient;
        using var __ = parentClient;
        using var ___ = studentClient;

        var teacher = await CreateTeacherAsync(adminClient, "notif.teacher.result@school.com", "T-NR1");
        var subject = await CreateSubjectAsync(adminClient, "Biology", "BIO-NR1");
        var academicClass = await CreateClassAsync(adminClient, "Grade 8 NR", teacher.Id);
        var exam = await CreateExamAsync(adminClient, academicClass.Id, subject.Id, "Biology Final NR");

        var resultResponse = await adminClient.PostAsJsonAsync(
            "/api/results",
            new CreateResultRequest(exam.Id, student.Id, 85m, "A", null));
        resultResponse.EnsureSuccessStatusCode();

        var studentNotifications = await GetNotificationsAsync(studentClient);
        var parentNotifications = await GetNotificationsAsync(parentClient);

        Assert.Contains(studentNotifications, notification => notification.Title == "Result published" && notification.Message.Contains("Biology Final NR"));
        Assert.Contains(parentNotifications, notification => notification.Title == "Result published" && notification.Message.Contains("Biology Final NR"));
    }

    [Fact]
    public async Task ExamCreated_SendsNotificationToStudentParentAndTeacher()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, parentClient, studentClient, student) = await SeedParentAndStudentAsync("EXAM");
        using var _ = adminClient;
        using var __ = parentClient;
        using var ___ = studentClient;

        var teacher = await CreateTeacherAsync(adminClient, "notif.teacher.exam@school.com", "T-NE1");
        var teacherClient = await _factory.CreateAuthenticatedClientAsync("notif.teacher.exam@school.com", "Teacher@123");
        using var ____ = teacherClient;

        var subject = await CreateSubjectAsync(adminClient, "Chemistry", "CHEM-NE1");
        var academicClass = await CreateClassAsync(adminClient, "Grade 9 NE", teacher.Id);
        await AssignStudentToClassAsync(adminClient, student, academicClass.Id);
        await CreateTimetableEntryAsync(
            adminClient,
            new CreateTimetableEntryRequest(academicClass.Id, subject.Id, teacher.Id, "Monday", new TimeOnly(9, 0), new TimeOnly(10, 0), "R1"));

        var examResponse = await adminClient.PostAsJsonAsync(
            "/api/exams",
            new CreateExamRequest(academicClass.Id, subject.Id, "Chemistry Midterm NE", new DateOnly(2026, 6, 15), 100m));
        examResponse.EnsureSuccessStatusCode();

        var studentNotifications = await GetNotificationsAsync(studentClient);
        var parentNotifications = await GetNotificationsAsync(parentClient);
        var teacherNotifications = await GetNotificationsAsync(teacherClient);

        Assert.Contains(studentNotifications, notification => notification.Title == "New exam scheduled" && notification.Message.Contains("Chemistry Midterm NE"));
        Assert.Contains(parentNotifications, notification => notification.Title == "New exam scheduled" && notification.Message.Contains("Chemistry Midterm NE"));
        Assert.Contains(teacherNotifications, notification => notification.Title == "New exam scheduled" && notification.Message.Contains("Chemistry Midterm NE"));
    }

    [Fact]
    public async Task ExamUpdated_SendsNotificationToStudentParentAndTeacher()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, parentClient, studentClient, student) = await SeedParentAndStudentAsync("EXAMUPD");
        using var _ = adminClient;
        using var __ = parentClient;
        using var ___ = studentClient;

        var teacher = await CreateTeacherAsync(adminClient, "notif.teacher.exam.update@school.com", "T-NE2");
        var teacherClient = await _factory.CreateAuthenticatedClientAsync("notif.teacher.exam.update@school.com", "Teacher@123");
        using var ____ = teacherClient;

        var subject = await CreateSubjectAsync(adminClient, "History", "HIST-NE2");
        var academicClass = await CreateClassAsync(adminClient, "Grade 7 NE", teacher.Id);
        await AssignStudentToClassAsync(adminClient, student, academicClass.Id);
        await CreateTimetableEntryAsync(
            adminClient,
            new CreateTimetableEntryRequest(academicClass.Id, subject.Id, teacher.Id, "Tuesday", new TimeOnly(10, 0), new TimeOnly(11, 0), "R2"));

        var exam = await CreateExamAsync(adminClient, academicClass.Id, subject.Id, "History Midterm NE");

        var studentBefore = (await GetNotificationsAsync(studentClient)).Count;
        var parentBefore = (await GetNotificationsAsync(parentClient)).Count;
        var teacherBefore = (await GetNotificationsAsync(teacherClient)).Count;

        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/exams/{exam.Id}",
            new UpdateExamRequest(academicClass.Id, subject.Id, "History Final NE", new DateOnly(2026, 6, 20), 100m));
        updateResponse.EnsureSuccessStatusCode();

        var studentNotifications = await GetNotificationsAsync(studentClient);
        var parentNotifications = await GetNotificationsAsync(parentClient);
        var teacherNotifications = await GetNotificationsAsync(teacherClient);

        Assert.True(studentNotifications.Count > studentBefore);
        Assert.True(parentNotifications.Count > parentBefore);
        Assert.True(teacherNotifications.Count > teacherBefore);
        Assert.Contains(studentNotifications, notification => notification.Title == "Exam updated" && notification.Message.Contains("History Final NE"));
        Assert.Contains(parentNotifications, notification => notification.Title == "Exam updated" && notification.Message.Contains("History Final NE"));
        Assert.Contains(teacherNotifications, notification => notification.Title == "Exam updated" && notification.Message.Contains("History Final NE"));
    }

    [Fact]
    public async Task AttendanceMarked_SendsNotificationToStudentAndParent()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, parentClient, studentClient, student) = await SeedParentAndStudentAsync("ATT");
        using var _ = adminClient;
        using var __ = parentClient;
        using var ___ = studentClient;

        var teacher = await CreateTeacherAsync(adminClient, "notif.teacher.att@school.com", "T-NA1");
        var subject = await CreateSubjectAsync(adminClient, "Physics", "PHY-NA1");
        var academicClass = await CreateClassAsync(adminClient, "Grade 10 NA", teacher.Id);
        await AssignStudentToClassAsync(adminClient, student, academicClass.Id);

        var studentBefore = (await GetNotificationsAsync(studentClient)).Count;
        var parentBefore = (await GetNotificationsAsync(parentClient)).Count;

        var attendanceResponse = await adminClient.PostAsJsonAsync(
            "/api/attendance",
            new CreateAttendanceRequest(
                student.Id,
                academicClass.Id,
                subject.Id,
                teacher.Id,
                new DateOnly(2026, 4, 9),
                "Present",
                null));
        attendanceResponse.EnsureSuccessStatusCode();

        var studentNotifications = await GetNotificationsAsync(studentClient);
        var parentNotifications = await GetNotificationsAsync(parentClient);

        Assert.True(studentNotifications.Count > studentBefore);
        Assert.True(parentNotifications.Count > parentBefore);
        Assert.Contains(studentNotifications, notification => notification.Title == "Attendance recorded");
        Assert.Contains(parentNotifications, notification => notification.Title == "Attendance recorded");
    }

    [Fact]
    public async Task FeeCreated_SendsNotificationToStudentAndParent()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, parentClient, studentClient, student) = await SeedParentAndStudentAsync("FEE");
        using var _ = adminClient;
        using var __ = parentClient;
        using var ___ = studentClient;

        var fee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(
                student.Id,
                "Library Fee",
                50m,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                FeeStatus.Pending));

        Assert.NotNull(fee);

        var studentNotifications = await GetNotificationsAsync(studentClient);
        var parentNotifications = await GetNotificationsAsync(parentClient);

        Assert.Contains(studentNotifications, notification => notification.Title == "New fee assigned" && notification.Message.Contains("Library Fee"));
        Assert.Contains(parentNotifications, notification => notification.Title == "New fee assigned" && notification.Message.Contains("Library Fee"));
    }

    [Fact]
    public async Task PaymentReceived_SendsNotificationToStudentAndParent()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, parentClient, studentClient, student) = await SeedParentAndStudentAsync("PAY");
        using var _ = adminClient;
        using var __ = parentClient;
        using var ___ = studentClient;

        var fee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(
                student.Id,
                "Sports Fee",
                100m,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                FeeStatus.Pending));

        await adminClient.AddPaymentAsync(
            new CreatePaymentRequest(fee.Id, 100m, DateTime.UtcNow, PaymentMethod.Cash, "PAY-NOTIF"));

        var studentNotifications = await GetNotificationsAsync(studentClient);
        var parentNotifications = await GetNotificationsAsync(parentClient);

        Assert.Contains(studentNotifications, notification => notification.Title == "Payment received" && notification.Message.Contains("Sports Fee"));
        Assert.Contains(parentNotifications, notification => notification.Title == "Payment received" && notification.Message.Contains("Sports Fee"));
    }

    [Fact]
    public async Task NotifyOverdue_MarksFeesAndSendsNotifications()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, parentClient, studentClient, student) = await SeedParentAndStudentAsync("OVERDUE");
        using var _ = adminClient;
        using var __ = parentClient;
        using var ___ = studentClient;

        var fee = await adminClient.CreateFeeAsync(
            new CreateFeeRequest(
                student.Id,
                "Overdue Test Fee",
                200m,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                FeeStatus.Pending));

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.Fees.FindAsync(fee.Id);
            entity!.DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
            await db.SaveChangesAsync();
        });

        var response = await adminClient.PostAsync("/api/fees/notify-overdue", null);
        response.EnsureSuccessStatusCode();

        var studentNotifications = await GetNotificationsAsync(studentClient);
        var parentNotifications = await GetNotificationsAsync(parentClient);

        Assert.Contains(studentNotifications, notification => notification.Title == "Fee overdue" && notification.Message.Contains("Overdue Test Fee"));
        Assert.Contains(parentNotifications, notification => notification.Title == "Fee overdue" && notification.Message.Contains("Overdue Test Fee"));
    }

    [Fact]
    public async Task Parent_ChildFilter_ReturnsOnlyChildSpecificNotifications()
    {
        await _factory.ResetDatabaseAsync();

        // Create two separate parent-child pairs
        var adminClient = await _factory.CreateAuthenticatedClientAsync();
        using var _admin = adminClient;

        var parent1 = await adminClient.CreateParentAsync(
            new CreateParentRequest("Filter Parent A", "filter.parent.a@school.com", "Parent@123", null, null, "Engineer"));
        var child1 = await adminClient.CreateStudentAsync(
            new CreateStudentRequest("Filter Child A", "filter.child.a@school.com", "Student@123",
                null, null, "FCA-001", new DateOnly(2012, 1, 1), Gender.Female, new DateOnly(2024, 9, 1), parent1.Id, null));
        var child2 = await adminClient.CreateStudentAsync(
            new CreateStudentRequest("Filter Child B", "filter.child.b@school.com", "Student@123",
                null, null, "FCB-001", new DateOnly(2012, 6, 1), Gender.Male, new DateOnly(2024, 9, 1), parent1.Id, null));

        using var parentClient = await _factory.CreateAuthenticatedClientAsync("filter.parent.a@school.com", "Parent@123");

        // Assign a fee to child1 — triggers a parent notification with StudentId = child1.Id
        await adminClient.CreateFeeAsync(
            new CreateFeeRequest(child1.Id, "Child A Fee", 50m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), FeeStatus.Pending));

        // Assign a fee to child2 — triggers a parent notification with StudentId = child2.Id
        await adminClient.CreateFeeAsync(
            new CreateFeeRequest(child2.Id, "Child B Fee", 75m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), FeeStatus.Pending));

        // Aggregated (all): parent sees both
        var allNotifications = await GetNotificationsAsync(parentClient);
        Assert.Contains(allNotifications, n => n.Title == "New fee assigned" && n.Message.Contains("Child A Fee"));
        Assert.Contains(allNotifications, n => n.Title == "New fee assigned" && n.Message.Contains("Child B Fee"));

        // Per-child filter: only child1's notifications
        var child1Response = await parentClient.GetAsync(
            $"/api/notifications?pageNumber=1&pageSize=50&studentId={child1.Id}");
        child1Response.EnsureSuccessStatusCode();
        var child1Payload = await child1Response.ReadApiResponseAsync<PagedResponse<NotificationResponse>>();

        Assert.Contains(child1Payload.Data!.Items, n => n.Title == "New fee assigned" && n.Message.Contains("Child A Fee"));
        Assert.DoesNotContain(child1Payload.Data.Items, n => n.Message.Contains("Child B Fee"));

        // Per-child filter: only child2's notifications
        var child2Response = await parentClient.GetAsync(
            $"/api/notifications?pageNumber=1&pageSize=50&studentId={child2.Id}");
        child2Response.EnsureSuccessStatusCode();
        var child2Payload = await child2Response.ReadApiResponseAsync<PagedResponse<NotificationResponse>>();

        Assert.Contains(child2Payload.Data!.Items, n => n.Title == "New fee assigned" && n.Message.Contains("Child B Fee"));
        Assert.DoesNotContain(child2Payload.Data.Items, n => n.Message.Contains("Child A Fee"));
    }

    [Fact]
    public async Task Parent_ChildNotification_IncludesStudentNameInResponse()
    {
        await _factory.ResetDatabaseAsync();
        var (adminClient, parentClient, _, student) = await SeedParentAndStudentAsync("CTXNAME");
        using var _a = adminClient;
        using var _p = parentClient;

        await adminClient.CreateFeeAsync(
            new CreateFeeRequest(student.Id, "Context Fee", 30m,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), FeeStatus.Pending));

        var notifications = await GetNotificationsAsync(parentClient);
        var feeNotification = notifications.FirstOrDefault(n => n.Title == "New fee assigned" && n.StudentId.HasValue);

        Assert.NotNull(feeNotification);
        Assert.Equal(student.Id, feeNotification.StudentId);
        Assert.False(string.IsNullOrEmpty(feeNotification.StudentName));
    }

    private async Task<(HttpClient Admin, HttpClient StudentClient, StudentResponse StudentRecord)> SeedStudentAsync(string suffix)
    {
        var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(
            new CreateStudentRequest(
                $"Notif Student {suffix}",
                $"notif.student.{suffix}@school.com",
                "Student@123",
                null,
                null,
                $"NS-{suffix}",
                new DateOnly(2010, 1, 1),
                Gender.Male,
                new DateOnly(2024, 9, 1),
                null,
                null));

        var studentClient = await _factory.CreateAuthenticatedClientAsync(
            $"notif.student.{suffix}@school.com",
            "Student@123");

        return (adminClient, studentClient, student);
    }

    private async Task<(HttpClient Admin, HttpClient ParentClient, HttpClient StudentClient, StudentResponse StudentRecord)>
        SeedParentAndStudentAsync(string suffix)
    {
        var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var parent = await adminClient.CreateParentAsync(
            new CreateParentRequest(
                $"Notif Parent {suffix}",
                $"notif.parent.{suffix}@school.com",
                "Parent@123",
                null,
                null,
                "Accountant"));

        var student = await adminClient.CreateStudentAsync(
            new CreateStudentRequest(
                $"Notif Child {suffix}",
                $"notif.child.{suffix}@school.com",
                "Student@123",
                null,
                null,
                $"NC-{suffix}",
                new DateOnly(2011, 3, 3),
                Gender.Female,
                new DateOnly(2024, 9, 1),
                parent.Id,
                null));

        var parentClient = await _factory.CreateAuthenticatedClientAsync(
            $"notif.parent.{suffix}@school.com",
            "Parent@123");
        var studentClient = await _factory.CreateAuthenticatedClientAsync(
            $"notif.child.{suffix}@school.com",
            "Student@123");

        return (adminClient, parentClient, studentClient, student);
    }

    private static async Task<List<NotificationResponse>> GetNotificationsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/notifications?pageNumber=1&pageSize=50");
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<PagedResponse<NotificationResponse>>();
        return payload.Data!.Items.ToList();
    }

    private static async Task AssignStudentToClassAsync(HttpClient client, StudentResponse student, Guid classId)
    {
        var response = await client.PutAsJsonAsync(
            $"/api/students/{student.Id}",
            new UpdateStudentRequest(
                student.FullName,
                student.Email,
                student.Phone,
                null,
                student.StudentCode,
                student.DateOfBirth,
                student.Gender,
                student.AdmissionDate,
                student.ParentId,
                classId));
        response.EnsureSuccessStatusCode();
    }

    private static async Task<TeacherResponse> CreateTeacherAsync(HttpClient client, string email, string code)
    {
        var response = await client.PostAsJsonAsync(
            "/api/teachers",
            new CreateTeacherRequest(
                $"Teacher {code}",
                email,
                "Teacher@123",
                null,
                null,
                code,
                "Science",
                new DateOnly(2022, 9, 1)));
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<TeacherResponse>();
        return payload.Data!;
    }

    private static async Task<SubjectResponse> CreateSubjectAsync(HttpClient client, string name, string code)
    {
        var response = await client.PostAsJsonAsync(
            "/api/subjects",
            new CreateSubjectRequest(name, code, null));
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<SubjectResponse>();
        return payload.Data!;
    }

    private static async Task<ClassResponse> CreateClassAsync(HttpClient client, string name, Guid teacherId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/classes",
            new CreateClassRequest(name, "A", "2025/2026", teacherId));
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<ClassResponse>();
        return payload.Data!;
    }

    private static async Task<TimetableEntryResponse> CreateTimetableEntryAsync(HttpClient client, CreateTimetableEntryRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/timetable", request);
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<TimetableEntryResponse>();
        return payload.Data!;
    }

    private static async Task<ExamResponse> CreateExamAsync(HttpClient client, Guid classId, Guid subjectId, string title)
    {
        var response = await client.PostAsJsonAsync(
            "/api/exams",
            new CreateExamRequest(classId, subjectId, title, new DateOnly(2026, 7, 1), 100m));
        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<ExamResponse>();
        return payload.Data!;
    }
}
