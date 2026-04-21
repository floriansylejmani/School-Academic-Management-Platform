using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Persistence.Services;

public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditService> _logger;
    private readonly AuditOptions _options;
    private readonly IAuditInitializationState _initializationState;

    public AuditService(
        AppDbContext context,
        ILogger<AuditService> logger,
        IOptions<AuditOptions> options,
        IAuditInitializationState initializationState)
    {
        _context = context;
        _logger = logger;
        _options = options.Value;
        _initializationState = initializationState;
    }

    public async Task LogAsync(AuditEvent auditEvent)
    {
        if (!_initializationState.IsReady)
        {
            _logger.LogDebug("Skipping audit logging because database initialization is not complete. TraceId: {TraceId}", auditEvent.TraceId);
            return;
        }

        if (!auditEvent.IsSensitive && !_options.LogAllEvents)
        {
            return;
        }

        try
        {
            var auditLog = new AuditLog
            {
                Timestamp = auditEvent.Timestamp,
                UserId = auditEvent.UserId,
                UserEmail = auditEvent.UserEmail,
                IpAddress = auditEvent.IpAddress,
                UserAgent = auditEvent.UserAgent,
                HttpMethod = auditEvent.HttpMethod,
                RequestPath = auditEvent.RequestPath,
                QueryString = auditEvent.QueryString,
                StatusCode = auditEvent.StatusCode,
                DurationMs = auditEvent.DurationMs,
                Success = auditEvent.Success,
                IsSensitive = auditEvent.IsSensitive,
                ActionType = auditEvent.ActionType,
                RequestBody = auditEvent.RequestBody,
                TraceId = auditEvent.TraceId
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit event for trace {TraceId}", auditEvent.TraceId);
        }
    }

    public Task FlushAsync() => Task.CompletedTask;
}

public sealed class AuditOptions
{
    public bool LogAllEvents { get; set; } = false;
    public int BatchSize { get; set; } = 100;
    public TimeSpan BatchFlushInterval { get; set; } = TimeSpan.FromMinutes(5);
}
