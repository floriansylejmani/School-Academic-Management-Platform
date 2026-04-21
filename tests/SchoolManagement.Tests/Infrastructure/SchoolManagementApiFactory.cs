using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SchoolManagement.API.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SchoolManagement.Application.AI;
using SchoolManagement.Application.Authentication;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Infrastructure.Authentication;
using SchoolManagement.Persistence;
using SchoolManagement.Persistence.Seed;

namespace SchoolManagement.Tests.Infrastructure;

public sealed class SchoolManagementApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@school.com";
    public const string AdminPassword = "Admin@12345";
    private const string JwtIssuer = "SchoolManagement.Tests";
    private const string JwtAudience = "SchoolManagement.Tests.Client";
    private const string JwtSecretKey = "SchoolManagementTests_SecretKey_1234567890!";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly CapturedPasswordResetNotifier _passwordResetNotifier = new();
    private readonly CapturedAIGradingService _aiGradingService = new();

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
                ["Jwt:SecretKey"] = JwtSecretKey
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
            services.AddSingleton(_passwordResetNotifier);
            services.AddSingleton<IPasswordResetNotifier>(_passwordResetNotifier);
            services.AddSingleton(_aiGradingService);
            services.AddSingleton<IAIGradingService>(_aiGradingService);
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
        await _connection.OpenAsync();
        await ResetDatabaseAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        _passwordResetNotifier.Clear();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        await DataSeeder.SeedAsync(dbContext, passwordHasher, CancellationToken.None);
    }

    public async Task<ApiResponse<AuthenticatedUserDto>> LoginAsync(HttpClient client, string email, string password)
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

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email = AdminEmail, string password = AdminPassword)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        await LoginAsync(client, email, password);
        return client;
    }

    public async Task SeedUserAsync(string roleName, string email, string password, string fullName)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var role = await dbContext.Roles.SingleAsync(x => x.Name == roleName);

        dbContext.Users.Add(new User
        {
            RoleId = role.Id,
            FullName = fullName,
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHasher.HashPassword(password),
            IsActive = true
        });

        await dbContext.SaveChangesAsync();
    }

    public async Task ExecuteDbContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(dbContext);
    }

    public async Task<T> ExecuteDbContextAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(dbContext);
    }

    public string GetCapturedResetToken(string email) => _passwordResetNotifier.GetToken(email);
    public CapturedAIGradingService AIGradingService => _aiGradingService;

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
