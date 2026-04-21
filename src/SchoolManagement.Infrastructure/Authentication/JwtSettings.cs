namespace SchoolManagement.Infrastructure.Authentication;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    public const string DefaultDevelopmentSecretKey = "ChangeThisSecretKeyInProduction_AtLeast32Characters!";

    public string Issuer { get; set; } = "SchoolManagement";
    public string Audience { get; set; } = "SchoolManagement.Client";
    public string SecretKey { get; set; } = DefaultDevelopmentSecretKey;
    public int AccessTokenExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
