using FluentValidation;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    Guid UserId,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAt,
    Guid? StudentId = null,
    string? StudentName = null);

public sealed record UnreadCountResponse(int Count);

public sealed class NotificationQueryRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public bool? UnreadOnly { get; init; }

    /// <summary>
    /// When set, returns only notifications linked to this specific student.
    /// Used by parents to filter notifications for a single child.
    /// </summary>
    public Guid? StudentId { get; init; }
}

public sealed record SendNotificationRequest(
    string Title,
    string Message,
    /// <summary>Send to a single user. Mutually exclusive with RoleName.</summary>
    Guid? UserId,
    /// <summary>Broadcast to every active user in this role. Mutually exclusive with UserId.</summary>
    string? RoleName);

public interface INotificationService
{
    Task<PagedResponse<NotificationResponse>> GetByUserIdAsync(Guid userId, NotificationQueryRequest request, CancellationToken cancellationToken);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken);
    Task<NotificationResponse> MarkAsReadAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken);
    Task SendToUserAsync(Guid userId, string title, string message, CancellationToken cancellationToken);
    Task SendToRoleAsync(string roleName, string title, string message, CancellationToken cancellationToken);
}

public sealed class NotificationQueryRequestValidator : AbstractValidator<NotificationQueryRequest>
{
    public NotificationQueryRequestValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class SendNotificationRequestValidator : AbstractValidator<SendNotificationRequest>
{
    public SendNotificationRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(500);
        RuleFor(x => x).Must(x => (x.UserId.HasValue && x.RoleName is null) || (!x.UserId.HasValue && x.RoleName is not null))
            .WithMessage("Exactly one of UserId or RoleName must be provided.");
        RuleFor(x => x.RoleName)
            .Must(r => r is null || new[] { "Admin", "Teacher", "Student", "Parent" }.Contains(r))
            .WithMessage("RoleName must be Admin, Teacher, Student, or Parent.");
    }
}
