using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.API.Common;

public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IOptions<RateLimitingOptions> _options;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public RateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<RateLimitingMiddleware> logger,
        IOptions<RateLimitingOptions> options)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var rateLimitAttribute = endpoint?.Metadata.GetMetadata<RateLimitAttribute>();

        // Determine rate limit settings
        (int limit, int windowSeconds) = GetRateLimitSettings(rateLimitAttribute, context.Request.Path);

        var clientId = GetClientId(context);
        var key = $"rate_limit:{clientId}:{context.Request.Path}";

        // Use semaphore to prevent race conditions
        await _semaphore.WaitAsync();
        try
        {
            var currentCount = _cache.Get<int>(key);
            var windowStart = _cache.Get<DateTime>($"{key}:window");

            // Initialize if not exists or window expired
            if (windowStart == default || DateTime.UtcNow > windowStart.AddSeconds(windowSeconds))
            {
                currentCount = 1;
                windowStart = DateTime.UtcNow;
                
                _cache.Set(key, currentCount, TimeSpan.FromSeconds(windowSeconds));
                _cache.Set($"{key}:window", windowStart, TimeSpan.FromSeconds(windowSeconds));
            }
            else
            {
                currentCount++;
                _cache.Set(key, currentCount, TimeSpan.FromSeconds(windowSeconds));
            }

            // Check if rate limit exceeded
            if (currentCount > limit)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId} on path {Path}. Count: {Count}, Limit: {Limit}",
                    clientId, context.Request.Path, currentCount, limit);

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/json";
                
                var response = ApiResponse<object>.Fail(
                    "Too many requests. Please try again later.",
                    traceId: context.TraceIdentifier);

                await context.Response.WriteAsJsonAsync(response);
                return;
            }

            // Add rate limit headers
            context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, limit - currentCount).ToString();
            context.Response.Headers["X-RateLimit-Reset"] = windowStart.AddSeconds(windowSeconds).ToString("yyyy-MM-ddTHH:mm:ssZ");
        }
        finally
        {
            _semaphore.Release();
        }

        await _next(context);
    }

    private (int limit, int windowSeconds) GetRateLimitSettings(RateLimitAttribute? attribute, string path)
    {
        // If endpoint has explicit rate limit attribute, use it
        if (attribute != null)
        {
            return (attribute.Limit, attribute.WindowSeconds);
        }

        // Apply global defaults based on path patterns
        var options = _options.Value;
        
        // Auth endpoints get stricter limits
        if (path.Contains("/auth/", StringComparison.OrdinalIgnoreCase))
        {
            return (options.AuthLimit, options.AuthWindowSeconds);
        }

        // Default global limits for all other endpoints
        return (options.DefaultLimit, options.DefaultWindowSeconds);
    }

    private static string GetClientId(HttpContext context)
    {
        // Try to get user ID from claims
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim))
        {
            return $"user:{userIdClaim}";
        }

        // Fall back to IP address
        var remoteIpAddress = context.Connection.RemoteIpAddress?.ToString();
        return $"ip:{remoteIpAddress ?? "unknown"}";
    }
}

public sealed class RateLimitingOptions
{
    public int DefaultLimit { get; set; } = 100;
    public int DefaultWindowSeconds { get; set; } = 60;
    public int AuthLimit { get; set; } = 5;
    public int AuthWindowSeconds { get; set; } = 300; // 5 minutes
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RateLimitAttribute : Attribute
{
    public int Limit { get; }
    public int WindowSeconds { get; }

    public RateLimitAttribute(int limit, int windowSeconds)
    {
        Limit = limit;
        WindowSeconds = windowSeconds;
    }
}

