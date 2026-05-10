using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using SchoolManagement.API.Common;
using SchoolManagement.Application.AI;
using SchoolManagement.Application.Authentication;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Students;
using SchoolManagement.Infrastructure.Authentication;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Persistence;
using SchoolManagement.Persistence.Seed;
using SchoolManagement.Tests.Common;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests.Security;

public sealed class SecurityIntegrationTests : IClassFixture<SchoolManagementApiFactory>
{
    private const string JwtIssuer = "SchoolManagement.Tests";
    private const string JwtAudience = "SchoolManagement.Tests.Client";
    private const string JwtSecretKey = "SchoolManagementTests_SecretKey_1234567890!";
    private readonly SchoolManagementApiFactory _factory;

    public SecurityIntegrationTests(SchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task Protected_endpoint_without_token_returns_401_envelope()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var response = await client.GetAsync("/api/students");

        await response.AssertStatusCodeAsync(HttpStatusCode.Unauthorized);
        var payload = await response.ReadApiResponseAsync<object>();
        Assert.False(payload.Success);
        Assert.Equal("Authentication is required to access this resource.", payload.Message);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task Protected_endpoint_with_invalid_token_returns_401()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var response = await client.GetAsync("/api/students");

        await response.AssertStatusCodeAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task Protected_endpoint_with_expired_token_returns_401()
    {
        await _factory.ResetDatabaseAsync();
        var adminUserId = await GetUserIdAsync(SchoolManagementApiFactory.AdminEmail);
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateJwt(adminUserId, SchoolManagementApiFactory.AdminEmail, "Admin", JwtSecretKey, DateTime.UtcNow.AddMinutes(-10)));

        var response = await client.GetAsync("/api/students");

        await response.AssertStatusCodeAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task Protected_endpoint_with_wrong_signing_key_returns_401()
    {
        await _factory.ResetDatabaseAsync();
        var adminUserId = await GetUserIdAsync(SchoolManagementApiFactory.AdminEmail);
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateJwt(adminUserId, SchoolManagementApiFactory.AdminEmail, "Admin", "WrongSecurityTests_SecretKey_1234567890!", DateTime.UtcNow.AddMinutes(10)));

        var response = await client.GetAsync("/api/students");

        await response.AssertStatusCodeAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task Protected_endpoint_with_valid_bearer_token_returns_200()
    {
        await _factory.ResetDatabaseAsync();
        using var loginClient = _factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(SchoolManagementApiFactory.AdminEmail, SchoolManagementApiFactory.AdminPassword));
        loginResponse.EnsureSuccessStatusCode();

        using var bearerClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        bearerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ExtractCookieValue(loginResponse, AuthCookieNames.AccessToken));

