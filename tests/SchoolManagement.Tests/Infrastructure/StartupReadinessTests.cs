using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.Authentication;
using SchoolManagement.Application.Common.Interfaces;

namespace SchoolManagement.Tests.Infrastructure;

public sealed class StartupReadinessTests : IClassFixture<SchoolManagementApiFactory>
{
    private readonly SchoolManagementApiFactory _factory;

    public StartupReadinessTests(SchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ReadinessEndpoints_ReflectInitializationState()
    {
        using var client = _factory.CreateClient();
        var initializationState = _factory.Services.GetRequiredService<IAuditInitializationState>();

        initializationState.IsReady = false;

        try
        {
            var liveResponse = await client.GetAsync("/live");
            var readinessResponse = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, readinessResponse.StatusCode);

            initializationState.IsReady = true;

            var readyResponse = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
        }
        finally
        {
            initializationState.IsReady = true;
        }
    }

    [Fact]
    public async Task Requests_ReturnServiceUnavailable_UntilInitializationCompletes()
    {
        using var client = _factory.CreateClient();
        var initializationState = _factory.Services.GetRequiredService<IAuditInitializationState>();

        initializationState.IsReady = false;

        try
        {
            var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(SchoolManagementApiFactory.AdminEmail, SchoolManagementApiFactory.AdminPassword));

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
        finally
        {
            initializationState.IsReady = true;
        }
    }
}
