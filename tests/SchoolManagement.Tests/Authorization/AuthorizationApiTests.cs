using System.Net;
using System.Net.Http.Json;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Parents;
using SchoolManagement.Application.Students;
using SchoolManagement.Application.Teachers;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Tests.Common;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests.Authorization;

public sealed class AuthorizationApiTests : IClassFixture<SchoolManagementApiFactory>
{
    private readonly SchoolManagementApiFactory _factory;

    public AuthorizationApiTests(SchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Teacher", "teacher.adminonly@school.com", "Teacher@123")]
    [InlineData("Student", "student.adminonly@school.com", "Student@123")]
    [InlineData("Parent", "parent.adminonly@school.com", "Parent@123")]
    public async Task AdminOnlyEndpoints_RejectNonAdminRoles(string role, string email, string password)
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        if (role == "Parent")
        {
            await adminClient.CreateParentAsync(new CreateParentRequest("Parent User", email, password, null, null, null));
        }
        else if (role == "Student")
        {
            await adminClient.CreateStudentAsync(
                new CreateStudentRequest("Student User", email, password, null, null, "ST-200", new DateOnly(2010, 1, 1), Gender.Male, new DateOnly(2024, 9, 1), null, null));
        }
        else
        {
            await _factory.SeedUserAsync(role, email, password, $"{role} User");
        }

        using var roleClient = await _factory.CreateAuthenticatedClientAsync(email, password);
        var response = await roleClient.GetAsync("/api/fees");

        await response.AssertStatusCodeAsync(HttpStatusCode.Forbidden);
        var payload = await response.ReadApiResponseAsync<object>();
        Assert.False(payload.Success);
        Assert.Equal("You do not have permission to access this resource.", payload.Message);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
    }

    [Fact]
    public async Task Parent_CanAccessOnlyLinkedChildData()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var parent = await adminClient.CreateParentAsync(
            new CreateParentRequest("Parent Linked", "parent.linked@school.com", "Parent@123", null, null, null));
        var anotherParent = await adminClient.CreateParentAsync(
            new CreateParentRequest("Parent Other", "parent.other@school.com", "Parent@123", null, null, null));

        var ownChild = await adminClient.CreateStudentAsync(
            new CreateStudentRequest("Own Child", "own.child.auth@school.com", "Student@123", null, null, "ST-210", new DateOnly(2010, 1, 1), Gender.Male, new DateOnly(2024, 9, 1), parent.Id, null));
        var otherChild = await adminClient.CreateStudentAsync(
            new CreateStudentRequest("Other Child", "other.child.auth@school.com", "Student@123", null, null, "ST-211", new DateOnly(2010, 1, 1), Gender.Female, new DateOnly(2024, 9, 1), anotherParent.Id, null));

        using var parentClient = await _factory.CreateAuthenticatedClientAsync("parent.linked@school.com", "Parent@123");

        var ownParentResponse = await parentClient.GetAsync($"/api/parents/{parent.Id}");
        ownParentResponse.EnsureSuccessStatusCode();

        var otherParentResponse = await parentClient.GetAsync($"/api/parents/{anotherParent.Id}");
        await otherParentResponse.AssertStatusCodeAsync(HttpStatusCode.Forbidden);

        var ownChildrenResponse = await parentClient.GetAsync("/api/students/parent/me?pageNumber=1&pageSize=10");
        ownChildrenResponse.EnsureSuccessStatusCode();
        var ownChildren = await ownChildrenResponse.ReadApiResponseAsync<PagedResponse<StudentResponse>>();
        Assert.Equal(ownChild.Id, Assert.Single(ownChildren.Data!.Items).Id);

        var forbiddenChildAccess = await parentClient.GetAsync($"/api/fees/student/{otherChild.Id}");
        await forbiddenChildAccess.AssertStatusCodeAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Student_CanAccessOnlySelfData()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(
            new CreateStudentRequest("Student One", "student.one.self@school.com", "Student@123", null, null, "ST-220", new DateOnly(2010, 1, 1), Gender.Male, new DateOnly(2024, 9, 1), null, null));
        var otherStudent = await adminClient.CreateStudentAsync(
            new CreateStudentRequest("Student Two", "student.two.self@school.com", "Student@123", null, null, "ST-221", new DateOnly(2010, 1, 1), Gender.Female, new DateOnly(2024, 9, 1), null, null));

        using var studentClient = await _factory.CreateAuthenticatedClientAsync("student.one.self@school.com", "Student@123");

        var meResponse = await studentClient.GetAsync("/api/students/me");
        meResponse.EnsureSuccessStatusCode();
        var mePayload = await meResponse.ReadApiResponseAsync<StudentResponse>();
        Assert.Equal(student.Id, mePayload.Data!.Id);

        var ownResponse = await studentClient.GetAsync($"/api/students/{student.Id}");
        ownResponse.EnsureSuccessStatusCode();

        var forbiddenResponse = await studentClient.GetAsync($"/api/students/{otherStudent.Id}");
        await forbiddenResponse.AssertStatusCodeAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_CanAccessAllowedAcademicEndpoints_Only()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var createTeacherResponse = await adminClient.PostAsJsonAsync(
            "/api/teachers",
            new CreateTeacherRequest("Allowed Teacher", "teacher.allowed@school.com", "Teacher@123", null, null, "T-220", "Mathematics", new DateOnly(2022, 9, 1)));
        createTeacherResponse.EnsureSuccessStatusCode();

        using var teacherClient = await _factory.CreateAuthenticatedClientAsync("teacher.allowed@school.com", "Teacher@123");

        var studentsResponse = await teacherClient.GetAsync("/api/students?pageNumber=1&pageSize=10");
        studentsResponse.EnsureSuccessStatusCode();

        var createStudentResponse = await teacherClient.PostAsJsonAsync(
            "/api/students",
            new CreateStudentRequest("Blocked", "blocked.teacher@school.com", "Student@123", null, null, "ST-230", new DateOnly(2010, 1, 1), Gender.Other, new DateOnly(2024, 9, 1), null, null));
        await createStudentResponse.AssertStatusCodeAsync(HttpStatusCode.Forbidden);

        var feesResponse = await teacherClient.GetAsync("/api/fees");
        await feesResponse.AssertStatusCodeAsync(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("/api/students")]
    [InlineData("/api/fees")]
    [InlineData("/api/parents")]
    public async Task UnauthenticatedRequests_Return401(string url)
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        await response.AssertStatusCodeAsync(HttpStatusCode.Unauthorized);
        var payload = await response.ReadApiResponseAsync<object>();
        Assert.False(payload.Success);
        Assert.Equal("Authentication is required to access this resource.", payload.Message);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
    }
}
