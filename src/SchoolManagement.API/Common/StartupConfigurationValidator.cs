using Microsoft.Extensions.Configuration;
using SchoolManagement.Application.Authentication;
using SchoolManagement.Infrastructure.AI;
using SchoolManagement.Infrastructure.Authentication;

namespace SchoolManagement.API.Common;

public static class StartupConfigurationValidator
{
    private static readonly string[] DemoJwtSecretKeys =
    [
        JwtSettings.DefaultDevelopmentSecretKey,
        "ChangeMeForDemoOnly_AtLeast32Characters!"
    ];

    public static void ValidateAllowedOrigins(string[] allowedOrigins, bool isDevelopment)
    {
        if (allowedOrigins.Length == 0 && !isDevelopment)
        {
            throw new InvalidOperationException("AllowedOrigins must be configured outside Development.");
        }

        foreach (var origin in allowedOrigins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException($"AllowedOrigins contains an invalid origin: '{origin}'.");
            }

            if (!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/")
            {
                throw new InvalidOperationException($"AllowedOrigins entries must not contain a path: '{origin}'.");
            }

            if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            {
                throw new InvalidOperationException($"AllowedOrigins entries must not contain query or fragment values: '{origin}'.");
            }
        }
    }

    public static void ValidateJwtSettings(JwtSettings jwtSettings, bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer must be configured.");
        }

        if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
        {
            throw new InvalidOperationException("Jwt:Audience must be configured.");
        }

        if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey) || jwtSettings.SecretKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SecretKey must be at least 32 characters long.");
        }

        if (!isDevelopment && DemoJwtSecretKeys.Contains(jwtSettings.SecretKey, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Jwt:SecretKey must be changed for non-development environments.");
        }

        if (jwtSettings.AccessTokenExpiryMinutes is <= 0 or > 1440)
        {
            throw new InvalidOperationException("Jwt:AccessTokenExpiryMinutes must be between 1 and 1440.");
        }

        if (jwtSettings.RefreshTokenExpiryDays is <= 0 or > 30)
        {
            throw new InvalidOperationException("Jwt:RefreshTokenExpiryDays must be between 1 and 30.");
        }
    }

    public static void ValidateDatabaseInitialization(IConfiguration configuration, bool isProduction)
    {
        if (!isProduction)
        {
            return;
        }

        var seedDemoData = configuration.GetValue("Database:SeedDemoData", false);
        if (seedDemoData)
        {
            throw new InvalidOperationException("Database:SeedDemoData must be false in Production.");
        }

        var autoMigrate = configuration.GetValue("Database:AutoMigrate", false);
        var allowProductionAutoMigrate = configuration.GetValue("Database:AllowProductionAutoMigrate", false);
        if (autoMigrate && !allowProductionAutoMigrate)
        {
            throw new InvalidOperationException(
                "Database:AutoMigrate must be false in Production unless Database:AllowProductionAutoMigrate is explicitly set to true.");
        }
    }

    public static void ValidatePasswordResetSettings(PasswordResetSettings settings, bool isDevelopment)
    {
        if (settings.TokenExpiryMinutes is < 5 or > 240)
        {
            throw new InvalidOperationException("PasswordReset:TokenExpiryMinutes must be between 5 and 240.");
        }

        if (!Uri.TryCreate(settings.FrontendResetUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("PasswordReset:FrontendResetUrl must be a valid absolute HTTP or HTTPS URL.");
        }

        var isLocalhost = uri.Host is "localhost" or "127.0.0.1" || uri.Host.EndsWith(".localhost");
        if (!isDevelopment && !isLocalhost && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("PasswordReset:FrontendResetUrl must use HTTPS for non-localhost URLs outside Development.");
        }
    }

    public static void ValidateOpenAISettings(OpenAISettings settings, bool isDevelopment)
    {
        if (!settings.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("OpenAI:ApiKey must be configured when OpenAI is enabled.");
        }

        if (string.IsNullOrWhiteSpace(settings.Model))
        {
            throw new InvalidOperationException("OpenAI:Model must be configured when OpenAI is enabled.");
        }

        if (!Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("OpenAI:BaseUrl must be a valid absolute HTTPS URL.");
        }

        if (settings.TimeoutSeconds is < 5 or > 120)
        {
            throw new InvalidOperationException("OpenAI:TimeoutSeconds must be between 5 and 120.");
        }

        if (settings.MaxEssayCharacters is < 1000 or > 20000)
        {
            throw new InvalidOperationException("OpenAI:MaxEssayCharacters must be between 1000 and 20000.");
        }

        var allowedReasoningEfforts = new[] { "minimal", "low", "medium", "high" };
        if (!string.IsNullOrWhiteSpace(settings.ReasoningEffort) &&
            !allowedReasoningEfforts.Contains(settings.ReasoningEffort, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("OpenAI:ReasoningEffort must be minimal, low, medium, or high.");
        }

        if (!isDevelopment &&
            baseUri.Host is "localhost" or "127.0.0.1")
        {
            throw new InvalidOperationException("OpenAI:BaseUrl must not point to localhost outside Development.");
        }
    }
}
