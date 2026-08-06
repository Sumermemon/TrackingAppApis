using RunningCompetition.Domain.Enums;

namespace RunningCompetition.Application.DTOs.Users;

// ─── Response DTOs ───────────────────────────────────────────────────────────

/// <summary>Full user profile response DTO.</summary>
public sealed record UserProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? ProfilePictureUrl,
    Gender Gender,
    DateOnly? DateOfBirth,
    int? Age,
    string? City,
    string? State,
    string? Country,
    decimal? HeightCm,
    decimal? WeightKg,
    GoalType? GoalType,
    decimal? GoalValue,
    PrivacyLevel ProfilePrivacy,
    PrivacyLevel ActivityPrivacy,
    string? ReferralCode,
    int XpPoints,
    int Level,
    int CurrentStreak,
    int LongestStreak,
    double TotalDistanceMeters,
    int TotalRuns,
    long TotalDurationSeconds,
    double TotalCalories,
    DateTime? LastRunAt,
    bool IsEmailVerified,
    bool IsPremium,
    UserStatus Status,
    DateTime CreatedAt);

/// <summary>Lightweight user card for lists and search results.</summary>
public sealed record UserCardDto(
    Guid Id,
    string FullName,
    string? ProfilePictureUrl,
    string? City,
    string? Country,
    int Level,
    int XpPoints,
    double TotalDistanceMeters,
    int TotalRuns,
    int CurrentStreak);

// ─── Request DTOs ────────────────────────────────────────────────────────────

/// <summary>Request to update a user's personal details.</summary>
public sealed record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    Gender Gender,
    DateOnly? DateOfBirth,
    string? City,
    string? State,
    string? Country,
    decimal? HeightCm,
    decimal? WeightKg,
    GoalType? GoalType,
    decimal? GoalValue);

/// <summary>Request to update privacy settings.</summary>
public sealed record UpdatePrivacyRequest(
    PrivacyLevel ProfilePrivacy,
    PrivacyLevel ActivityPrivacy);

/// <summary>Request to update push notification settings.</summary>
public sealed record UpdatePushSettingsRequest(
    bool PushNotificationsEnabled,
    string? PushToken);

/// <summary>Admin request to update a user's status.</summary>
public sealed record UpdateUserStatusRequest(UserStatus Status, string? Reason);

/// <summary>Admin request to assign a role to a user.</summary>
public sealed record AssignRoleRequest(Guid RoleId);