        var response = await bearerClient.GetAsync("/api/students?pageNumber=1&pageSize=10");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task Admin_can_access_admin_endpoint_and_teacher_is_forbidden()
    {
        await _factory.ResetDatabaseAsync();
        await _factory.SeedUserAsync("Teacher", "security.teacher@school.com", "Teacher@123", "Security Teacher");

        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var adminResponse = await adminClient.GetAsync("/api/users?pageNumber=1&pageSize=10");
        adminResponse.EnsureSuccessStatusCode();

        using var teacherClient = await _factory.CreateAuthenticatedClientAsync("security.teacher@school.com", "Teacher@123");
        var teacherResponse = await teacherClient.GetAsync("/api/users?pageNumber=1&pageSize=10");
        await teacherResponse.AssertStatusCodeAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task Student_and_parent_boundaries_return_forbidden_for_unrelated_data()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var parent = await adminClient.CreateParentAsync(
            new("Security Parent", "security.parent@school.com", "Parent@123", null, null, null));
        var otherParent = await adminClient.CreateParentAsync(
            new("Security Other Parent", "security.other.parent@school.com", "Parent@123", null, null, null));
        var ownChild = await adminClient.CreateStudentAsync(
            new("Security Own Child", "security.own.child@school.com", "Student@123", null, null, "SEC-ST-1", new DateOnly(2012, 1, 1), Gender.Other, new DateOnly(2024, 9, 1), parent.Id, null));
        var otherChild = await adminClient.CreateStudentAsync(
            new("Security Other Child", "security.other.child@school.com", "Student@123", null, null, "SEC-ST-2", new DateOnly(2012, 1, 1), Gender.Other, new DateOnly(2024, 9, 1), otherParent.Id, null));

        using var studentClient = await _factory.CreateAuthenticatedClientAsync("security.own.child@school.com", "Student@123");
        var ownProfileResponse = await studentClient.GetAsync("/api/students/me");
        ownProfileResponse.EnsureSuccessStatusCode();
        var otherStudentResponse = await studentClient.GetAsync($"/api/students/{otherChild.Id}");
        await otherStudentResponse.AssertStatusCodeAsync(HttpStatusCode.Forbidden);
        var studentAdminResponse = await studentClient.GetAsync("/api/users?pageNumber=1&pageSize=10");
        await studentAdminResponse.AssertStatusCodeAsync(HttpStatusCode.Forbidden);

        using var parentClient = await _factory.CreateAuthenticatedClientAsync("security.parent@school.com", "Parent@123");
        var ownChildrenResponse = await parentClient.GetAsync("/api/students/parent/me?pageNumber=1&pageSize=10");
        ownChildrenResponse.EnsureSuccessStatusCode();
        var ownChildren = await ownChildrenResponse.ReadApiResponseAsync<PagedResponse<StudentResponse>>();
        Assert.Equal(ownChild.Id, Assert.Single(ownChildren.Data!.Items).Id);
        var unrelatedFeeResponse = await parentClient.GetAsync($"/api/fees/student/{otherChild.Id}");
        await unrelatedFeeResponse.AssertStatusCodeAsync(HttpStatusCode.Forbidden);
        var parentAdminResponse = await parentClient.GetAsync("/api/users?pageNumber=1&pageSize=10");
        await parentAdminResponse.AssertStatusCodeAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task Cors_preflight_allows_configured_origin_only()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        using var allowedRequest = CreatePreflightRequest("http://localhost");
        var allowedResponse = await client.SendAsync(allowedRequest);
        Assert.Contains(allowedResponse.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.NoContent });
        Assert.True(allowedResponse.Headers.TryGetValues("Access-Control-Allow-Origin", out var allowedOrigins));
        Assert.Equal("http://localhost", Assert.Single(allowedOrigins));
        Assert.True(allowedResponse.Headers.TryGetValues("Access-Control-Allow-Credentials", out var credentialValues));
        Assert.Equal("true", Assert.Single(credentialValues));
        Assert.True(allowedResponse.Headers.TryGetValues("Access-Control-Allow-Methods", out var methodValues));
        Assert.Contains("POST", string.Join(",", methodValues), StringComparison.OrdinalIgnoreCase);

