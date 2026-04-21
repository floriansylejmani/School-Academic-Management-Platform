using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Notifications;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    [Authorize(Roles = "Student,Parent,Teacher,Admin")]
    public async Task<ActionResult<ApiResponse<PagedResponse<NotificationResponse>>>> GetMine([FromQuery] NotificationQueryRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<PagedResponse<NotificationResponse>>.Ok(await _notificationService.GetByUserIdAsync(GetCurrentUserId(), request, cancellationToken)));
    }

    [HttpGet("unread-count")]
    [Authorize(Roles = "Student,Parent,Teacher,Admin")]
    public async Task<ActionResult<ApiResponse<UnreadCountResponse>>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await _notificationService.GetUnreadCountAsync(GetCurrentUserId(), cancellationToken);
        return Ok(ApiResponse<UnreadCountResponse>.Ok(new UnreadCountResponse(count)));
    }

    [HttpPatch("{id:guid}/read")]
    [Authorize(Roles = "Student,Parent,Teacher,Admin")]
    public async Task<ActionResult<ApiResponse<NotificationResponse>>> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<NotificationResponse>.Ok(await _notificationService.MarkAsReadAsync(GetCurrentUserId(), id, cancellationToken), "Notification marked as read."));
    }

    [HttpPatch("read-all")]
    [Authorize(Roles = "Student,Parent,Teacher,Admin")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllAsReadAsync(GetCurrentUserId(), cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Notifications marked as read."));
    }

    /// <summary>
    /// Admin-only: manually send a notification to a single user (UserId) or broadcast to an entire role (RoleName).
    /// Exactly one of the two must be supplied.
    /// </summary>
    [HttpPost("send")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Send([FromBody] SendNotificationRequest request, CancellationToken cancellationToken)
    {
        if (request.UserId.HasValue)
        {
            await _notificationService.SendToUserAsync(request.UserId.Value, request.Title, request.Message, cancellationToken);
        }
        else if (request.RoleName is not null)
        {
            await _notificationService.SendToRoleAsync(request.RoleName, request.Title, request.Message, cancellationToken);
        }
        else
        {
            return BadRequest(ApiResponse<object>.Fail("Either UserId or RoleName must be provided."));
        }

        return Ok(ApiResponse<object>.Ok(null, "Notification sent."));
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User id claim is missing.");

        return Guid.Parse(userId);
    }
}
