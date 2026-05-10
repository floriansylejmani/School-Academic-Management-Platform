using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Students;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Persistence;
using SchoolManagement.Tests.Common;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests.Pagination;

public sealed class PaginationApiTests : IClassFixture<SchoolManagementApiFactory>
{
    private readonly SchoolManagementApiFactory _factory;

    public PaginationApiTests(SchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Students_PageSize10_ReturnsTenItemsAndMetadata()
    {
        await _factory.ResetDatabaseAsync();
        await SeedStudentsAsync(105);
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync("/api/students?pageNumber=1&pageSize=10");

        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<PagedResponse<StudentResponse>>();
        Assert.Equal(1, payload.Data!.PageNumber);
        Assert.Equal(10, payload.Data.PageSize);
        Assert.Equal(10, payload.Data.Items.Count);
        Assert.True(payload.Data.TotalCount >= 105);
    }

    [Fact]
    public async Task Students_PageSize100_ReturnsOneHundredItemsAndMetadata()
    {
        await _factory.ResetDatabaseAsync();
        await SeedStudentsAsync(105);
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync("/api/students?pageNumber=1&pageSize=100");

        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<PagedResponse<StudentResponse>>();
        Assert.Equal(1, payload.Data!.PageNumber);
        Assert.Equal(100, payload.Data.PageSize);
        Assert.Equal(100, payload.Data.Items.Count);
        Assert.True(payload.Data.TotalCount >= 105);
    }

    [Fact]
    public async Task Students_PageSize101_ReturnsValidationError()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync("/api/students?pageNumber=1&pageSize=101");

        await AssertValidationErrorMentionsAsync(response, "PageSize");
    }

    [Fact]
    public async Task Students_PageNumberZero_ReturnsValidationError()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync("/api/students?pageNumber=0&pageSize=10");

        await AssertValidationErrorMentionsAsync(response, "PageNumber");
    }

    [Fact]
    public async Task Students_PageSizeZero_ReturnsValidationError()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync("/api/students?pageNumber=1&pageSize=0");

        await AssertValidationErrorMentionsAsync(response, "PageSize");
    }

    [Fact]
    public async Task Students_PageOneAndPageTwo_ReturnDifferentRecords()
    {
        await _factory.ResetDatabaseAsync();
        await SeedStudentsAsync(105);
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var pageOneResponse = await adminClient.GetAsync("/api/students?pageNumber=1&pageSize=10");
        var pageTwoResponse = await adminClient.GetAsync("/api/students?pageNumber=2&pageSize=10");

        pageOneResponse.EnsureSuccessStatusCode();
        pageTwoResponse.EnsureSuccessStatusCode();
        var pageOne = await pageOneResponse.ReadApiResponseAsync<PagedResponse<StudentResponse>>();
        var pageTwo = await pageTwoResponse.ReadApiResponseAsync<PagedResponse<StudentResponse>>();

        Assert.Equal(10, pageOne.Data!.Items.Count);
        Assert.Equal(10, pageTwo.Data!.Items.Count);
        Assert.Empty(pageOne.Data.Items.Select(x => x.Id).Intersect(pageTwo.Data.Items.Select(x => x.Id)));
    }

    [Fact]
    public async Task Fees_PageSize101_ReturnsValidationError()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync("/api/fees?pageNumber=1&pageSize=101");

        await AssertValidationErrorMentionsAsync(response, "PageSize");
    }

    private async Task SeedStudentsAsync(int count)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var studentRole = await dbContext.Roles.SingleAsync(x => x.Name == "Student");

        for (var index = 0; index < count; index++)
        {
            dbContext.Students.Add(new Student
            {
                User = new User
                {
                    RoleId = studentRole.Id,
                    FullName = $"Pagination Student {index:D3}",
                    Email = $"pagination.student.{index:D3}@school.com",
                    PasswordHash = passwordHasher.HashPassword("Student@123"),
                    IsActive = true
                },
                StudentCode = $"PG-{index:D3}",
                DateOfBirth = new DateOnly(2011, 1, 1).AddDays(index),
                Gender = Gender.Other,
                AdmissionDate = new DateOnly(2024, 9, 1)
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task AssertValidationErrorMentionsAsync(HttpResponseMessage response, string fieldName)
    {
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Validation failed", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(fieldName, body, StringComparison.OrdinalIgnoreCase);
    }
}
