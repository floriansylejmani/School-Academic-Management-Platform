using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Extensions;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.Common.Interfaces;

namespace SchoolManagement.API.Common;

public sealed class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    // Content types that should not be buffered for audit logging
    private static readonly HashSet<string> NonBufferableContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/octet-stream",
        "application/pdf",
        "image/",
        "video/",
        "audio/",
        "application/zip",
        "application/x-zip-compressed",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.",
        "application/vnd.ms-",
        "text/csv"
    };

    // File extensions that should not be buffered
    private static readonly HashSet<string> NonBufferableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".zip", ".rar", ".7z", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".mp4", ".avi", ".mov", ".mp3",
        ".wav", ".csv", ".txt", ".log"
    };

    public AuditLoggingMiddleware(
        RequestDelegate next,
        ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var shouldBufferResponse = ShouldBufferResponse(context);
        var originalResponseBodyStream = context.Response.Body;

        // Only buffer response if we actually need to inspect it
        if (shouldBufferResponse)
        {
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);
            }
            finally
            {
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                // Only read response body if we need it (currently we don't)
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalResponseBodyStream);

                await LogRequestAsync(context, startTime, duration, requestBody: null);
            }
        }
        else
        {
            // For large responses, files, etc., don't buffer
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            try
            {
                await _next(context);
            }
            finally
            {
                endTime = DateTime.UtcNow;
                duration = endTime - startTime;
                await LogRequestAsync(context, startTime, duration, requestBody: null);
            }
        }
    }

    private async Task LogRequestAsync(HttpContext context, DateTime startTime, TimeSpan duration, string? requestBody)
    {
        try
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var request = context.Request;
            var response = context.Response;

            var auditEvent = new AuditEvent
            {
                Timestamp = startTime,
                UserId = string.IsNullOrEmpty(userId) ? null : Guid.Parse(userId),
                UserEmail = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
                IpAddress = GetClientIpAddress(context),
                UserAgent = request.Headers["User-Agent"].ToString(),
                HttpMethod = request.Method,
                RequestPath = request.Path,
                QueryString = request.QueryString.ToString(),
                StatusCode = response.StatusCode,
                DurationMs = (long)duration.TotalMilliseconds,
                Success = response.StatusCode < 400,
                TraceId = context.TraceIdentifier
            };

            // Only mark as sensitive and capture request body for sensitive actions
            if (IsSensitiveAction(request.Path, request.Method))
            {
                auditEvent.IsSensitive = true;
                auditEvent.ActionType = DetermineActionType(request.Path, request.Method);

                // Only capture request body for sensitive actions that actually need it
                if (ShouldCaptureRequestBody(request.Path, request.Method))
                {
                    auditEvent.RequestBody = await GetRequestBodyAsync(request);
                }
            }

            var auditService = context.RequestServices.GetRequiredService<IAuditService>();
            await auditService.LogAsync(auditEvent);
            _logger.LogInformation(
                "Audit: {Method} {Path} - {StatusCode} - {DurationMs}ms - User: {UserId} - IP: {IpAddress}",
                request.Method,
                request.Path,
                response.StatusCode,
                (long)duration.TotalMilliseconds,
                userId ?? "anonymous",
                GetClientIpAddress(context));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in audit logging for trace {TraceId}", context.TraceIdentifier);
            // Never throw - audit logging failures should never break the request pipeline
        }
    }

    private static bool ShouldBufferResponse(HttpContext context)
    {
        var contentType = context.Response.ContentType?.ToLowerInvariant() ?? string.Empty;
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Don't buffer if content type indicates a file or binary data
        if (NonBufferableContentTypes.Any(ct => contentType.StartsWith(ct)))
            return false;

        // Don't buffer if path indicates a file download
        if (NonBufferableExtensions.Any(ext => path.Contains(ext)))
            return false;

        // Don't buffer large responses
        if (context.Response.ContentLength > 10 * 1024 * 1024) // 10MB
            return false;

        return true;
    }

    private static bool ShouldCaptureRequestBody(string path, string method)
    {
        // Only capture request body for auth endpoints and user management
        var capturePaths = new[]
        {
            "/api/auth/login",
            "/api/auth/register",
            "/api/auth/reset-password",
            "/api/users"
        };

        return capturePaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) &&
               (method == "POST" || method == "PUT");
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static bool IsSensitiveAction(string path, string method)
    {
        var sensitivePaths = new[]
        {
            "/api/auth/login",
            "/api/auth/register",
            "/api/auth/refresh",
            "/api/auth/reset-password",
            "/api/users",
            "/api/students",
            "/api/teachers",
            "/api/classes",
            "/api/exams",
            "/api/results",
            "/api/attendance",
            "/api/fees"
        };

        var sensitiveMethods = new[] { "POST", "PUT", "DELETE", "PATCH" };

        return sensitivePaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) &&
               sensitiveMethods.Contains(method.ToUpperInvariant());
    }

    private static string DetermineActionType(string path, string method)
    {
        if (path.Contains("/auth/"))
        {
            return method switch
            {
                "POST" => path.Contains("login") ? "AUTH_LOGIN" : path.Contains("register") ? "AUTH_REGISTER" : "AUTH_OTHER",
                _ => "AUTH_OTHER"
            };
        }

        if (path.Contains("/users/")) return method switch { "POST" => "USER_CREATE", "PUT" => "USER_UPDATE", "DELETE" => "USER_DELETE", _ => "USER_OTHER" };
        if (path.Contains("/students/")) return method switch { "POST" => "STUDENT_CREATE", "PUT" => "STUDENT_UPDATE", "DELETE" => "STUDENT_DELETE", _ => "STUDENT_OTHER" };
        if (path.Contains("/teachers/")) return method switch { "POST" => "TEACHER_CREATE", "PUT" => "TEACHER_UPDATE", "DELETE" => "TEACHER_DELETE", _ => "TEACHER_OTHER" };
        if (path.Contains("/classes/")) return method switch { "POST" => "CLASS_CREATE", "PUT" => "CLASS_UPDATE", "DELETE" => "CLASS_DELETE", _ => "CLASS_OTHER" };
        if (path.Contains("/exams/")) return method switch { "POST" => "EXAM_CREATE", "PUT" => "EXAM_UPDATE", "DELETE" => "EXAM_DELETE", _ => "EXAM_OTHER" };
        if (path.Contains("/results/")) return method switch { "POST" => "RESULT_CREATE", "PUT" => "RESULT_UPDATE", "DELETE" => "RESULT_DELETE", _ => "RESULT_OTHER" };
        if (path.Contains("/attendance/")) return method switch { "POST" => "ATTENDANCE_CREATE", "PUT" => "ATTENDANCE_UPDATE", "DELETE" => "ATTENDANCE_DELETE", _ => "ATTENDANCE_OTHER" };
        if (path.Contains("/fees/")) return method switch { "POST" => "FEE_CREATE", "PUT" => "FEE_UPDATE", "DELETE" => "FEE_DELETE", _ => "FEE_OTHER" };

        return "OTHER";
    }

    private static async Task<string?> GetRequestBodyAsync(HttpRequest request)
    {
        // Enable request body buffering if not already enabled
        request.EnableBuffering();

        if (!request.Body.CanRead || request.ContentLength == null || request.ContentLength > 1024 * 1024) // 1MB limit
        {
            return null;
        }

        request.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Seek(0, SeekOrigin.Begin);

        // Enhanced sensitive data redaction
        var redactedBody = RedactSensitiveData(body, request.Path);
        return redactedBody.Length > 1000 ? redactedBody.Substring(0, 1000) + "..." : redactedBody;
    }

    private static string RedactSensitiveData(string body, string path)
    {
        if (string.IsNullOrEmpty(body))
            return body;

        var redacted = body;

        // Redact passwords
        redacted = Regex.Replace(redacted, @"""password""\s*:\s*""[^""]*""", @"""password"":""[REDACTED]""", RegexOptions.IgnoreCase);
        redacted = Regex.Replace(redacted, @"password\s*=\s*[^&\s;]+", @"password=[REDACTED]", RegexOptions.IgnoreCase);

        // Redact JWT tokens and refresh tokens
        redacted = Regex.Replace(redacted, @"""token""\s*:\s*""[^""]{20,}""", @"""token"":""[REDACTED]""", RegexOptions.IgnoreCase);
        redacted = Regex.Replace(redacted, @"""refreshToken""\s*:\s*""[^""]{20,}""", @"""refreshToken"":""[REDACTED]""", RegexOptions.IgnoreCase);
        redacted = Regex.Replace(redacted, @"""accessToken""\s*:\s*""[^""]{20,}""", @"""accessToken"":""[REDACTED]""", RegexOptions.IgnoreCase);

        // Redact reset tokens
        redacted = Regex.Replace(redacted, @"""token""\s*:\s*""[a-f0-9]{32,}""", @"""token"":""[REDACTED]""", RegexOptions.IgnoreCase);
        redacted = Regex.Replace(redacted, @"""tokenHash""\s*:\s*""[a-f0-9]{64,}""", @"""tokenHash"":""[REDACTED]""", RegexOptions.IgnoreCase);

        // Redact API keys and secrets
        redacted = Regex.Replace(redacted, @"""apiKey""\s*:\s*""[^""]{10,}""", @"""apiKey"":""[REDACTED]""", RegexOptions.IgnoreCase);
        redacted = Regex.Replace(redacted, @"""secret""\s*:\s*""[^""]{10,}""", @"""secret"":""[REDACTED]""", RegexOptions.IgnoreCase);
        redacted = Regex.Replace(redacted, @"""clientSecret""\s*:\s*""[^""]{10,}""", @"""clientSecret"":""[REDACTED]""", RegexOptions.IgnoreCase);

        // Redact credit card numbers (basic pattern)
        redacted = Regex.Replace(redacted, @"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b", "[REDACTED_CARD]");

        // Redact email addresses in non-auth contexts
        if (!path.Contains("/auth/"))
        {
            redacted = Regex.Replace(redacted, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", "[REDACTED_EMAIL]");
        }

        // Redact phone numbers
        redacted = Regex.Replace(redacted, @"\b\d{3}[-.\s]?\d{3}[-.\s]?\d{4}\b", "[REDACTED_PHONE]");
        redacted = Regex.Replace(redacted, @"\b\+\d{1,3}[-.\s]?\d{3}[-.\s]?\d{3}[-.\s]?\d{4}\b", "[REDACTED_PHONE]");

        return redacted;
    }
}
