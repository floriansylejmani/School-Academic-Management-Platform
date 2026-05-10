using System.Net;
using System.Net.Http.Json;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Students;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Tests.Common;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests.Students;

public sealed class StudentApiTests : IClassFixture<SchoolManagementApiFactory>
{
    private readonly SchoolManagementApiFactory _factory;

    public StudentApiTests(SchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_CanCreateStudent()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var student = await adminClient.CreateStudentAsync(BuildStudentRequest("student.create@school.com", "ST-001"));

        Assert.Equal("student.create@school.com", student.Email);
        Assert.Equal("ST-001", student.StudentCode);
    }

    [Fact]
    public async Task DuplicateEmail_IsRejected()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        await adminClient.CreateStudentAsync(BuildStudentRequest("duplicate.email@school.com", "ST-010"));
        var response = await adminClient.PostAsJsonAsync("/api/students", BuildStudentRequest("duplicate.email@school.com", "ST-011"));

        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DuplicateStudentCode_IsRejected()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        await adminClient.CreateStudentAsync(BuildStudentRequest("duplicate.code.1@school.com", "ST-020"));
        var response = await adminClient.PostAsJsonAsync("/api/students", BuildStudentRequest("duplicate.code.2@school.com", "ST-020"));

        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetStudentById_Works()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(BuildStudentRequest("student.get@school.com", "ST-030"));

        var response = await adminClient.GetAsync($"/api/students/{student.Id}");

        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<StudentResponse>();
        Assert.Equal(student.Id, payload.Data!.Id);
        Assert.Equal("student.get@school.com", payload.Data.Email);
    }

    [Fact]
    public async Task ListStudents_ReturnsPagedResult()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        await adminClient.CreateStudentAsync(BuildStudentRequest("student.list.1@school.com", "ST-040"));
        await adminClient.CreateStudentAsync(BuildStudentRequest("student.list.2@school.com", "ST-041"));

        var response = await adminClient.GetAsync("/api/students?pageNumber=1&pageSize=100");

        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<PagedResponse<StudentResponse>>();
        Assert.True(payload.Data!.TotalCount >= 2);
        Assert.Equal(1, payload.Data.PageNumber);
        Assert.Equal(100, payload.Data.PageSize);
        Assert.Contains(payload.Data.Items, x => x.Email == "student.list.1@school.com");
        Assert.Contains(payload.Data.Items, x => x.Email == "student.list.2@school.com");
    }

    [Fact]
    public async Task UpdateStudent_Works()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(BuildStudentRequest("student.update@school.com", "ST-050"));

        var response = await adminClient.PutAsJsonAsync(
            $"/api/students/{student.Id}",
            new UpdateStudentRequest(
                "Updated Student",
                "student.updated@school.com",
                "111222333",
                "Updated address",
                "ST-050",
                new DateOnly(2010, 5, 14),
                Gender.Female,
                new DateOnly(2024, 9, 1),
                null,
                null));

        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<StudentResponse>();
        Assert.Equal("Updated Student", payload.Data!.FullName);
        Assert.Equal("student.updated@school.com", payload.Data.Email);
    }

    [Fact]
    public async Task DeleteStudent_Works()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var student = await adminClient.CreateStudentAsync(BuildStudentRequest("student.delete@school.com", "ST-060"));

        var deleteResponse = await adminClient.DeleteAsync($"/api/students/{student.Id}");
        deleteResponse.EnsureSuccessStatusCode();

        var getResponse = await adminClient.GetAsync($"/api/students/{student.Id}");
        await getResponse.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Student_CannotAccessAnotherStudentsProtectedData()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var firstStudent = await adminClient.CreateStudentAsync(BuildStudentRequest("student.self.1@school.com", "ST-070"));
        var secondStudent = await adminClient.CreateStudentAsync(BuildStudentRequest("student.self.2@school.com", "ST-071"));

        using var studentClient = await _factory.CreateAuthenticatedClientAsync("student.self.1@school.com", "Student@123");

        var ownResponse = await studentClient.GetAsync($"/api/students/{firstStudent.Id}");
        ownResponse.EnsureSuccessStatusCode();

        var forbiddenResponse = await studentClient.GetAsync($"/api/students/{secondStudent.Id}");
        await forbiddenResponse.AssertStatusCodeAsync(HttpStatusCode.Forbidden);
    }

    private static CreateStudentRequest BuildStudentRequest(string email, string studentCode)
        => new(
            "Jane Student",
            email,
            "Student@123",
            "123456789",
            "Main street",
            studentCode,
            new DateOnly(2010, 5, 14),
            Gender.Female,
            new DateOnly(2024, 9, 1),
            null,
            null);
}
