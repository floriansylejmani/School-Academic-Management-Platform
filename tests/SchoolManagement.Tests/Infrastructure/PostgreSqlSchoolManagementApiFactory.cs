using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SchoolManagement.API.Common;
using SchoolManagement.Application.AI;
using SchoolManagement.Application.Authentication;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Infrastructure.Authentication;
using SchoolManagement.Persistence;
using Testcontainers.PostgreSql;

namespace SchoolManagement.Tests.Infrastructure;

public sealed class PostgreSqlSchoolManagementApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "postgres.admin@school.com";
    public const string AdminPassword = "Admin@12345";
    private const string JwtIssuer = "SchoolManagement.PostgreSql.Tests";
    private const string JwtAudience = "SchoolManagement.PostgreSql.Tests.Client";
    private const string JwtSecretKey = "SchoolManagementPostgreSqlTests_SecretKey_1234567890!";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("school_management_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly CapturedPasswordResetNotifier _passwordResetNotifier = new();
    private readonly CapturedAIGradingService _aiGradingService = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _container.GetConnectionString(),
                ["Database:AutoMigrate"] = "false",
                ["Database:SeedDemoData"] = "false",
                ["AllowedOrigins:0"] = "http://localhost",
                ["PasswordReset:FrontendResetUrl"] = "http://localhost/reset-password",
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:SecretKey"] = JwtSecretKey,
                ["RateLimiting:Enabled"] = "false",
                ["OpenAI:Enabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPasswordResetNotifier>();
            services.RemoveAll<IAIGradingService>();

            services.AddSingleton(_passwordResetNotifier);
            services.AddSingleton<IPasswordResetNotifier>(_passwordResetNotifier);
            services.AddSingleton(_aiGradingService);
            services.AddSingleton<IAIGradingService>(_aiGradingService);
            services.PostConfigure<RateLimitingOptions>(options => options.Enabled = false);
            services.PostConfigure<JwtSettings>(settings =>
            {
                settings.Issuer = JwtIssuer;
                settings.Audience = JwtAudience;
                settings.SecretKey = JwtSecretKey;
            });
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.ValidIssuer = JwtIssuer;
                options.TokenValidationParameters.ValidAudience = JwtAudience;
                options.TokenValidationParameters.IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecretKey));
            });
        });
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "PostgreSQL Testcontainers tests require Docker to be running and reachable. Start Docker Desktop or your Docker daemon, then rerun tests with --filter Category=PostgreSQL.",
                ex);
        }

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
        await ResetDatabaseAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await dbContext.Database.ExecuteSqlRawAsync("""
            DO $$
            DECLARE
                truncate_statement text;
            BEGIN
                SELECT string_agg(format('TRUNCATE TABLE %I.%I RESTART IDENTITY CASCADE', schemaname, tablename), '; ')
                INTO truncate_statement
                FROM pg_tables
                WHERE schemaname = 'public'
                  AND tablename <> '__EFMigrationsHistory';

                IF truncate_statement IS NOT NULL THEN
                    EXECUTE truncate_statement;
                END IF;
            END $$;
            """);

        var adminRole = new Role { Name = "Admin" };
        dbContext.Roles.AddRange(
            adminRole,
            new Role { Name = "Teacher" },
            new Role { Name = "Student" },
            new Role { Name = "Parent" });

        dbContext.Users.Add(new User
        {
            Role = adminRole,
            FullName = "PostgreSQL Admin",
            Email = AdminEmail,
            PasswordHash = passwordHasher.HashPassword(AdminPassword),
            IsActive = true
        });

        await dbContext.SaveChangesAsync();
    }

    public IServiceScope CreateDbContextScope() => Services.CreateScope();

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email = AdminEmail, string password = AdminPassword)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        await LoginAsync(client, email, password);
        return client;
    }

    private static async Task<ApiResponse<AuthenticatedUserDto>> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        if (TryGetCookieValue(response, AuthCookieNames.CsrfToken, out var csrfToken))
        {
            client.DefaultRequestHeaders.Remove("X-CSRF-Token");
            client.DefaultRequestHeaders.Add("X-CSRF-Token", csrfToken);
        }

        return await response.ReadRequiredJsonAsync<ApiResponse<AuthenticatedUserDto>>();
    }

    private static bool TryGetCookieValue(HttpResponseMessage response, string cookieName, out string cookieValue)
    {
        cookieValue = string.Empty;

        foreach (var header in response.Headers.TryGetValues("Set-Cookie", out var values) ? values : [])
        {
            var prefix = $"{cookieName}=";
            var segment = header.Split(';', 2)[0];
            if (!segment.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            cookieValue = segment[prefix.Length..];
            return true;
        }

        return false;
    }
}
