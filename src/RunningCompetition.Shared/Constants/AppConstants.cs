namespace RunningCompetition.Shared.Constants;

/// <summary>Application-wide constants.</summary>
public static class AppConstants
{
    /// <summary>Default token expiry in minutes.</summary>
    public const int AccessTokenExpiryMinutes = 15;

    /// <summary>Default refresh token expiry in days.</summary>
    public const int RefreshTokenExpiryDays = 7;

    /// <summary>Email verification token expiry in hours.</summary>
    public const int EmailVerificationTokenExpiryHours = 24;

    /// <summary>Password reset token expiry in hours.</summary>
    public const int PasswordResetTokenExpiryHours = 2;

    /// <summary>Default leaderboard cache TTL in minutes.</summary>
    public const int LeaderboardCacheTtlMinutes = 5;

    /// <summary>Max GPS batch insert size.</summary>
    public const int GpsBatchSize = 100;

    /// <summary>Default referral code length.</summary>
    public const int ReferralCodeLength = 8;

    /// <summary>Max failed login attempts before lockout.</summary>
    public const int MaxFailedLoginAttempts = 5;

    /// <summary>Lockout duration in minutes.</summary>
    public const int LockoutDurationMinutes = 15;

    /// <summary>Default profile picture URL.</summary>
    public const string DefaultProfilePictureUrl = "https://cdn.runningapp.com/defaults/avatar.png";
}

/// <summary>Caching key templates.</summary>
public static class CacheKeys
{
    public static string Leaderboard(string period, string scope) => $"leaderboard:{period}:{scope}";
    public static string UserProfile(Guid userId) => $"user:profile:{userId}";
    public static string ActiveRun(Guid userId) => $"run:active:{userId}";
    public static string SystemSettings() => "system:settings";
    public static string UserPermissions(Guid userId) => $"user:permissions:{userId}";
    public static string FriendList(Guid userId) => $"user:friends:{userId}";
    public static string DashboardStats() => "admin:dashboard:stats";
}

/// <summary>Permission constants.</summary>
public static class Permissions
{
    public static class Users
    {
        public const string View = "users.view";
        public const string Create = "users.create";
        public const string Update = "users.update";
        public const string Delete = "users.delete";
        public const string ManageRoles = "users.manage_roles";
    }

    public static class Runs
    {
        public const string View = "runs.view";
        public const string Create = "runs.create";
        public const string Update = "runs.update";
        public const string Delete = "runs.delete";
    }

    public static class Leaderboards
    {
        public const string View = "leaderboards.view";
    }

    public static class Admin
    {
        public const string Dashboard = "admin.dashboard";
        public const string Settings = "admin.settings";
        public const string Announcements = "admin.announcements";
    }

    public static class Roles
    {
        public const string View = "roles.view";
        public const string Create = "roles.create";
        public const string Update = "roles.update";
        public const string Delete = "roles.delete";
    }
}

/// <summary>Role name constants.</summary>
public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string User = "User";
}

/// <summary>Policy name constants.</summary>
public static class PolicyNames
{
    public const string RequireSuperAdmin = "RequireSuperAdmin";
    public const string RequireAdmin = "RequireAdmin";
    public const string RequirePermission = "RequirePermission";
}
