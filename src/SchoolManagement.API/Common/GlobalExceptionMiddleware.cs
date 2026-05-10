using FluentValidation;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.API.Common;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            _logger.LogWarning(exception, "Unauthorized request rejected. TraceId: {TraceId}", context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail(
                    "Authentication is required to access this resource.",
                    traceId: context.TraceIdentifier));
        }
        catch (ValidationException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            _logger.LogInformation("Validation failed for request. TraceId: {TraceId}", context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail(
                    "Validation failed",
                    exception.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(x => x.Key, x => x.Select(y => y.ErrorMessage).ToArray()),
                    context.TraceIdentifier));
        }
        catch (ArgumentException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            _logger.LogWarning(exception, "Bad request rejected. TraceId: {TraceId}", context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail(
                    exception.Message,
                    traceId: context.TraceIdentifier));
        }
        catch (AppException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            _logger.Log(
                exception.StatusCode >= StatusCodes.Status500InternalServerError ? LogLevel.Error : LogLevel.Warning,
                exception,
                "Handled application exception. TraceId: {TraceId}",
                context.TraceIdentifier);
            context.Response.StatusCode = exception.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail(
                    exception.Message,
                    traceId: context.TraceIdentifier));
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            _logger.LogError(exception, "Unhandled server exception. TraceId: {TraceId}", context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail(
                    _environment.IsDevelopment() ? exception.Message : "An unexpected server error occurred.",
                    traceId: context.TraceIdentifier));
        }
    }
}
