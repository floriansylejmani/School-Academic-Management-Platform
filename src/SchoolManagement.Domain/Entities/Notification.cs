using SchoolManagement.Domain.Common;

namespace SchoolManagement.Domain.Entities;

public sealed class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }

    /// <summary>
    /// When set, this notification relates to a specific student (e.g. a parent notification about their child).
    /// Null for account-level / broadcast notifications.
    /// </summary>
    public Guid? StudentId { get; set; }

    public User? User { get; set; }
    public Student? Student { get; set; }
}
