using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.API.Common;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate Content Security Policy based on request type
        var csp = GenerateContentSecurityPolicy(context);
        context.Response.Headers["Content-Security-Policy"] = csp;

        // Prevent clickjacking
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // Prevent MIME type sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // XSS Protection (legacy but still useful for older browsers)
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Referrer Policy - balanced for analytics and privacy
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Permissions Policy - disable unnecessary browser features
        var permissionsPolicy = "geolocation=(), " +
                              "microphone=(), " +
                              "camera=(), " +
                              "payment=(), " +
                              "usb=(), " +
                              "interest-group=(), " +
                              "browsing-topics=(), " +
                              "private-state-token-issuance=()";

        context.Response.Headers["Permissions-Policy"] = permissionsPolicy;

        // HSTS (only in production with HTTPS)
        if (!_environment.IsDevelopment())
        {
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        }

        // Remove server header to hide server information
        context.Response.Headers["Server"] = string.Empty;

        // Cache control based on content type
        SetCacheControlHeaders(context);

        await _next(context);
    }

    private static string GenerateContentSecurityPolicy(HttpContext context)
    {
        var path = context.Request.Path;
        var isDevelopment = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();

        // Base CSP directives
        var cspBuilder = new System.Text.StringBuilder();
        
        // Default to same-origin for everything
        cspBuilder.Append("default-src 'self'; ");
        
        // Script sources - more restrictive in production
        if (isDevelopment)
        {
            // Development allows hot-reloading and eval for debugging
            cspBuilder.Append("script-src 'self' 'unsafe-inline' 'unsafe-eval'; ");
        }
        else
        {
            // Production - allow same-origin scripts and inline styles for Tailwind
            cspBuilder.Append("script-src 'self'; ");
        }
        
        // Style sources - allow inline for Tailwind CSS and dynamic styling
        cspBuilder.Append("style-src 'self' 'unsafe-inline'; ");
        
        // Image sources - allow data URLs for avatars and file previews
        cspBuilder.Append("img-src 'self' data: blob:; ");
        
        // Font sources - same-origin only
        cspBuilder.Append("font-src 'self'; ");
        
        // Connect sources - allow API calls, SignalR, and external services
        cspBuilder.Append("connect-src 'self' ws: wss:; ");
        
        // Media sources - for potential audio/video features
        cspBuilder.Append("media-src 'self' blob:; ");
        
        // Object sources - restrict for security
        cspBuilder.Append("object-src 'none'; ");
        
        // Base URI - same-origin only
        cspBuilder.Append("base-uri 'self'; ");
        
        // Form actions - same-origin only
        cspBuilder.Append("form-action 'self'; ");
        
        // Frame ancestors - prevent clickjacking
        cspBuilder.Append("frame-ancestors 'none'; ");
        
        // Worker sources - same-origin only for security
        cspBuilder.Append("worker-src 'self'; ");

        return cspBuilder.ToString().TrimEnd();
    }

    private static void SetCacheControlHeaders(HttpContext context)
    {
        var path = context.Request.Path;
        
        // API responses - no caching for sensitive data
        if (path.StartsWithSegments("/api"))
        {
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            return;
        }
        
        // Static assets - aggressive caching with validation
        if (path.StartsWithSegments("/_next") || 
            path.StartsWithSegments("/static") ||
            path.ToString().Contains(".css") || 
            path.ToString().Contains(".js") ||
            path.ToString().Contains(".woff") ||
            path.ToString().Contains(".ttf"))
        {
            context.Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
            return;
        }
        
        // File uploads/downloads - no caching for security
        if (path.StartsWithSegments("/uploads") || 
            path.ToString().Contains("/files/") ||
            path.ToString().Contains("/download"))
        {
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            return;
        }
        
        // Health check - short caching for load balancers
        if (path.StartsWithSegments("/health"))
        {
            context.Response.Headers["Cache-Control"] = "public, max-age=30";
            return;
        }
        
        // Default - moderate caching
        context.Response.Headers["Cache-Control"] = "public, max-age=3600";
    }
}
