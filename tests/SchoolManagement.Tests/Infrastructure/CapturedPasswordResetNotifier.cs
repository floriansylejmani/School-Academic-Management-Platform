using System.Collections.Concurrent;
using SchoolManagement.Application.Common.Interfaces;

namespace SchoolManagement.Tests.Infrastructure;

public sealed class CapturedPasswordResetNotifier : IPasswordResetNotifier
{
    private readonly ConcurrentDictionary<string, string> _tokensByEmail = new(StringComparer.OrdinalIgnoreCase);

    public Task SendPasswordResetAsync(string email, string fullName, string resetToken, CancellationToken cancellationToken)
    {
        _tokensByEmail[email] = resetToken;
        return Task.CompletedTask;
    }

    public string GetToken(string email)
    {
        if (_tokensByEmail.TryGetValue(email, out var token))
        {
            return token;
        }

        throw new InvalidOperationException($"No reset token captured for {email}.");
    }

    public void Clear()
    {
        _tokensByEmail.Clear();
    }
}
