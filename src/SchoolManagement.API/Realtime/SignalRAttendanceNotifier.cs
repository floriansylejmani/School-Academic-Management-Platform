using Microsoft.AspNetCore.SignalR;
using SchoolManagement.API.Realtime.Hubs;
using SchoolManagement.Application.Attendance;

namespace SchoolManagement.API.Realtime;

public sealed class SignalRAttendanceNotifier : IAttendanceRealtimeNotifier
{
    private readonly IHubContext<AttendanceHub> _hubContext;
    private readonly ILogger<SignalRAttendanceNotifier> _logger;

    public SignalRAttendanceNotifier(IHubContext<AttendanceHub> hubContext, ILogger<SignalRAttendanceNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastAttendanceChangedAsync(IEnumerable<Guid> recipientUserIds, AttendanceRealtimeEvent payload, CancellationToken cancellationToken)
    {
        var recipients = recipientUserIds
            .Distinct()
            .Select(x => x.ToString())
            .ToArray();

        if (recipients.Length == 0)
        {
            return;
        }

        await _hubContext.Clients.Users(recipients)
            .SendAsync(AttendanceHub.AttendanceChangedMethod, payload, cancellationToken);

        _logger.LogInformation(
            "Attendance realtime event broadcast. AttendanceId: {AttendanceId}, EventType: {EventType}, RecipientCount: {RecipientCount}",
            payload.Attendance.Id,
            payload.EventType,
            recipients.Length);
    }
}
