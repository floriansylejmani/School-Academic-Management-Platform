using FluentValidation;
using SchoolManagement.Application.AI;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Submissions;

public sealed record SubmissionAIReviewResponse(
    string Mode,
    string Model,
    DateTime GeneratedAt,
    int GrammarScore,
    int ClarityScore,
    int StructureScore,
    int ContentScore,
    decimal OverallSuggestedScore,
    string SummaryFeedback,
    IReadOnlyCollection<string> Strengths,
    IReadOnlyCollection<string> Weaknesses,
    IReadOnlyCollection<string> Improvements,
    IReadOnlyCollection<AIRubricBreakdownItem> RubricBreakdown,
    string? SafetyNotes);

public sealed record SubmissionResponse(
    Guid Id,
    Guid ExamId,
    string ExamTitle,
    Guid StudentId,
    string StudentName,
    Guid ClassId,
    string ClassName,
    Guid SubjectId,
    string SubjectName,
    string? EssayPrompt,
    string AnswerText,
    decimal MaximumScore,
    decimal? TeacherFinalScore,
    string? TeacherFinalGrade,
    string? TeacherReviewNotes,
    bool IsAiFeedbackReleasedToStudent,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    bool HasAIReview,
    SubmissionAIReviewResponse? AIReview);

public sealed class SubmissionQueryRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public Guid? ExamId { get; init; }
    public Guid? StudentId { get; init; }
    public bool? ReleasedOnly { get; init; }
}

public sealed record CreateSubmissionRequest(
    Guid ExamId,
    Guid? StudentId,
    string? EssayPrompt,
    string AnswerText);

public sealed record RequestSubmissionAIRequest(
    string? RubricInstructions,
    string? AdditionalInstructions);

public sealed record UpdateSubmissionTeacherReviewRequest(
    decimal? TeacherFinalScore,
    string? TeacherFinalGrade,
    string? TeacherReviewNotes,
    bool IsAiFeedbackReleasedToStudent);

public interface ISubmissionService
{
    Task<PagedResponse<SubmissionResponse>> GetPagedAsync(SubmissionQueryRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<SubmissionResponse>> GetForTeacherUserAsync(Guid teacherUserId, SubmissionQueryRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<SubmissionResponse>> GetForStudentUserAsync(Guid studentUserId, SubmissionQueryRequest request, CancellationToken cancellationToken);
    Task<SubmissionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<SubmissionResponse> GetForTeacherUserByIdAsync(Guid teacherUserId, Guid id, CancellationToken cancellationToken);
    Task<SubmissionResponse> GetForStudentUserByIdAsync(Guid studentUserId, Guid id, CancellationToken cancellationToken);
    Task<SubmissionResponse> CreateAsync(Guid submittedByUserId, CreateSubmissionRequest request, CancellationToken cancellationToken);
    Task<SubmissionResponse> CreateForTeacherUserAsync(Guid teacherUserId, CreateSubmissionRequest request, CancellationToken cancellationToken);
    Task<SubmissionResponse> CreateForStudentUserAsync(Guid studentUserId, CreateSubmissionRequest request, CancellationToken cancellationToken);
    Task<SubmissionResponse> UpdateTeacherReviewAsync(Guid reviewerUserId, Guid id, UpdateSubmissionTeacherReviewRequest request, CancellationToken cancellationToken);
    Task<SubmissionResponse> UpdateTeacherReviewForTeacherUserAsync(Guid teacherUserId, Guid id, UpdateSubmissionTeacherReviewRequest request, CancellationToken cancellationToken);
    Task<SubmissionResponse> GenerateAIFeedbackAsync(Guid requestedByUserId, Guid id, RequestSubmissionAIRequest request, CancellationToken cancellationToken);
    Task<SubmissionResponse> GenerateAIFeedbackForTeacherUserAsync(Guid teacherUserId, Guid id, RequestSubmissionAIRequest request, CancellationToken cancellationToken);
    Task<SubmissionResponse> GenerateSmartGradeAsync(Guid requestedByUserId, Guid id, RequestSubmissionAIRequest request, CancellationToken cancellationToken);
    Task<SubmissionResponse> GenerateSmartGradeForTeacherUserAsync(Guid teacherUserId, Guid id, RequestSubmissionAIRequest request, CancellationToken cancellationToken);
}

public sealed class SubmissionQueryRequestValidator : AbstractValidator<SubmissionQueryRequest>
{
    public SubmissionQueryRequestValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class CreateSubmissionRequestValidator : AbstractValidator<CreateSubmissionRequest>
{
    public CreateSubmissionRequestValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty().When(x => x.StudentId.HasValue);
        RuleFor(x => x.EssayPrompt).MaximumLength(2_000);
        RuleFor(x => x.AnswerText).NotEmpty().MaximumLength(20_000);
    }
}

public sealed class RequestSubmissionAIRequestValidator : AbstractValidator<RequestSubmissionAIRequest>
{
    public RequestSubmissionAIRequestValidator()
    {
        RuleFor(x => x.RubricInstructions).MaximumLength(2_000);
        RuleFor(x => x.AdditionalInstructions).MaximumLength(1_000);
    }
}

public sealed class UpdateSubmissionTeacherReviewRequestValidator : AbstractValidator<UpdateSubmissionTeacherReviewRequest>
{
    public UpdateSubmissionTeacherReviewRequestValidator()
    {
        RuleFor(x => x.TeacherFinalScore)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TeacherFinalScore.HasValue);
        RuleFor(x => x.TeacherFinalGrade).MaximumLength(20);
        RuleFor(x => x.TeacherReviewNotes).MaximumLength(2_000);
    }
}
