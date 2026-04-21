using SchoolManagement.Domain.Common;

namespace SchoolManagement.Domain.Entities;

public sealed class Submission : BaseEntity
{
    public Guid ExamId { get; set; }
    public Guid StudentId { get; set; }
    public Guid SubmittedByUserId { get; set; }
    public string? EssayPrompt { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public decimal MaximumScore { get; set; }
    public decimal? TeacherFinalScore { get; set; }
    public string? TeacherFinalGrade { get; set; }
    public string? TeacherReviewNotes { get; set; }
    public bool IsAiFeedbackReleasedToStudent { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public Exam? Exam { get; set; }
    public Student? Student { get; set; }
    public User? SubmittedByUser { get; set; }
    public User? ReviewedByUser { get; set; }
    public SubmissionAIReview? AIReview { get; set; }
}
