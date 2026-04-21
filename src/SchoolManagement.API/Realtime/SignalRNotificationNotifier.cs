using Microsoft.AspNetCore.SignalR;
using SchoolManagement.API.Realtime.Hubs;
using SchoolManagement.Application.Notifications;

namespace SchoolManagement.API.Realtime;

public sealed class SignalRNotificationNotifier : INotificationRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationNotifier> _logger;

    public SignalRNotificationNotifier(IHubContext<NotificationHub> hubContext, ILogger<SignalRNotificationNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastNotificationEventAsync(Guid recipientUserId, NotificationRealtimeEvent payload, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.User(recipientUserId.ToString())
            .SendAsync(NotificationHub.NotificationEventMethod, payload, cancellationToken);

        _logger.LogInformation(
            "Notification realtime event broadcast. UserId: {UserId}, EventType: {EventType}, NotificationIds: {NotificationIds}",
            recipientUserId,
            payload.EventType,
            string.Join(",", payload.NotificationIds));
    }
}
