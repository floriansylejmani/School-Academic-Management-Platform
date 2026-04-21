using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Notifications;
using SchoolManagement.Persistence.Common;

namespace SchoolManagement.Persistence.Services;

public sealed class NotificationRealtimeDispatcher
{
    private readonly AppDbContext _context;
    private readonly INotificationRealtimeNotifier _notificationRealtimeNotifier;
    private readonly ILogger<NotificationRealtimeDispatcher> _logger;

    public NotificationRealtimeDispatcher(
        AppDbContext context,
        INotificationRealtimeNotifier notificationRealtimeNotifier,
        ILogger<NotificationRealtimeDispatcher> logger)
    {
        _context = context;
        _notificationRealtimeNotifier = notificationRealtimeNotifier;
        _logger = logger;
    }

    public async Task BroadcastCreatedAsync(IEnumerable<Guid> notificationIds, CancellationToken cancellationToken)
    {
        var ids = notificationIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        try
        {
            var notifications = await LoadNotificationsAsync(ids, cancellationToken);
            if (notifications.Count == 0)
            {
                return;
            }

            var unreadCounts = await GetUnreadCountsAsync(notifications.Select(x => x.UserId), cancellationToken);

            foreach (var notification in notifications)
            {
                await _notificationRealtimeNotifier.BroadcastNotificationEventAsync(
                    notification.UserId,
                    new NotificationRealtimeEvent(
                        NotificationRealtimeEventTypes.Created,
                        notification.ToResponse(),
                        [notification.Id],
                        unreadCounts.GetValueOrDefault(notification.UserId),
                        DateTime.UtcNow),
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast created notifications. NotificationIds: {NotificationIds}", string.Join(",", ids));
        }
    }

    public async Task BroadcastReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        try
        {
            var notification = await LoadNotificationsAsync([notificationId], cancellationToken);
            var resolved = notification.SingleOrDefault(x => x.UserId == userId);
            if (resolved is null)
            {
                return;
            }

            var unreadCounts = await GetUnreadCountsAsync([userId], cancellationToken);

            await _notificationRealtimeNotifier.BroadcastNotificationEventAsync(
                userId,
                new NotificationRealtimeEvent(
                    NotificationRealtimeEventTypes.Read,
                    resolved.ToResponse(),
                    [resolved.Id],
                    unreadCounts.GetValueOrDefault(userId),
                    DateTime.UtcNow),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast read notification. UserId: {UserId}, NotificationId: {NotificationId}", userId, notificationId);
        }
    }

    public async Task BroadcastReadAllAsync(Guid userId, IEnumerable<Guid> notificationIds, CancellationToken cancellationToken)
    {
        var ids = notificationIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        try
        {
            await _notificationRealtimeNotifier.BroadcastNotificationEventAsync(
                userId,
                new NotificationRealtimeEvent(
                    NotificationRealtimeEventTypes.ReadAll,
                    null,
                    ids,
                    0,
                    DateTime.UtcNow),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast read-all notifications. UserId: {UserId}, NotificationIds: {NotificationIds}", userId, string.Join(",", ids));
        }
    }

    private async Task<List<Domain.Entities.Notification>> LoadNotificationsAsync(IEnumerable<Guid> notificationIds, CancellationToken cancellationToken)
    {
        var ids = notificationIds.Distinct().ToArray();

        return await _context.Notifications
            .AsNoTracking()
            .Include(x => x.Student)
                .ThenInclude(x => x!.User)
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, int>> GetUnreadCountsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        return await _context.Notifications
            .AsNoTracking()
            .Where(x => ids.Contains(x.UserId) && !x.IsRead)
            .GroupBy(x => x.UserId)
            .Select(x => new { UserId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);
    }
}
