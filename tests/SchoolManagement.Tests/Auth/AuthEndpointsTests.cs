using System.Net;
using System.Net.Http.Json;
using SchoolManagement.API.Common;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Authentication;
using SchoolManagement.Tests.Common;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests.Auth;

public sealed class AuthEndpointsTests : IClassFixture<SchoolManagementApiFactory>
{
    private readonly SchoolManagementApiFactory _factory;

    public AuthEndpointsTests(SchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAuthenticatedUser_AndSetsHttpOnlyCookies()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(SchoolManagementApiFactory.AdminEmail, SchoolManagementApiFactory.AdminPassword));

        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var payload = await response.ReadApiResponseAsync<AuthenticatedUserDto>();
        Assert.NotNull(payload.Data);
        Assert.Equal(SchoolManagementApiFactory.AdminEmail, payload.Data!.Email);

        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToArray();
        Assert.Contains(setCookieHeaders, value => value.StartsWith($"{AuthCookieNames.AccessToken}=", StringComparison.Ordinal) && value.Contains("HttpOnly", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(setCookieHeaders, value => value.StartsWith($"{AuthCookieNames.RefreshToken}=", StringComparison.Ordinal) && value.Contains("HttpOnly", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(setCookieHeaders, value => value.StartsWith($"{AuthCookieNames.CsrfToken}=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(SchoolManagementApiFactory.AdminEmail, "WrongPassword@123"));

        await response.AssertStatusCodeAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithValidCookie_RotatesRefreshCookie()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(SchoolManagementApiFactory.AdminEmail, SchoolManagementApiFactory.AdminPassword));
        loginResponse.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Remove("X-CSRF-Token");
        client.DefaultRequestHeaders.Add("X-CSRF-Token", ExtractCookieValue(loginResponse, AuthCookieNames.CsrfToken));
        var previousRefreshCookie = ExtractCookieValue(loginResponse, AuthCookieNames.RefreshToken);

        var response = await client.PostAsync("/api/auth/refresh", null);

        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<AuthenticatedUserDto>();
        Assert.Equal(SchoolManagementApiFactory.AdminEmail, payload.Data!.Email);
        Assert.NotEqual(previousRefreshCookie, ExtractCookieValue(response, AuthCookieNames.RefreshToken));
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/auth/refresh", null);

        await response.AssertStatusCodeAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Session_UsesCookieBasedAuthentication()
    {
        await _factory.ResetDatabaseAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/auth/session");

        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<AuthenticatedUserDto>();
        Assert.Equal(SchoolManagementApiFactory.AdminEmail, payload.Data!.Email);
    }

    [Fact]
    public async Task Logout_ClearsSessionCookies()
    {
        await _factory.ResetDatabaseAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var logoutResponse = await client.PostAsync("/api/auth/logout", null);
        logoutResponse.EnsureSuccessStatusCode();

        var sessionResponse = await client.GetAsync("/api/auth/session");
        await sessionResponse.AssertStatusCodeAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ForgotPassword_ReturnsGenericSuccessResponse()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequest("unknown.user@school.com"));

        response.EnsureSuccessStatusCode();
        var payload = await response.ReadApiResponseAsync<ForgotPasswordResponse>();
        Assert.Equal("If an account exists for this email, a password reset link has been sent.", payload.Data!.Message);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_Works()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        var forgot = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequest(SchoolManagementApiFactory.AdminEmail));
        forgot.EnsureSuccessStatusCode();

        var token = _factory.GetCapturedResetToken(SchoolManagementApiFactory.AdminEmail);
        var reset = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new ResetPasswordRequest(token, "NewAdmin@12345", "NewAdmin@12345"));
        reset.EnsureSuccessStatusCode();

        var oldLogin = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(SchoolManagementApiFactory.AdminEmail, SchoolManagementApiFactory.AdminPassword));
        await oldLogin.AssertStatusCodeAsync(HttpStatusCode.Unauthorized);

        var newLogin = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(SchoolManagementApiFactory.AdminEmail, "NewAdmin@12345"));
        newLogin.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_Fails()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new ResetPasswordRequest("invalid-token", "Another@12345", "Another@12345"));

        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WithExpiredToken_Fails()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        var forgot = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequest(SchoolManagementApiFactory.AdminEmail));
        forgot.EnsureSuccessStatusCode();

        var token = _factory.GetCapturedResetToken(SchoolManagementApiFactory.AdminEmail);
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var resetToken = await db.ResetTokens.SingleAsync();
            resetToken.ExpiresAt = DateTime.UtcNow.AddMinutes(-5);
            await db.SaveChangesAsync();
        });

        var response = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new ResetPasswordRequest(token, "Expired@12345", "Expired@12345"));

        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
    }

    private static string ExtractCookieValue(HttpResponseMessage response, string cookieName)
    {
        var prefix = $"{cookieName}=";
        var value = response.Headers
            .GetValues("Set-Cookie")
            .Select((header) => header.Split(';', 2)[0])
            .Single((cookie) => cookie.StartsWith(prefix, StringComparison.Ordinal));

        return value[prefix.Length..];
    }
}
