using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SchoolManagement.API.Realtime.Hubs;

[Authorize(Roles = "Admin,Teacher,Student,Parent")]
public sealed class NotificationHub : Hub
{
    public const string NotificationEventMethod = "notificationEvent";

    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "Notification hub connected. UserId: {UserId}, ConnectionId: {ConnectionId}",
            Context.UserIdentifier ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown",
            Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is null)
        {
            _logger.LogInformation(
                "Notification hub disconnected. UserId: {UserId}, ConnectionId: {ConnectionId}",
                Context.UserIdentifier ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Notification hub disconnected with error. UserId: {UserId}, ConnectionId: {ConnectionId}",
                Context.UserIdentifier ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown",
                Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
