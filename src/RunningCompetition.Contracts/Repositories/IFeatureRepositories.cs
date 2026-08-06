using RunningCompetition.Domain.Entities;
using RunningCompetition.Domain.Enums;
using RunningCompetition.Shared.Common;

namespace RunningCompetition.Contracts.Repositories;

/// <summary>User-specific repository operations.</summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>Finds a user by their email address.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Finds a user by their referral code.</summary>
    Task<User?> GetByReferralCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Gets a user with their roles included.</summary>
    Task<User?> GetWithRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Gets a user with their achievements included.</summary>
    Task<User?> GetWithAchievementsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Searches users by name or email.</summary>
    Task<PagedList<User>> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Gets all user IDs for a given role name.</summary>
    Task<IReadOnlyList<Guid>> GetUserIdsByRoleAsync(string roleName, CancellationToken cancellationToken = default);

    /// <summary>Gets dashboard statistics for the admin panel.</summary>
    Task<(int TotalUsers, int ActiveUsers, int PremiumUsers)> GetUserStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>Run session-specific repository operations.</summary>
public interface IRunSessionRepository : IRepository<RunSession>
{
    /// <summary>Gets the active (in-progress or paused) session for a user.</summary>
    Task<RunSession?> GetActiveSessionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Gets paginated run sessions for a user.</summary>
    Task<PagedList<RunSession>> GetByUserAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Gets a session with all GPS locations and laps.</summary>
    Task<RunSession?> GetWithDetailsAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>Batch inserts GPS locations efficiently.</summary>
    Task BatchInsertGpsLocationsAsync(IEnumerable<GpsLocation> locations, CancellationToken cancellationToken = default);

    /// <summary>Gets run statistics for a user within a date range.</summary>
    Task<(double TotalDistance, long TotalDuration, int TotalRuns, double TotalCalories)> GetStatsAsync(
        Guid userId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>Gets total active run count for the dashboard.</summary>
    Task<int> GetActiveRunsCountAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets total distance run today for the dashboard.</summary>
    Task<double> GetTodayTotalDistanceAsync(CancellationToken cancellationToken = default);
}

/// <summary>Leaderboard-specific repository operations.</summary>
public interface ILeaderboardRepository : IRepository<LeaderboardEntry>
{
    /// <summary>Gets the leaderboard for a given period and scope.</summary>
    Task<IReadOnlyList<LeaderboardEntry>> GetLeaderboardAsync(
        LeaderboardPeriod period,
        LeaderboardScope scope,
        string? scopeValue,
        int top,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a user's rank for a period and scope.</summary>
    Task<int?> GetUserRankAsync(Guid userId, LeaderboardPeriod period, LeaderboardScope scope, string? scopeValue, CancellationToken cancellationToken = default);

    /// <summary>Rebuilds leaderboard snapshots for a given period.</summary>
    Task RebuildSnapshotAsync(LeaderboardPeriod period, CancellationToken cancellationToken = default);
}

/// <summary>Friendship-specific repository operations.</summary>
public interface IFriendshipRepository : IRepository<Friendship>
{
    /// <summary>Gets the friendship status between two users.</summary>
    Task<Friendship?> GetFriendshipAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default);

    /// <summary>Gets all accepted friends for a user.</summary>
    Task<IReadOnlyList<User>> GetFriendsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Gets pending friend requests for a user.</summary>
    Task<IReadOnlyList<Friendship>> GetPendingRequestsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Checks whether two users are friends.</summary>
    Task<bool> AreFriendsAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default);
}

/// <summary>Achievement-specific repository operations.</summary>
public interface IAchievementRepository : IRepository<Achievement>
{
    /// <summary>Gets achievements not yet earned by a user.</summary>
    Task<IReadOnlyList<Achievement>> GetUnearnedAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Gets achievements recently earned by a user.</summary>
    Task<IReadOnlyList<UserAchievement>> GetUserAchievementsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Checks if a user has earned a specific achievement.</summary>
    Task<bool> HasEarnedAsync(Guid userId, BadgeType badgeType, CancellationToken cancellationToken = default);
}

/// <summary>Refresh token-specific repository operations.</summary>
public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    /// <summary>Gets a refresh token by its value.</summary>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Revokes all refresh tokens for a user.</summary>
    Task RevokeAllForUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>Gets all active refresh tokens for a user.</summary>
    Task<IReadOnlyList<RefreshToken>> GetActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>Notification-specific repository operations.</summary>
public interface INotificationRepository : IRepository<Notification>
{
    /// <summary>Gets paginated notifications for a user.</summary>
    Task<PagedList<Notification>> GetByUserAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Gets the unread notification count for a user.</summary>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Marks all notifications as read for a user.</summary>
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>Referral-specific repository operations.</summary>
public interface IReferralRepository : IRepository<Referral>
{
    /// <summary>Gets all referrals made by a user.</summary>
    Task<IReadOnlyList<Referral>> GetByReferrerAsync(Guid referrerId, CancellationToken cancellationToken = default);

    /// <summary>Gets referral statistics for a user.</summary>
    Task<(int TotalReferrals, int RewardedReferrals, int TotalXpEarned)> GetStatsAsync(Guid referrerId, CancellationToken cancellationToken = default);
}

/// <summary>Audit log-specific repository operations.</summary>
public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>Gets paginated audit logs with optional filters.</summary>
    Task<PagedList<AuditLog>> GetPagedAsync(
        int page, int pageSize,
        Guid? userId = null,
        string? entityType = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);
}
