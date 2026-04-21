namespace SchoolManagement.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, string fullName, string role);
    string GenerateRefreshToken();
    DateTime GetRefreshTokenExpiry();
}

public interface IPasswordResetNotifier
{
    Task SendPasswordResetAsync(string email, string fullName, string resetToken, CancellationToken cancellationToken);
}
