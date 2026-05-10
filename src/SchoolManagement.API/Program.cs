using System.Text;
using System.Security.Claims;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SchoolManagement.API.Common;
using SchoolManagement.API.Extensions;
using SchoolManagement.API.Realtime;
using SchoolManagement.API.Realtime.Hubs;
using SchoolManagement.Persistence.Services;
using SchoolManagement.Application.AI;
using SchoolManagement.Application.Authentication;
using SchoolManagement.Application.Attendance;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Notifications;
using SchoolManagement.Infrastructure.AI;
using SchoolManagement.Infrastructure.Authentication;
using SchoolManagement.Infrastructure.DependencyInjection;
using SchoolManagement.Persistence.DependencyInjection;
using Serilog;
using Serilog.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

builder.Services.AddControllers();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(LoginRequestValidator).Assembly);
builder.Services.AddScoped<IAuthCookieService, AuthCookieService>();
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();
builder.Services.AddSingleton<IAttendanceRealtimeNotifier, SignalRAttendanceNotifier>();
builder.Services.AddSingleton<INotificationRealtimeNotifier, SignalRNotificationNotifier>();

// Add memory cache for rate limiting
builder.Services.AddMemoryCache();

// Add rate limiting configuration
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection("RateLimiting"));
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddSingleton<IAuditInitializationState, AuditInitializationState>();
builder.Services.Configure<AuditOptions>(builder.Configuration.GetSection("Audit"));
builder.Services.Configure<AuditCleanupOptions>(builder.Configuration.GetSection("Audit:Cleanup"));
builder.Services.AddHostedService<AuditLogCleanupService>();
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("AllowedOrigins")
        .Get<string[]>() ?? [];

    if (allowedOrigins.Length == 0 && builder.Environment.IsDevelopment())
    {
        allowedOrigins = ["http://localhost:3000"];
    }

    StartupConfigurationValidator.ValidateAllowedOrigins(allowedOrigins, builder.Environment.IsDevelopment());

    options.AddPolicy("AllowFrontend", policy =>
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition"));
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                x => x.Key,
                x => x.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

        return new BadRequestObjectResult(ApiResponse<object>.Fail("Validation failed", errors, context.HttpContext.TraceIdentifier));
    };
});

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
var passwordResetSettings = builder.Configuration.GetSection(PasswordResetSettings.SectionName).Get<PasswordResetSettings>() ?? new PasswordResetSettings();
var openAiSettings = builder.Configuration.GetSection(OpenAISettings.SectionName).Get<OpenAISettings>() ?? new OpenAISettings();
StartupConfigurationValidator.ValidateJwtSettings(jwtSettings, builder.Environment.IsDevelopment());
StartupConfigurationValidator.ValidateDatabaseInitialization(builder.Configuration, builder.Environment.IsProduction());
StartupConfigurationValidator.ValidatePasswordResetSettings(passwordResetSettings, builder.Environment.IsDevelopment());
StartupConfigurationValidator.ValidateOpenAISettings(openAiSettings, builder.Environment.IsDevelopment());

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrWhiteSpace(context.Token) &&
                    context.Request.Cookies.TryGetValue(AuthCookieNames.AccessToken, out var accessToken))
                {
                    context.Token = accessToken;
                }

                if (string.IsNullOrWhiteSpace(context.Token) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs") &&
                    context.Request.Query.TryGetValue("access_token", out var hubAccessToken))
                {
                    context.Token = hubAccessToken;
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtBearer");
                logger.LogWarning(context.Exception, "JWT authentication failed. TraceId: {TraceId}", context.HttpContext.TraceIdentifier);
                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                if (context.Response.HasStarted)
                {
                    return;
                }

                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    ApiResponse<object>.Fail(
                        "Authentication is required to access this resource.",
                        traceId: context.HttpContext.TraceIdentifier));
            },
            OnForbidden = async context =>
            {
                if (context.Response.HasStarted)
                {
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    ApiResponse<object>.Fail(
                        "You do not have permission to access this resource.",
                        traceId: context.HttpContext.TraceIdentifier));
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "School Management API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT bearer token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

app.Use(async (context, next) =>
{
    using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
    {
        await next();
    }
});

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserId", httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous");
    };
});

// Add security headers middleware (first in pipeline)
app.UseCors("AllowFrontend");
app.UseMiddleware<SecurityHeadersMiddleware>();

var initializationState = app.Services.GetRequiredService<IAuditInitializationState>();

app.Use(async (context, next) =>
{
    if (initializationState.IsReady ||
        context.Request.Path.StartsWithSegments("/health") ||
        context.Request.Path.StartsWithSegments("/live") ||
        context.Request.Path.StartsWithSegments("/hubs"))
    {
        await next();
        return;
    }

    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    context.Response.Headers.RetryAfter = "5";
    await context.Response.WriteAsJsonAsync(new
    {
        status = "starting",
        message = "Database initialization is still in progress."
    });
});

// Add rate limiting middleware
app.UseMiddleware<RateLimitingMiddleware>();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseStaticFiles();
app.UseMiddleware<CookieCsrfMiddleware>();
app.UseAuthentication();
app.UseMiddleware<AuditLoggingMiddleware>();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Ensure the upload directories exist at startup
var uploadRoot = Path.Combine(app.Environment.WebRootPath
    ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
Directory.CreateDirectory(Path.Combine(uploadRoot, "profile-pictures"));
Directory.CreateDirectory(Path.Combine(uploadRoot, "student-documents"));

app.MapControllers();
app.MapHub<AttendanceHub>("/hubs/attendance");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapGet("/live", () => Results.Ok(new { status = "live" })).AllowAnonymous();
app.MapGet("/health", (IAuditInitializationState state) =>
    state.IsReady
        ? Results.Ok(new { status = "ready" })
        : Results.Json(
            new { status = "starting", message = "Database initialization is still in progress." },
            statusCode: StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous();

// Start the HTTP server *before* running migrations so the /health endpoint
// responds during the (potentially long) database initialisation phase.
startupLogger.LogInformation("Starting HTTP server.");
await app.StartAsync();
foreach (var url in app.Urls)
{
    startupLogger.LogInformation("Now listening on: {Url}", url);
}
startupLogger.LogInformation("HTTP server started. Beginning database initialization.");
await app.InitialiseDatabaseAsync();
startupLogger.LogInformation("Database initialization finished. Waiting for shutdown.");
await app.WaitForShutdownAsync();

public partial class Program;
