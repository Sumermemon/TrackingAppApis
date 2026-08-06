using RunningCompetition.Domain.Common;
using RunningCompetition.Domain.Enums;

namespace RunningCompetition.Domain.Entities;

/// <summary>Represents a badge/achievement definition.</summary>
public class Achievement : BaseEntity
{
    /// <summary>Gets or sets the badge type.</summary>
    public BadgeType BadgeType { get; set; }

    /// <summary>Gets or sets the unique code for this achievement.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the achievement name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the icon URL.</summary>
    public string? IconUrl { get; set; }

    /// <summary>Gets or sets the XP points awarded for this achievement.</summary>
    public int XpReward { get; set; }

    /// <summary>Gets or sets the threshold value (e.g., 5000 meters for 5K badge).</summary>
    public double? ThresholdValue { get; set; }

    /// <summary>Gets or sets whether this achievement is active.</summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    /// <summary>Gets or sets the users who have earned this achievement.</summary>
    public ICollection<UserAchievement> UserAchievements { get; set; } = [];
}

/// <summary>Represents an achievement earned by a user.</summary>
public class UserAchievement : BaseEntity
{
    /// <summary>Gets or sets the user ID.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the achievement ID.</summary>
    public Guid AchievementId { get; set; }

    /// <summary>Gets or sets when the achievement was earned.</summary>
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the run session that triggered this achievement.</summary>
    public Guid? RunSessionId { get; set; }

    // Navigation
    /// <summary>Gets or sets the user.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the achievement definition.</summary>
    public Achievement Achievement { get; set; } = null!;
}

/// <summary>Represents a leaderboard snapshot entry (cached aggregation).</summary>
public class LeaderboardEntry : BaseEntity
{
    /// <summary>Gets or sets the user ID.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the leaderboard period.</summary>
    public LeaderboardPeriod Period { get; set; }

    /// <summary>Gets or sets the leaderboard scope.</summary>
    public LeaderboardScope Scope { get; set; }

    /// <summary>Gets or sets the scope value (e.g., city name, country code).</summary>
    public string? ScopeValue { get; set; }

    /// <summary>Gets or sets the user's rank.</summary>
    public int Rank { get; set; }

    /// <summary>Gets or sets total distance in meters for this period.</summary>
    public double TotalDistanceMeters { get; set; }

    /// <summary>Gets or sets total runs for this period.</summary>
    public int TotalRuns { get; set; }

    /// <summary>Gets or sets total duration in seconds for this period.</summary>
    public long TotalDurationSeconds { get; set; }

    /// <summary>Gets or sets the snapshot creation time.</summary>
    public DateTime SnapshotAt { get; set; } = DateTime.UtcNow;

    // Navigation
    /// <summary>Gets or sets the user.</summary>
    public User User { get; set; } = null!;
}
