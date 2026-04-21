namespace SchoolManagement.Application.Authentication;

public sealed class PasswordResetSettings
{
    public const string SectionName = "PasswordReset";

    public int TokenExpiryMinutes { get; set; } = 30;
    public string FrontendResetUrl { get; set; } = "http://localhost:3000/reset-password";
}
