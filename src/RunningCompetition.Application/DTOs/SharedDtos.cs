using RunningCompetition.Domain.Enums;

namespace RunningCompetition.Application.DTOs;

// ─── Leaderboard DTOs ────────────────────────────────────────────────────────

/// <summary>A single leaderboard entry shown to the user.</summary>
public sealed record LeaderboardEntryDto(
    int Rank,
    Guid UserId,
    string FullName,
    string? ProfilePictureUrl,
    string? City,
    string? Country,
    double TotalDistanceMeters,
    int TotalRuns,
    long TotalDurationSeconds);

// ─── Social DTOs ─────────────────────────────────────────────────────────────

/// <summary>Request to send a friend request.</summary>
public sealed record SendFriendRequestDto(Guid AddresseeId);

/// <summary>Request to respond to a friend request.</summary>
public sealed record RespondFriendRequestDto(Guid FriendshipId, FriendRequestStatus Response);

/// <summary>Friend list entry.</summary>
public sealed record FriendDto(
    Guid Id,
    string FullName,
    string? ProfilePictureUrl,
    string? City,
    string? Country,
    DateTime? LastRunAt,
    double TotalDistanceMeters,
    int CurrentStreak);

// ─── Referral DTOs ───────────────────────────────────────────────────────────

/// <summary>Request to apply a referral code.</summary>
public sealed record ApplyReferralCodeRequest(string ReferralCode);

/// <summary>Referral history item.</summary>
public sealed record ReferralDto(
    Guid Id,
    string ReferredUserName,
    string ReferralCode,
    bool RewardGranted,
    int XpAwarded,
    DateTime CreatedAt);

/// <summary>Referral statistics for a user.</summary>
public sealed record ReferralStatsDto(
    int TotalReferrals,
    int RewardedReferrals,
    int TotalXpEarned,
    string MyReferralCode);

// ─── Achievement DTOs ────────────────────────────────────────────────────────

/// <summary>Achievement definition DTO.</summary>
public sealed record AchievementDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string? IconUrl,
    int XpReward,
    BadgeType BadgeType);

/// <summary>User achievement DTO (earned badge).</summary>
public sealed record UserAchievementDto(
    AchievementDto Achievement,
    DateTime EarnedAt);

// ─── Notification DTOs ───────────────────────────────────────────────────────

/// <summary>Notification DTO.</summary>
public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Body,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);

// ─── Admin DTOs ──────────────────────────────────────────────────────────────

/// <summary>Admin dashboard statistics widget data.</summary>
public sealed record DashboardStatsDto(
    int TotalUsers,
    int ActiveUsers,
    int ActiveRuns,
    double TodayTotalDistanceKm,
    int TodayRuns,
    int PremiumUsers,
    IReadOnlyList<LeaderboardEntryDto> TopRunners);

/// <summary>Role management DTO.</summary>
public sealed record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    int UserCount,
    IReadOnlyList<PermissionDto> Permissions);

/// <summary>Permission DTO.</summary>
public sealed record PermissionDto(
    Guid Id,
    string Name,
    string DisplayName,
    string Group);

/// <summary>Create/update role request.</summary>
public sealed record CreateRoleRequest(
    string Name,
    string? Description,
    IReadOnlyList<Guid> PermissionIds);

/// <summary>System setting DTO.</summary>
public sealed record SystemSettingDto(
    Guid Id,
    string Key,
    string Label,
    string Value,
    string Group,
    bool IsPublic);

/// <summary>Request to update a system setting.</summary>
public sealed record UpdateSettingRequest(string Value);

/// <summary>Announcement DTO.</summary>
public sealed record AnnouncementDto(
    Guid Id,
    string Title,
    string Body,
    bool IsPublished,
    DateTime? PublishedAt,
    DateTime? ExpiresAt,
    DateTime CreatedAt);

/// <summary>Create/update announcement request.</summary>
public sealed record CreateAnnouncementRequest(
    string Title,
    string Body,
    bool Publish = false,
    DateTime? ExpiresAt = null);

/// <summary>Audit log entry DTO.</summary>
public sealed record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string? UserEmail,
    AuditAction Action,
    string EntityType,
    string? EntityId,
    string? IpAddress,
    DateTime CreatedAt);

/// <summary>AI settings DTO.</summary>
public sealed record AiSettingDto(
    Guid Id,
    string Provider,
    string Model,
    string? SystemPrompt,
    double Temperature,
    bool IsEnabled);

/// <summary>Update AI settings request.</summary>
public sealed record UpdateAiSettingRequest(
    string Provider,
    string Model,
    string ApiKey,
    string? SystemPrompt,
    double Temperature,
    bool IsEnabled);
