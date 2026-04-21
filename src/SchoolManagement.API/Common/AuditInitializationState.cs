using SchoolManagement.Application.Common.Interfaces;

namespace SchoolManagement.API.Common;

public sealed class AuditInitializationState : IAuditInitializationState
{
    private volatile bool _isReady;

    public bool IsReady
    {
        get => _isReady;
        set => _isReady = value;
    }
}
