using FluentValidation;

namespace SchoolManagement.Application.AI;

public static class AIGradingModes
{
    public const string Feedback = "Feedback";
    public const string SmartGrade = "SmartGrade";
}

public sealed record AIRubricBreakdownItem(
    string Criterion,
    decimal Score,
    decimal MaxScore,
    string Feedback);

public sealed record AIEssayAssessmentRequest(
    Guid SubmissionId,
    string Mode,
    string ExamTitle,
    string SubjectName,
    string ClassName,
    decimal MaximumScore,
    string? EssayPrompt,
    string SubmissionText,
    string? RubricInstructions,
    string? AdditionalInstructions);

public sealed record AIEssayAssessmentResult(
    string Mode,
    string Model,
    string? ProviderResponseId,
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

public interface IAIGradingService
{
    Task<AIEssayAssessmentResult> GenerateEssayAssessmentAsync(AIEssayAssessmentRequest request, CancellationToken cancellationToken);
}

public sealed class AIEssayAssessmentRequestValidator : AbstractValidator<AIEssayAssessmentRequest>
{
    public AIEssayAssessmentRequestValidator()
    {
        RuleFor(x => x.SubmissionId).NotEmpty();
        RuleFor(x => x.Mode)
            .Must(mode => mode is AIGradingModes.Feedback or AIGradingModes.SmartGrade)
            .WithMessage("Mode must be Feedback or SmartGrade.");
        RuleFor(x => x.ExamTitle).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SubjectName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ClassName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MaximumScore).GreaterThan(0);
        RuleFor(x => x.SubmissionText).NotEmpty().MaximumLength(20_000);
        RuleFor(x => x.EssayPrompt).MaximumLength(2_000);
        RuleFor(x => x.RubricInstructions).MaximumLength(2_000);
        RuleFor(x => x.AdditionalInstructions).MaximumLength(1_000);
    }
}
