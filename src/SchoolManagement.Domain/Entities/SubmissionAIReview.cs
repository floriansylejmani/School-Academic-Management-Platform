using SchoolManagement.Domain.Common;

namespace SchoolManagement.Domain.Entities;

public sealed class SubmissionAIReview : BaseEntity
{
    public Guid SubmissionId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? ProviderResponseId { get; set; }
    public int GrammarScore { get; set; }
    public int ClarityScore { get; set; }
    public int StructureScore { get; set; }
    public int ContentScore { get; set; }
    public decimal OverallSuggestedScore { get; set; }
    public string SummaryFeedback { get; set; } = string.Empty;
    public string StrengthsJson { get; set; } = "[]";
    public string WeaknessesJson { get; set; } = "[]";
    public string ImprovementsJson { get; set; } = "[]";
    public string RubricBreakdownJson { get; set; } = "[]";
    public string? SafetyNotes { get; set; }

    public Submission? Submission { get; set; }
    public User? RequestedByUser { get; set; }
}
