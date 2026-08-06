namespace RunningCompetition.Domain.Enums;

/// <summary>User gender options.</summary>
public enum Gender { NotSpecified, Male, Female, Other }

/// <summary>User account status.</summary>
public enum UserStatus { Active, Inactive, Suspended, PendingVerification }

/// <summary>Run session status.</summary>
public enum RunStatus { NotStarted, InProgress, Paused, Completed, Abandoned }

/// <summary>Friend request status.</summary>
public enum FriendRequestStatus { Pending, Accepted, Rejected }

/// <summary>Notification type.</summary>
public enum NotificationType
{
    FriendRequest,
    FriendRequestAccepted,
    ChallengeInvitation,
    ReferralReward,
    AchievementUnlocked,
    System,
    Announcement
}

/// <summary>Leaderboard time period.</summary>
public enum LeaderboardPeriod { Daily, Weekly, Monthly, Yearly, AllTime }

/// <summary>Leaderboard scope.</summary>
public enum LeaderboardScope { Global, City, State, Country, Friends }

/// <summary>Achievement badge type.</summary>
public enum BadgeType
{
    FirstRun,
    Distance5K,
    Distance10K,
    HalfMarathon,
    Marathon,
    Streak7Days,
    Streak30Days,
    Streak100Days,
    Speed,
    Consistency,
    Social,
    Referral,
    Custom
}

/// <summary>Reward type for referrals.</summary>
public enum RewardType { XpPoints, Badge, Premium, Custom }

/// <summary>Audit log action types.</summary>
public enum AuditAction { Create, Update, Delete, Login, Logout, PasswordChange, PermissionChange }

/// <summary>Privacy level for user profiles.</summary>
public enum PrivacyLevel { Public, FriendsOnly, Private }

/// <summary>System setting data type.</summary>
public enum SettingType { String, Integer, Boolean, Json, Decimal }

/// <summary>Running goal type.</summary>
public enum GoalType { Distance, Duration, Frequency, Weight, Speed }
