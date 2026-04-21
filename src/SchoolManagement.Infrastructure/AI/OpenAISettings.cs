namespace SchoolManagement.Infrastructure.AI;

public sealed class OpenAISettings
{
    public const string SectionName = "OpenAI";

    public bool Enabled { get; init; }
    public string ApiKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://api.openai.com/v1/";
    public string Model { get; init; } = "gpt-5.4-mini";
    public string ReasoningEffort { get; init; } = "low";
    public int TimeoutSeconds { get; init; } = 45;
    public int MaxEssayCharacters { get; init; } = 12000;
    public bool UseModeration { get; init; }
    public string ModerationModel { get; init; } = "omni-moderation-latest";
}
