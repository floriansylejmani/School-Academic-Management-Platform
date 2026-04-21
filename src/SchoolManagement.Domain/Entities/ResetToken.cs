using SchoolManagement.Domain.Common;

namespace SchoolManagement.Domain.Entities;

public sealed class ResetToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public User? User { get; set; }
}
