using System.Security.Cryptography;
using System.Text;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.API.Common;

public sealed class CookieCsrfMiddleware
{
    private readonly RequestDelegate _next;

    public CookieCsrfMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequiresValidation(context.Request))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Cookies.TryGetValue(AuthCookieNames.CsrfToken, out var cookieToken) ||
            string.IsNullOrWhiteSpace(cookieToken) ||
            !context.Request.Headers.TryGetValue("X-CSRF-Token", out var headerValues) ||
            string.IsNullOrWhiteSpace(headerValues.ToString()) ||
            !TokensMatch(cookieToken, headerValues.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail(
                    "CSRF validation failed.",
                    traceId: context.TraceIdentifier));
            return;
        }

        await _next(context);
    }

    private static bool RequiresValidation(HttpRequest request)
    {
        if (!request.Path.StartsWithSegments("/api"))
        {
            return false;
        }

        if (HttpMethods.IsGet(request.Method) ||
            HttpMethods.IsHead(request.Method) ||
            HttpMethods.IsOptions(request.Method) ||
            HttpMethods.IsTrace(request.Method))
        {
            return false;
        }

        if (request.Headers.ContainsKey("Authorization"))
        {
            return false;
        }

        if (!request.Cookies.ContainsKey(AuthCookieNames.AccessToken) &&
            !request.Cookies.ContainsKey(AuthCookieNames.RefreshToken))
        {
            return false;
        }

        return !IsAnonymousAuthPath(request.Path);
    }

    private static bool IsAnonymousAuthPath(PathString path)
    {
        return path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
               path.Equals("/api/auth/forgot-password", StringComparison.OrdinalIgnoreCase) ||
               path.Equals("/api/auth/reset-password", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TokensMatch(string cookieToken, string headerToken)
    {
        var left = Encoding.UTF8.GetBytes(cookieToken);
        var right = Encoding.UTF8.GetBytes(headerToken);

        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }
}
