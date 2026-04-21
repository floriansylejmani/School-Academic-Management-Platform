using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using SchoolManagement.Application.Authentication;
using SchoolManagement.Infrastructure.Authentication;

namespace SchoolManagement.API.Common;

public interface IAuthCookieService
{
    void AppendSessionCookies(HttpResponse response, AuthResponse session);
    void ClearSessionCookies(HttpResponse response);
}

public sealed class AuthCookieService : IAuthCookieService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IWebHostEnvironment _environment;

    public AuthCookieService(IOptions<JwtSettings> jwtOptions, IWebHostEnvironment environment)
    {
        _jwtSettings = jwtOptions.Value;
        _environment = environment;
    }

    public void AppendSessionCookies(HttpResponse response, AuthResponse session)
    {
        response.Cookies.Append(
            AuthCookieNames.AccessToken,
            session.AccessToken,
            BuildCookieOptions(httpOnly: true, expiresAt: DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes)));

        response.Cookies.Append(
            AuthCookieNames.RefreshToken,
            session.RefreshToken,
            BuildCookieOptions(httpOnly: true, expiresAt: DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)));

        response.Cookies.Append(
            AuthCookieNames.CsrfToken,
            GenerateBrowserToken(),
            BuildCookieOptions(httpOnly: false, expiresAt: DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)));
    }

    public void ClearSessionCookies(HttpResponse response)
    {
        response.Cookies.Delete(AuthCookieNames.AccessToken, BuildDeletionOptions());
        response.Cookies.Delete(AuthCookieNames.RefreshToken, BuildDeletionOptions());
        response.Cookies.Delete(AuthCookieNames.CsrfToken, BuildDeletionOptions());
    }

    private CookieOptions BuildCookieOptions(bool httpOnly, DateTimeOffset expiresAt)
    {
        return new CookieOptions
        {
            HttpOnly = httpOnly,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = expiresAt,
            Path = "/"
        };
    }

    private CookieOptions BuildDeletionOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = "/"
        };
    }

    private static string GenerateBrowserToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
