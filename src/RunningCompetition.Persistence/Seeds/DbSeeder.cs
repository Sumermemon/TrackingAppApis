using Microsoft.EntityFrameworkCore;
using RunningCompetition.Domain.Entities;
using RunningCompetition.Domain.Enums;
using RunningCompetition.Persistence.Context;
using RunningCompetition.Shared.Constants;

namespace RunningCompetition.Persistence.Seeds;

/// <summary>Seeds the database with initial required data.</summary>
public static class DbSeeder
{
    /// <summary>Seeds roles, permissions, super admin user, and achievements.</summary>
    public static async Task SeedAsync(AppDbContext context)
    {
        await SeedPermissionsAsync(context);
        await SeedRolesAsync(context);
        await SeedSuperAdminAsync(context);
        await SeedAchievementsAsync(context);
        await SeedSystemSettingsAsync(context);
    }

    private static async Task SeedPermissionsAsync(AppDbContext context)
    {
        if (await context.Permissions.AnyAsync()) return;

        var permissions = new[]
        {
            // Users
            new Permission { Name = Permissions.Users.View,       DisplayName = "View Users",        Group = "Users" },
            new Permission { Name = Permissions.Users.Create,     DisplayName = "Create Users",      Group = "Users" },
            new Permission { Name = Permissions.Users.Update,     DisplayName = "Update Users",      Group = "Users" },
            new Permission { Name = Permissions.Users.Delete,     DisplayName = "Delete Users",      Group = "Users" },
            new Permission { Name = Permissions.Users.ManageRoles,DisplayName = "Manage User Roles", Group = "Users" },
            // Runs
            new Permission { Name = Permissions.Runs.View,   DisplayName = "View Runs",   Group = "Runs" },
            new Permission { Name = Permissions.Runs.Create, DisplayName = "Create Runs", Group = "Runs" },
            new Permission { Name = Permissions.Runs.Update, DisplayName = "Update Runs", Group = "Runs" },
            new Permission { Name = Permissions.Runs.Delete, DisplayName = "Delete Runs", Group = "Runs" },
            // Leaderboards
            new Permission { Name = Permissions.Leaderboards.View, DisplayName = "View Leaderboards", Group = "Leaderboards" },
            // Admin
            new Permission { Name = Permissions.Admin.Dashboard,     DisplayName = "Admin Dashboard",     Group = "Admin" },
            new Permission { Name = Permissions.Admin.Settings,      DisplayName = "System Settings",     Group = "Admin" },
            new Permission { Name = Permissions.Admin.Announcements, DisplayName = "Manage Announcements",Group = "Admin" },
            // Roles
            new Permission { Name = Permissions.Roles.View,   DisplayName = "View Roles",   Group = "Roles" },
            new Permission { Name = Permissions.Roles.Create, DisplayName = "Create Roles", Group = "Roles" },
            new Permission { Name = Permissions.Roles.Update, DisplayName = "Update Roles", Group = "Roles" },
            new Permission { Name = Permissions.Roles.Delete, DisplayName = "Delete Roles", Group = "Roles" },
        };

        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(AppDbContext context)
    {
        if (await context.Roles.AnyAsync()) return;

        var allPermissions = await context.Permissions.ToListAsync();

        var superAdminRole = new Role
        {
            Name = RoleNames.SuperAdmin,
            NormalizedName = RoleNames.SuperAdmin.ToUpperInvariant(),
            Description = "Full system access",
            IsSystem = true
        };
        superAdminRole.RolePermissions = allPermissions
            .Select(p => new RolePermission { Role = superAdminRole, Permission = p })
            .ToList();

        var adminRole = new Role
        {
            Name = RoleNames.Admin,
            NormalizedName = RoleNames.Admin.ToUpperInvariant(),
            Description = "Administrative access",
            IsSystem = true
        };
        var adminPermNames = new[]
        {
            Permissions.Users.View, Permissions.Users.Update,
            Permissions.Runs.View, Permissions.Leaderboards.View,
            Permissions.Admin.Dashboard, Permissions.Admin.Announcements
        };
        adminRole.RolePermissions = allPermissions
            .Where(p => adminPermNames.Contains(p.Name))
            .Select(p => new RolePermission { Role = adminRole, Permission = p })
            .ToList();

        var userRole = new Role
        {
            Name = RoleNames.User,
            NormalizedName = RoleNames.User.ToUpperInvariant(),
            Description = "Standard user access",
            IsSystem = true
        };
        var userPermNames = new[]
        {
            Permissions.Runs.View, Permissions.Runs.Create, Permissions.Runs.Update,
            Permissions.Leaderboards.View
        };
        userRole.RolePermissions = allPermissions
            .Where(p => userPermNames.Contains(p.Name))
            .Select(p => new RolePermission { Role = userRole, Permission = p })
            .ToList();

        await context.Roles.AddRangeAsync(superAdminRole, adminRole, userRole);
        await context.SaveChangesAsync();
    }

    private static async Task SeedSuperAdminAsync(AppDbContext context)
    {
        if (await context.Users.AnyAsync(u => u.EmailNormalized == "SUPERADMIN@RUNNINGAPP.COM")) return;

        var superAdminRole = await context.Roles.FirstAsync(r => r.Name == RoleNames.SuperAdmin);

        var admin = new User
        {
            FirstName = "Super",
            LastName = "Admin",
            Email = "superadmin@runningapp.com",
            EmailNormalized = "SUPERADMIN@RUNNINGAPP.COM",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
            Status = UserStatus.Active,
            IsEmailVerified = true,
            ReferralCode = "SUPERADM1"
        };

        admin.UserRoles.Add(new UserRole { User = admin, Role = superAdminRole });

        await context.Users.AddAsync(admin);
        await context.SaveChangesAsync();
    }

    private static async Task SeedAchievementsAsync(AppDbContext context)
    {
        if (await context.Achievements.AnyAsync()) return;

        var achievements = new[]
        {
            new Achievement { BadgeType = BadgeType.FirstRun, Code = "FIRST_RUN", Name = "First Steps", Description = "Complete your first run", XpReward = 50, ThresholdValue = 1 },
            new Achievement { BadgeType = BadgeType.Distance5K, Code = "5K", Name = "5K Runner", Description = "Complete a 5K run", XpReward = 100, ThresholdValue = 5000 },
            new Achievement { BadgeType = BadgeType.Distance10K, Code = "10K", Name = "10K Runner", Description = "Complete a 10K run", XpReward = 200, ThresholdValue = 10000 },
            new Achievement { BadgeType = BadgeType.HalfMarathon, Code = "HALF_MARATHON", Name = "Half Marathon Hero", Description = "Complete a half marathon (21.1 km)", XpReward = 500, ThresholdValue = 21100 },
            new Achievement { BadgeType = BadgeType.Marathon, Code = "MARATHON", Name = "Marathon Legend", Description = "Complete a full marathon (42.2 km)", XpReward = 1000, ThresholdValue = 42200 },
            new Achievement { BadgeType = BadgeType.Streak7Days, Code = "STREAK_7", Name = "Week Warrior", Description = "Run 7 days in a row", XpReward = 150, ThresholdValue = 7 },
            new Achievement { BadgeType = BadgeType.Streak30Days, Code = "STREAK_30", Name = "Monthly Grinder", Description = "Run 30 days in a row", XpReward = 500, ThresholdValue = 30 },
            new Achievement { BadgeType = BadgeType.Streak100Days, Code = "STREAK_100", Name = "Iron Runner", Description = "Run 100 days in a row", XpReward = 2000, ThresholdValue = 100 },
            new Achievement { BadgeType = BadgeType.Referral, Code = "REFERRAL_5", Name = "Community Builder", Description = "Refer 5 friends", XpReward = 300, ThresholdValue = 5 },
        };

        await context.Achievements.AddRangeAsync(achievements);
        await context.SaveChangesAsync();
    }

    private static async Task SeedSystemSettingsAsync(AppDbContext context)
    {
        if (await context.SystemSettings.AnyAsync()) return;

        var settings = new[]
        {
            new SystemSetting { Key = "app.name", Value = "Running Competition", Label = "App Name", Group = "General", Type = SettingType.String, IsPublic = true },
            new SystemSetting { Key = "app.version", Value = "1.0.0", Label = "App Version", Group = "General", Type = SettingType.String, IsPublic = true },
            new SystemSetting { Key = "run.min_distance_meters", Value = "100", Label = "Minimum Run Distance (m)", Group = "Running", Type = SettingType.Integer },
            new SystemSetting { Key = "referral.xp_reward", Value = "100", Label = "Referral XP Reward", Group = "Referral", Type = SettingType.Integer },
            new SystemSetting { Key = "leaderboard.cache_ttl_minutes", Value = "5", Label = "Leaderboard Cache TTL (min)", Group = "Performance", Type = SettingType.Integer },
            new SystemSetting { Key = "email.verification_required", Value = "true", Label = "Email Verification Required", Group = "Auth", Type = SettingType.Boolean },
        };

        await context.SystemSettings.AddRangeAsync(settings);
        await context.SaveChangesAsync();
    }
}
