using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RunningCompetition.Application.DTOs.Auth;
using RunningCompetition.Application.Services;
using RunningCompetition.Shared.Common;

namespace RunningCompetition.API.Controllers.V1;

/// <summary>Handles user authentication: register, login, token refresh, logout, and password management.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
[EnableRateLimiting("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    /// <summary>Initializes a new instance of <see cref="AuthController"/>.</summary>
    public AuthController(AuthService authService) => _authService = authService;

    private string? IpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();

    /// <summary>Registers a new user account.</summary>
    /// <param name="request">Registration details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Auth tokens and user summary.</returns>
    /// <response code="201">User registered successfully.</response>
    /// <response code="409">Email already in use.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<AuthResponse>.Created(result, "Registration successful. Please verify your email."));
    }

    /// <summary>Authenticates a user and returns JWT + refresh tokens.</summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Auth tokens and user summary.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid credentials or account locked.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, IpAddress, cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Login successful."));
    }

    /// <summary>Refreshes a JWT access token using a valid refresh token.</summary>
    /// <param name="request">Refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New auth tokens.</returns>
    /// <response code="200">Token refreshed successfully.</response>
    /// <response code="401">Invalid or expired refresh token.</response>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request, IpAddress, cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Token refreshed."));
    }

    /// <summary>Logs out the current user by revoking the refresh token.</summary>
    /// <param name="request">Refresh token to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Logout successful.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        await _authService.LogoutAsync(request, userId, cancellationToken);
        return Ok(ApiResponse.Ok("Logged out successfully."));
    }

    /// <summary>Changes the authenticated user's password.</summary>
    /// <param name="request">Current and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Password changed successfully.</response>
    /// <response code="400">Current password incorrect or validation failed.</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        await _authService.ChangePasswordAsync(request, userId, cancellationToken);
        return Ok(ApiResponse.Ok("Password changed successfully."));
    }

    /// <summary>Initiates a password reset flow by sending a reset email.</summary>
    /// <param name="request">Email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Reset email sent (if account exists).</response>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok("If an account exists with that email, a reset link has been sent."));
    }

    /// <summary>Completes a password reset using the token from the reset email.</summary>
    /// <param name="request">Reset token and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Password reset successfully.</response>
    /// <response code="400">Invalid or expired token.</response>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok("Password reset successfully."));
    }

    /// <summary>Verifies a user's email address using the verification token.</summary>
    /// <param name="token">Email verification token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Email verified successfully.</response>
    /// <response code="400">Invalid or expired token.</response>
    [HttpGet("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token, CancellationToken cancellationToken)
    {
        await _authService.VerifyEmailAsync(new VerifyEmailRequest(token), cancellationToken);
        return Ok(ApiResponse.Ok("Email verified successfully. You can now log in."));
    }
}
