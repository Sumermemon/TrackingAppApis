using Microsoft.EntityFrameworkCore;
using RunningCompetition.Contracts.Repositories;
using RunningCompetition.Domain.Entities;
using RunningCompetition.Domain.Enums;
using RunningCompetition.Persistence.Context;
using RunningCompetition.Shared.Common;

namespace RunningCompetition.Persistence.Repositories;

/// <summary>User repository implementation.</summary>
public sealed class UserRepository : Repository<User>, IUserRepository
{
    /// <inheritdoc />
    public UserRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await DbSet
            .FirstOrDefaultAsync(u => u.EmailNormalized == email.ToUpperInvariant(), cancellationToken);

    /// <inheritdoc />
    public async Task<User?> GetByReferralCodeAsync(string code, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(u => u.ReferralCode == code, cancellationToken);

    /// <inheritdoc />
    public async Task<User?> GetWithRolesAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    /// <inheritdoc />
    public async Task<User?> GetWithAchievementsAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(u => u.Achievements)
            .ThenInclude(ua => ua.Achievement)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    /// <inheritdoc />
    public async Task<PagedList<User>> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var normalized = query.ToUpperInvariant();
        var q = DbSet.AsNoTracking()
            .Where(u => u.EmailNormalized.Contains(normalized)
                || (u.FirstName + " " + u.LastName).ToUpper().Contains(normalized));

        var count = await q.CountAsync(cancellationToken);
        var items = await q.OrderBy(u => u.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<User>(items, count, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetUserIdsByRoleAsync(string roleName, CancellationToken cancellationToken = default)
        => await Context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.Role.NormalizedName == roleName.ToUpperInvariant())
            .Select(ur => ur.UserId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<(int TotalUsers, int ActiveUsers, int PremiumUsers)> GetUserStatsAsync(CancellationToken cancellationToken = default)
    {
        var total = await DbSet.CountAsync(cancellationToken);
        var active = await DbSet.CountAsync(u => u.Status == UserStatus.Active, cancellationToken);
        var premium = await DbSet.CountAsync(u => u.IsPremium, cancellationToken);
        return (total, active, premium);
    }
}

/// <summary>Run session repository implementation.</summary>
public sealed class RunSessionRepository : Repository<RunSession>, IRunSessionRepository
{
    /// <inheritdoc />
    public RunSessionRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<RunSession?> GetActiveSessionAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(
            rs => rs.UserId == userId && (rs.Status == RunStatus.InProgress || rs.Status == RunStatus.Paused),
            cancellationToken);

    /// <inheritdoc />
    public async Task<PagedList<RunSession>> GetByUserAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var q = DbSet.AsNoTracking()
            .Where(rs => rs.UserId == userId)
            .OrderByDescending(rs => rs.StartedAt);

        var count = await q.CountAsync(cancellationToken);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedList<RunSession>(items, count, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<RunSession?> GetWithDetailsAsync(Guid sessionId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(rs => rs.GpsLocations.OrderBy(g => g.Sequence))
            .Include(rs => rs.Laps.OrderBy(l => l.LapNumber))
            .Include(rs => rs.Pauses)
            .FirstOrDefaultAsync(rs => rs.Id == sessionId, cancellationToken);

    /// <inheritdoc />
    public async Task BatchInsertGpsLocationsAsync(IEnumerable<GpsLocation> locations, CancellationToken cancellationToken = default)
    {
        await Context.GpsLocations.AddRangeAsync(locations, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(double TotalDistance, long TotalDuration, int TotalRuns, double TotalCalories)> GetStatsAsync(
        Guid userId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var stats = await DbSet.AsNoTracking()
            .Where(rs => rs.UserId == userId
                && rs.Status == RunStatus.Completed
                && rs.StartedAt >= from && rs.StartedAt <= to)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalDistance = g.Sum(rs => rs.DistanceMeters),
                TotalDuration = g.Sum(rs => rs.DurationSeconds),
                TotalRuns = g.Count(),
                TotalCalories = g.Sum(rs => rs.CaloriesBurned)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return stats is null
            ? (0, 0, 0, 0)
            : (stats.TotalDistance, stats.TotalDuration, stats.TotalRuns, stats.TotalCalories);
    }

    /// <inheritdoc />
    public async Task<int> GetActiveRunsCountAsync(CancellationToken cancellationToken = default)
        => await DbSet.CountAsync(rs => rs.Status == RunStatus.InProgress, cancellationToken);

    /// <inheritdoc />
    public async Task<double> GetTodayTotalDistanceAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await DbSet.AsNoTracking()
            .Where(rs => rs.Status == RunStatus.Completed && rs.StartedAt >= today)
            .SumAsync(rs => rs.DistanceMeters, cancellationToken);
    }
}

/// <summary>Friendship repository implementation.</summary>
public sealed class FriendshipRepository : Repository<Friendship>, IFriendshipRepository
{
    /// <inheritdoc />
    public FriendshipRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<Friendship?> GetFriendshipAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(
            f => (f.RequesterId == userId1 && f.AddresseeId == userId2) ||
                 (f.RequesterId == userId2 && f.AddresseeId == userId1),
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetFriendsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var friendIds = await DbSet.AsNoTracking()
            .Where(f => f.Status == FriendRequestStatus.Accepted &&
                       (f.RequesterId == userId || f.AddresseeId == userId))
            .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
            .ToListAsync(cancellationToken);

        return await Context.Users.AsNoTracking()
            .Where(u => friendIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Friendship>> GetPendingRequestsAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(f => f.Requester)
            .Where(f => f.AddresseeId == userId && f.Status == FriendRequestStatus.Pending)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> AreFriendsAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default)
        => await DbSet.AnyAsync(
            f => f.Status == FriendRequestStatus.Accepted &&
                ((f.RequesterId == userId1 && f.AddresseeId == userId2) ||
                 (f.RequesterId == userId2 && f.AddresseeId == userId1)),
            cancellationToken);
}

/// <summary>Notification repository implementation.</summary>
public sealed class NotificationRepository : Repository<Notification>, INotificationRepository
{
    /// <inheritdoc />
    public NotificationRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<PagedList<Notification>> GetByUserAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var q = DbSet.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var count = await q.CountAsync(cancellationToken);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedList<Notification>(items, count, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    /// <inheritdoc />
    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow),
            cancellationToken);
    }
}

/// <summary>Referral repository implementation.</summary>
public sealed class ReferralRepository : Repository<Referral>, IReferralRepository
{
    /// <inheritdoc />
    public ReferralRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Referral>> GetByReferrerAsync(Guid referrerId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(r => r.ReferredUser)
            .Where(r => r.ReferrerId == referrerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<(int TotalReferrals, int RewardedReferrals, int TotalXpEarned)> GetStatsAsync(Guid referrerId, CancellationToken cancellationToken = default)
    {
        var stats = await DbSet.AsNoTracking()
            .Where(r => r.ReferrerId == referrerId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Rewarded = g.Count(r => r.RewardGranted),
                XpEarned = g.Sum(r => r.XpAwarded)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return stats is null ? (0, 0, 0) : (stats.Total, stats.Rewarded, stats.XpEarned);
    }
}

/// <summary>Achievement repository implementation.</summary>
public sealed class AchievementRepository : Repository<Achievement>, IAchievementRepository
{
    /// <inheritdoc />
    public AchievementRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Achievement>> GetUnearnedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var earnedIds = await Context.UserAchievements.AsNoTracking()
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.AchievementId)
            .ToListAsync(cancellationToken);

        return await DbSet.AsNoTracking()
            .Where(a => a.IsActive && !earnedIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserAchievement>> GetUserAchievementsAsync(Guid userId, CancellationToken cancellationToken = default)
        => await Context.UserAchievements.AsNoTracking()
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UserId == userId)
            .OrderByDescending(ua => ua.EarnedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasEarnedAsync(Guid userId, BadgeType badgeType, CancellationToken cancellationToken = default)
        => await Context.UserAchievements.AnyAsync(
            ua => ua.UserId == userId && ua.Achievement.BadgeType == badgeType,
            cancellationToken);
}

/// <summary>Refresh token repository implementation.</summary>
public sealed class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    /// <inheritdoc />
    public RefreshTokenRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        => await DbSet.Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

    /// <inheritdoc />
    public async Task RevokeAllForUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow)
                .SetProperty(rt => rt.RevokedReason, reason),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RefreshToken>> GetActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
}

/// <summary>Audit log repository implementation.</summary>
public sealed class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    /// <inheritdoc />
    public AuditLogRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<PagedList<AuditLog>> GetPagedAsync(
        int page, int pageSize,
        Guid? userId = null,
        string? entityType = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var q = DbSet.AsNoTracking().AsQueryable();
        if (userId.HasValue) q = q.Where(a => a.UserId == userId.Value);
        if (!string.IsNullOrEmpty(entityType)) q = q.Where(a => a.EntityType == entityType);
        if (from.HasValue) q = q.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(a => a.CreatedAt <= to.Value);

        q = q.OrderByDescending(a => a.CreatedAt);
        var count = await q.CountAsync(cancellationToken);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedList<AuditLog>(items, count, page, pageSize);
    }
}

/// <summary>Leaderboard repository implementation.</summary>
public sealed class LeaderboardRepository : Repository<LeaderboardEntry>, ILeaderboardRepository
{
    /// <inheritdoc />
    public LeaderboardRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LeaderboardEntry>> GetLeaderboardAsync(
        LeaderboardPeriod period,
        LeaderboardScope scope,
        string? scopeValue,
        int top,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(le => le.User)
            .Where(le => le.Period == period && le.Scope == scope &&
                        (scopeValue == null || le.ScopeValue == scopeValue))
            .OrderBy(le => le.Rank)
            .Take(top)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<int?> GetUserRankAsync(Guid userId, LeaderboardPeriod period, LeaderboardScope scope, string? scopeValue, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(le => le.UserId == userId && le.Period == period &&
                                      le.Scope == scope && (scopeValue == null || le.ScopeValue == scopeValue),
            cancellationToken);
        return entry?.Rank;
    }

    /// <inheritdoc />
    public async Task RebuildSnapshotAsync(LeaderboardPeriod period, CancellationToken cancellationToken = default)
    {
        // Delete old snapshots for this period
        await DbSet.Where(le => le.Period == period)
            .ExecuteDeleteAsync(cancellationToken);

        // Determine date range
        var (from, to) = GetDateRange(period);

        // Build aggregated rankings from run sessions
        var rankings = await Context.RunSessions.AsNoTracking()
            .Where(rs => rs.Status == RunStatus.Completed && rs.StartedAt >= from && rs.StartedAt <= to)
            .GroupBy(rs => rs.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalDistance = g.Sum(rs => rs.DistanceMeters),
                TotalRuns = g.Count(),
                TotalDuration = g.Sum(rs => rs.DurationSeconds)
            })
            .OrderByDescending(x => x.TotalDistance)
            .ToListAsync(cancellationToken);

        var entries = rankings.Select((r, idx) => new LeaderboardEntry
        {
            UserId = r.UserId,
            Period = period,
            Scope = LeaderboardScope.Global,
            Rank = idx + 1,
            TotalDistanceMeters = r.TotalDistance,
            TotalRuns = r.TotalRuns,
            TotalDurationSeconds = r.TotalDuration,
            SnapshotAt = DateTime.UtcNow
        });

        await Context.LeaderboardEntries.AddRangeAsync(entries, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
    }

    private static (DateTime From, DateTime To) GetDateRange(LeaderboardPeriod period)
    {
        var now = DateTime.UtcNow;
        return period switch
        {
            LeaderboardPeriod.Daily => (now.Date, now),
            LeaderboardPeriod.Weekly => (now.AddDays(-(int)now.DayOfWeek).Date, now),
            LeaderboardPeriod.Monthly => (new DateTime(now.Year, now.Month, 1), now),
            LeaderboardPeriod.Yearly => (new DateTime(now.Year, 1, 1), now),
            _ => (DateTime.MinValue, now)
        };
    }
}
