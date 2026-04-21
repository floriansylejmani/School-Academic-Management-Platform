using SchoolManagement.Application.Common;

namespace SchoolManagement.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(AuditEvent auditEvent);
    Task FlushAsync();
}
