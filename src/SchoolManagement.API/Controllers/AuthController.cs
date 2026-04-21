using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.API.Common;
using SchoolManagement.Application.Authentication;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuthCookieService _authCookieService;

    public AuthController(IAuthService authService, IAuthCookieService authCookieService)
    {
        _authService = authService;
        _authCookieService = authCookieService;
    }

    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    [RateLimit(10, 300)] // 10 requests per 5 minutes
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Ok(response, "Registration completed successfully."));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [RateLimit(10, 300)] // 10 requests per 5 minutes
    public async Task<ActionResult<ApiResponse<AuthenticatedUserDto>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        _authCookieService.AppendSessionCookies(Response, response);
        return Ok(ApiResponse<AuthenticatedUserDto>.Ok(response.User, "Login successful."));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [RateLimit(10, 300)] // 10 requests per 5 minutes
    public async Task<ActionResult<ApiResponse<AuthenticatedUserDto>>> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(AuthCookieNames.RefreshToken, out var refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(ApiResponse<object>.Fail("Refresh token is invalid or expired.", traceId: HttpContext.TraceIdentifier));
        }

        var response = await _authService.RefreshAsync(refreshToken, cancellationToken);
        _authCookieService.AppendSessionCookies(Response, response);
        return Ok(ApiResponse<AuthenticatedUserDto>.Ok(response.User, "Token refreshed successfully."));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Logout(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(AuthCookieNames.RefreshToken, out var refreshToken);
        await _authService.RevokeSessionAsync(refreshToken, cancellationToken);
        _authCookieService.ClearSessionCookies(Response);
        return Ok(ApiResponse<object>.Ok(null, "Logged out successfully."));
    }

    [HttpGet("session")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AuthenticatedUserDto>>> Session(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User id claim is missing.");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User id claim is invalid.");
        }

        var response = await _authService.GetAuthenticatedUserAsync(userId, cancellationToken);
        return Ok(ApiResponse<AuthenticatedUserDto>.Ok(response, "Session loaded successfully."));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [RateLimit(3, 300)] // 3 requests per 5 minutes
    public async Task<ActionResult<ApiResponse<ForgotPasswordResponse>>> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<ForgotPasswordResponse>.Ok(response, response.Message));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [RateLimit(3, 300)] // 3 requests per 5 minutes
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Password has been reset successfully."));
    }
}
