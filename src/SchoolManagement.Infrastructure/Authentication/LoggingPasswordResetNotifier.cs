using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchoolManagement.Application.Authentication;
using SchoolManagement.Application.Common.Interfaces;

namespace SchoolManagement.Infrastructure.Authentication;

public sealed class LoggingPasswordResetNotifier : IPasswordResetNotifier
{
    private readonly ILogger<LoggingPasswordResetNotifier> _logger;
    private readonly PasswordResetSettings _settings;

    public LoggingPasswordResetNotifier(ILogger<LoggingPasswordResetNotifier> logger, IOptions<PasswordResetSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public Task SendPasswordResetAsync(string email, string fullName, string resetToken, CancellationToken cancellationToken)
    {
        var separator = _settings.FrontendResetUrl.Contains('?') ? "&" : "?";
        var resetUrl = $"{_settings.FrontendResetUrl}{separator}token={Uri.EscapeDataString(resetToken)}";

        _logger.LogInformation(
            "Password reset requested for {Email} ({FullName}). Mock reset link: {ResetUrl}",
            email,
            fullName,
            resetUrl);

        return Task.CompletedTask;
    }
}
