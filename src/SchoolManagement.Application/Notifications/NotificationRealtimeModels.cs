namespace SchoolManagement.Application.Notifications;

public static class NotificationRealtimeEventTypes
{
    public const string Created = "created";
    public const string Read = "read";
    public const string ReadAll = "readAll";
}

public sealed record NotificationRealtimeEvent(
    string EventType,
    NotificationResponse? Notification,
    IReadOnlyCollection<Guid> NotificationIds,
    int UnreadCount,
    DateTime OccurredAt);

public interface INotificationRealtimeNotifier
{
    Task BroadcastNotificationEventAsync(Guid recipientUserId, NotificationRealtimeEvent payload, CancellationToken cancellationToken);
}
