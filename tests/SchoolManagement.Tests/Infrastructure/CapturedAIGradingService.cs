using SchoolManagement.Application.AI;

namespace SchoolManagement.Tests.Infrastructure;

public sealed class CapturedAIGradingService : IAIGradingService
{
    private AIEssayAssessmentResult _nextResult = new(
        AIGradingModes.Feedback,
        "fake-model",
        "fake-response-id",
        84,
        82,
        80,
        85,
        17,
        "Strong core argument with room to sharpen evidence and transitions.",
        ["Clear thesis", "Relevant topic coverage"],
        ["Some grammar slips", "Transitions need work"],
        ["Tighten sentence structure", "Add stronger textual evidence"],
        [
            new AIRubricBreakdownItem("Grammar", 4, 5, "Mostly accurate with a few slips."),
            new AIRubricBreakdownItem("Clarity", 4, 5, "Generally clear, though some sentences are dense."),
            new AIRubricBreakdownItem("Content", 9, 10, "Good engagement with the prompt.")
        ],
        null);

    public AIEssayAssessmentRequest? LastRequest { get; private set; }
    public Exception? NextException { get; set; }

    public Task<AIEssayAssessmentResult> GenerateEssayAssessmentAsync(AIEssayAssessmentRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;

        if (NextException is not null)
        {
            var exception = NextException;
            NextException = null;
            throw exception;
        }

        var result = _nextResult with
        {
            Mode = request.Mode
        };

        return Task.FromResult(result);
    }

    public void SetNextResult(AIEssayAssessmentResult result)
    {
        _nextResult = result;
    }
}
