namespace SchoolManagement.Application.Common;

public sealed class AuditEvent
{
    public DateTime Timestamp { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    public bool Success { get; set; }
    public bool IsSensitive { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? RequestBody { get; set; }
    public string TraceId { get; set; } = string.Empty;
}