        using var disallowedRequest = CreatePreflightRequest("https://evil.example");
        var disallowedResponse = await client.SendAsync(disallowedRequest);
        Assert.False(disallowedResponse.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task Rate_limiting_can_be_enabled_and_returns_429_after_limit()
    {
        var limitedFactory = new RateLimitedSecurityApiFactory();
        await limitedFactory.InitializeAsync();

        try
        {
            using var client = limitedFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

            (await client.GetAsync("/live")).EnsureSuccessStatusCode();
            (await client.GetAsync("/live")).EnsureSuccessStatusCode();
            var limitedResponse = await client.GetAsync("/live");

            await limitedResponse.AssertStatusCodeAsync(HttpStatusCode.TooManyRequests);
            var payload = await limitedResponse.ReadApiResponseAsync<object>();
            Assert.False(payload.Success);
            Assert.Equal("Too many requests. Please try again later.", payload.Message);
        }
        finally
        {
            await limitedFactory.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task Default_test_factory_has_rate_limiting_disabled()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var response = await client.GetAsync("/live");

        response.EnsureSuccessStatusCode();
        Assert.False(response.Headers.Contains("X-RateLimit-Limit"));
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task Security_headers_are_added_to_responses()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/live");

        response.EnsureSuccessStatusCode();
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("strict-origin-when-cross-origin", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.Contains("default-src 'self'", Assert.Single(response.Headers.GetValues("Content-Security-Policy")));
        Assert.Contains("geolocation=()", Assert.Single(response.Headers.GetValues("Permissions-Policy")));
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task Sql_injection_like_login_input_does_not_bypass_authentication()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("' OR '1'='1", "anything"));

        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized });
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.ReadApiResponseAsync<object>();
        Assert.False(payload.Success);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task Xss_like_student_name_is_rejected_by_validation()
    {
        await _factory.ResetDatabaseAsync();
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await adminClient.PostAsJsonAsync(
            "/api/students",
            new CreateStudentRequest("<script>alert(1)</script>", "xss.student@school.com", "Student@123", null, null, "SEC-XSS-1", new DateOnly(2012, 1, 1), Gender.Other, new DateOnly(2024, 9, 1), null, null));

        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var payload = await response.ReadApiResponseAsync<object>();
        Assert.False(payload.Success);
        Assert.NotNull(payload.Errors);
        Assert.Contains("FullName", payload.Errors!.Keys);
    }

    private async Task<Guid> GetUserIdAsync(string email)
    {
        return await _factory.ExecuteDbContextAsync(async dbContext =>
            await dbContext.Users
                .Where(x => x.Email == email)
                .Select(x => x.Id)
                .SingleAsync());
    }

    private static string CreateJwt(Guid userId, string email, string role, string secretKey, DateTime expires)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims:
            [
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new(JwtRegisteredClaimNames.Name, email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new(ClaimTypes.Role, role),
                new(ClaimTypes.NameIdentifier, userId.ToString())
            ],
            notBefore: expires.AddMinutes(-5),
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static HttpRequestMessage CreatePreflightRequest(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        return request;
    }

    private static string ExtractCookieValue(HttpResponseMessage response, string cookieName)
    {
        var prefix = $"{cookieName}=";
        var value = response.Headers
            .GetValues("Set-Cookie")
            .Select(header => header.Split(';', 2)[0])
            .Single(cookie => cookie.StartsWith(prefix, StringComparison.Ordinal));

        return value[prefix.Length..];
    }

    private sealed class RateLimitedSecurityApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                    ["Database:AutoMigrate"] = "false",
                    ["Database:SeedDemoData"] = "false",
                    ["AllowedOrigins:0"] = "http://localhost",
                    ["PasswordReset:FrontendResetUrl"] = "http://localhost/reset-password",
                    ["Jwt:Issuer"] = JwtIssuer,
                    ["Jwt:Audience"] = JwtAudience,
                    ["Jwt:SecretKey"] = JwtSecretKey,
                    ["RateLimiting:Enabled"] = "true",
                    ["RateLimiting:DefaultLimit"] = "2",
                    ["RateLimiting:DefaultWindowSeconds"] = "60",
                    ["RateLimiting:AuthLimit"] = "2",
                    ["RateLimiting:AuthWindowSeconds"] = "60"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.RemoveAll<IPasswordResetNotifier>();
                services.RemoveAll<IAIGradingService>();

                services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
                services.AddSingleton<IPasswordResetNotifier, CapturedPasswordResetNotifier>();
                services.AddSingleton<IAIGradingService, CapturedAIGradingService>();
                services.PostConfigure<RateLimitingOptions>(options =>
                {
                    options.Enabled = true;
                    options.DefaultLimit = 2;
                    options.DefaultWindowSeconds = 60;
                    options.AuthLimit = 2;
                    options.AuthWindowSeconds = 60;
                });
                services.PostConfigure<JwtSettings>(settings =>
                {
                    settings.Issuer = JwtIssuer;
                    settings.Audience = JwtAudience;
                    settings.SecretKey = JwtSecretKey;
                });
            });
        }

        public async Task InitializeAsync()
        {
            await _connection.OpenAsync();
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
            await DataSeeder.SeedAsync(dbContext, passwordHasher, CancellationToken.None);
        }

        public new async Task DisposeAsync()
        {
            await base.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
