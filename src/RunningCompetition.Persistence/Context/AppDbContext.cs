using Microsoft.EntityFrameworkCore;
using RunningCompetition.Domain.Entities;

namespace RunningCompetition.Persistence.Context;

/// <summary>
/// The primary EF Core DbContext for the Running Competition application.
/// Implements soft-delete filtering, audit tracking, and PostgreSQL optimizations.
/// </summary>
public class AppDbContext : DbContext
{
    /// <inheritdoc />
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Auth
    /// <summary>Gets or sets the Users DbSet.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets or sets the Roles DbSet.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Gets or sets the Permissions DbSet.</summary>
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <summary>Gets or sets the RolePermissions DbSet.</summary>
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    /// <summary>Gets or sets the UserRoles DbSet.</summary>
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    /// <summary>Gets or sets the RefreshTokens DbSet.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Running
    /// <summary>Gets or sets the RunSessions DbSet.</summary>
    public DbSet<RunSession> RunSessions => Set<RunSession>();

    /// <summary>Gets or sets the GpsLocations DbSet.</summary>
    public DbSet<GpsLocation> GpsLocations => Set<GpsLocation>();

    /// <summary>Gets or sets the RunLaps DbSet.</summary>
    public DbSet<RunLap> RunLaps => Set<RunLap>();

    /// <summary>Gets or sets the RunPauses DbSet.</summary>
    public DbSet<RunPause> RunPauses => Set<RunPause>();

    // Social
    /// <summary>Gets or sets the Friendships DbSet.</summary>
    public DbSet<Friendship> Friendships => Set<Friendship>();

    // Referral
    /// <summary>Gets or sets the Referrals DbSet.</summary>
    public DbSet<Referral> Referrals => Set<Referral>();

    // Notifications
    /// <summary>Gets or sets the Notifications DbSet.</summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    // Achievements
    /// <summary>Gets or sets the Achievements DbSet.</summary>
    public DbSet<Achievement> Achievements => Set<Achievement>();

    /// <summary>Gets or sets the UserAchievements DbSet.</summary>
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();

    // Leaderboards
    /// <summary>Gets or sets the LeaderboardEntries DbSet.</summary>
    public DbSet<LeaderboardEntry> LeaderboardEntries => Set<LeaderboardEntry>();

    // Admin
    /// <summary>Gets or sets the Announcements DbSet.</summary>
    public DbSet<Announcement> Announcements => Set<Announcement>();

    /// <summary>Gets or sets the SystemSettings DbSet.</summary>
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    /// <summary>Gets or sets the AiSettings DbSet.</summary>
    public DbSet<AiSetting> AiSettings => Set<AiSetting>();

    // Audit
    /// <summary>Gets or sets the AuditLogs DbSet.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the Persistence assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filter: exclude soft-deleted records by default
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var isDeletedProperty = entityType.FindProperty("IsDeleted");
            if (isDeletedProperty != null && isDeletedProperty.ClrType == typeof(bool))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, "IsDeleted");
                var condition = System.Linq.Expressions.Expression.Equal(property,
                    System.Linq.Expressions.Expression.Constant(false));
                var lambda = System.Linq.Expressions.Expression.Lambda(condition, parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
