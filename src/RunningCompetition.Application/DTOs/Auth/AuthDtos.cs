using RunningCompetition.Domain.Enums;

namespace RunningCompetition.Application.DTOs.Auth;

// ─── Request DTOs ────────────────────────────────────────────────────────────

/// <summary>Request payload for user registration.</summary>
public sealed record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? ReferralCode = null);

/// <summary>Request payload for user login.</summary>
public sealed record LoginRequest(
    string Email,
    string Password,
    string? DeviceInfo = null);

/// <summary>Request payload for token refresh.</summary>
public sealed record RefreshTokenRequest(string RefreshToken);

/// <summary>Request payload for logout.</summary>
public sealed record LogoutRequest(string RefreshToken);

/// <summary>Request payload for changing password.</summary>
public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);

/// <summary>Request payload for initiating password reset.</summary>
public sealed record ForgotPasswordRequest(string Email);

/// <summary>Request payload for completing password reset.</summary>
public sealed record ResetPasswordRequest(
    string Token,
    string NewPassword,
    string ConfirmNewPassword);

/// <summary>Request payload for email verification.</summary>
public sealed record VerifyEmailRequest(string Token);

// ─── Response DTOs ───────────────────────────────────────────────────────────

/// <summary>Authentication response returned after login or token refresh.</summary>
public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    UserSummaryDto User);

/// <summary>Summary of the authenticated user included in auth responses.</summary>
public sealed record UserSummaryDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? ProfilePictureUrl,
    IReadOnlyList<string> Roles,
    bool IsEmailVerified,
    bool IsPremium);
