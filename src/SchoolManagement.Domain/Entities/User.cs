using SchoolManagement.Domain.Common;

namespace SchoolManagement.Domain.Entities;

public sealed class User : BaseEntity
{
    public Guid RoleId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public string? ProfilePictureUrl { get; set; }

    public Role? Role { get; set; }
    public Parent? ParentProfile { get; set; }
    public Teacher? TeacherProfile { get; set; }
    public Student? StudentProfile { get; set; }
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<ResetToken> ResetTokens { get; set; } = new List<ResetToken>();
    public ICollection<Submission> SubmittedSubmissions { get; set; } = new List<Submission>();
    public ICollection<Submission> ReviewedSubmissions { get; set; } = new List<Submission>();
    public ICollection<SubmissionAIReview> RequestedSubmissionAIReviews { get; set; } = new List<SubmissionAIReview>();
}
