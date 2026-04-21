using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SchoolManagement.Application.Authentication;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Roles;
using SchoolManagement.Application.Users;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Persistence.Common;

namespace SchoolManagement.Persistence.Services;

public sealed class AuthService : IAuthService
{
    private static readonly Dictionary<string, string> AllowedRegistrationRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Teacher"] = "Teacher",
        ["Student"] = "Student",
        ["Parent"] = "Parent"
    };

    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IPasswordResetNotifier _passwordResetNotifier;
    private readonly PasswordResetSettings _passwordResetSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IPasswordResetNotifier passwordResetNotifier,
        IOptions<PasswordResetSettings> passwordResetOptions,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _passwordResetNotifier = passwordResetNotifier;
        _passwordResetSettings = passwordResetOptions.Value;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var requestedRole = request.Role.Trim();

        if (await _context.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new AppException("A user with this email already exists.");
        }

        if (!AllowedRegistrationRoles.TryGetValue(requestedRole, out var roleName))
        {
            throw new AppException("Registration is limited to Teacher, Student, or Parent roles.");
        }

        var role = await _context.Roles.SingleOrDefaultAsync(x => x.Name == roleName, cancellationToken)
            ?? throw new AppException("Requested role does not exist.", 404);

        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var user = new User
        {
            RoleId = role.Id,
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            IsActive = true,
            RefreshToken = HashRefreshToken(rawRefreshToken),
            RefreshTokenExpiresAt = _tokenService.GetRefreshTokenExpiry()
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return BuildAuthResponse(user, role.Name, rawRefreshToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.Email == email, cancellationToken)
            ?? throw new AppException("Invalid email or password.", 401);

        if (!user.IsActive || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new AppException("Invalid email or password.", 401);
        }

        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = HashRefreshToken(rawRefreshToken);
        user.RefreshTokenExpiresAt = _tokenService.GetRefreshTokenExpiry();
        await _context.SaveChangesAsync(cancellationToken);

        return BuildAuthResponse(user, user.Role?.Name ?? string.Empty, rawRefreshToken);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var normalizedRefreshToken = refreshToken.Trim();
        if (string.IsNullOrWhiteSpace(normalizedRefreshToken))
        {
            throw new AppException("Refresh token is invalid or expired.", 401);
        }

        var hashedRefreshToken = HashRefreshToken(normalizedRefreshToken);
        var user = await _context.Users
            .Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.RefreshToken == hashedRefreshToken, cancellationToken)
            ?? throw new AppException("Refresh token is invalid or expired.", 401);

        if (user.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            throw new AppException("Refresh token is invalid or expired.", 401);
        }

        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = HashRefreshToken(rawRefreshToken);
        user.RefreshTokenExpiresAt = _tokenService.GetRefreshTokenExpiry();
        await _context.SaveChangesAsync(cancellationToken);

        return BuildAuthResponse(user, user.Role?.Name ?? string.Empty, rawRefreshToken);
    }

    public async Task RevokeSessionAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var hashedRefreshToken = HashRefreshToken(refreshToken.Trim());
        var user = await _context.Users.SingleOrDefaultAsync(x => x.RefreshToken == hashedRefreshToken, cancellationToken);
        if (user is null)
        {
            return;
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthenticatedUserDto> GetAuthenticatedUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == userId && x.IsActive, cancellationToken)
            ?? throw new AppException("Authenticated user could not be resolved.", 401);

        return new AuthenticatedUserDto(user.Id, user.FullName, user.Email, user.Role?.Name ?? string.Empty);
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var responseMessage = "If an account exists for this email, a password reset link has been sent.";
        var user = await _context.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            _logger.LogInformation("Password reset requested for non-existent or inactive account {Email}.", email);
            return new ForgotPasswordResponse(responseMessage);
        }

        var now = DateTime.UtcNow;
        var activeTokens = await _context.ResetTokens
            .Where(x => x.UserId == user.Id && x.UsedAt == null && x.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var activeToken in activeTokens)
        {
            activeToken.UsedAt = now;
        }

        var rawToken = GenerateResetToken();
        var resetToken = new ResetToken
        {
            UserId = user.Id,
            TokenHash = HashResetToken(rawToken),
            ExpiresAt = now.AddMinutes(_passwordResetSettings.TokenExpiryMinutes)
        };

        _context.ResetTokens.Add(resetToken);
        await _context.SaveChangesAsync(cancellationToken);
        await _passwordResetNotifier.SendPasswordResetAsync(user.Email, user.FullName, rawToken, cancellationToken);

        return new ForgotPasswordResponse(responseMessage);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var hashedToken = HashResetToken(request.Token.Trim());
        var resetToken = await _context.ResetTokens
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == hashedToken, cancellationToken)
            ?? throw new AppException("Reset token is invalid or has expired.", 400);

        if (resetToken.UsedAt.HasValue || resetToken.ExpiresAt <= now)
        {
            throw new AppException("Reset token is invalid or has expired.", 400);
        }

        var user = resetToken.User;
        if (user is null || !user.IsActive)
        {
            throw new AppException("User account is not available for password reset.", 400);
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        resetToken.UsedAt = now;

        var siblingTokens = await _context.ResetTokens
            .Where(x => x.UserId == user.Id && x.Id != resetToken.Id && x.UsedAt == null && x.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var siblingToken in siblingTokens)
        {
            siblingToken.UsedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private AuthResponse BuildAuthResponse(User user, string role, string rawRefreshToken)
    {
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.FullName, role);
        return new AuthResponse(accessToken, rawRefreshToken, new AuthenticatedUserDto(user.Id, user.FullName, user.Email, role));
    }

    private static string HashRefreshToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private static string GenerateResetToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashResetToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}

public sealed class RoleService : IRoleService
{
    private readonly AppDbContext _context;

    public RoleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<RoleResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken)
    {
        return await _context.Roles.AsNoTracking().OrderBy(x => x.Name).ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<RoleResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Role not found.", 404);

        return role.ToResponse();
    }

    public async Task<RoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await _context.Roles.AnyAsync(x => x.Name == name, cancellationToken))
        {
            throw new AppException("Role already exists.");
        }

        var role = new Role { Name = name };
        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);
        return role.ToResponse();
    }

    public async Task<RoleResponse> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Role not found.", 404);

        var name = request.Name.Trim();
        if (await _context.Roles.AnyAsync(x => x.Id != id && x.Name == name, cancellationToken))
        {
            throw new AppException("Role name is already in use.");
        }

        role.Name = name;
        await _context.SaveChangesAsync(cancellationToken);
        return role.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.Include(x => x.Users).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Role not found.", 404);

        if (role.Users.Count > 0)
        {
            throw new AppException("Role cannot be deleted while assigned to users.");
        }

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public sealed class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(AppDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<PagedResponse<UserResponse>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken)
    {
        return await _context.Users.AsNoTracking()
            .Include(x => x.Role)
            .OrderBy(x => x.FullName)
            .ToPagedResponseAsync(request, cancellationToken, x => x.ToResponse());
    }

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _context.Users.AsNoTracking().Include(x => x.Role).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("User not found.", 404);

        return user.ToResponse();
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        await EnsureRoleExistsAsync(request.RoleId, cancellationToken);
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new AppException("A user with this email already exists.");
        }

        var user = new User
        {
            RoleId = request.RoleId,
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Phone = request.Phone?.Trim(),
            Address = request.Address?.Trim(),
            IsActive = request.IsActive
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("User not found.", 404);

        await EnsureRoleExistsAsync(request.RoleId, cancellationToken);
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(x => x.Id != id && x.Email == email, cancellationToken))
        {
            throw new AppException("A user with this email already exists.");
        }

        user.RoleId = request.RoleId;
        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.Phone = request.Phone?.Trim();
        user.Address = request.Address?.Trim();
        user.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _context.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("User not found.", 404);

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureRoleExistsAsync(Guid roleId, CancellationToken cancellationToken)
    {
        if (!await _context.Roles.AnyAsync(x => x.Id == roleId, cancellationToken))
        {
            throw new AppException("Role not found.", 404);
        }
    }
}
