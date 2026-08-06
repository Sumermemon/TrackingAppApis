using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RunningCompetition.Domain.Entities;

namespace RunningCompetition.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="RunSession"/>.</summary>
internal sealed class RunSessionConfiguration : IEntityTypeConfiguration<RunSession>
{
    public void Configure(EntityTypeBuilder<RunSession> builder)
    {
        builder.ToTable("run_sessions");
        builder.HasKey(rs => rs.Id);
        builder.Property(rs => rs.Notes).HasMaxLength(2000);
        builder.Property(rs => rs.WeatherCondition).HasMaxLength(100);
        builder.Property(rs => rs.DistanceMeters).HasPrecision(12, 2);
        builder.Property(rs => rs.CaloriesBurned).HasPrecision(10, 2);

        builder.HasIndex(rs => rs.UserId).HasDatabaseName("ix_run_sessions_user_id");
        builder.HasIndex(rs => rs.Status).HasDatabaseName("ix_run_sessions_status");
        builder.HasIndex(rs => rs.StartedAt).HasDatabaseName("ix_run_sessions_started_at");
        builder.HasIndex(rs => new { rs.UserId, rs.StartedAt }).HasDatabaseName("ix_run_sessions_user_started");

        builder.HasMany(rs => rs.GpsLocations).WithOne(g => g.RunSession).HasForeignKey(g => g.RunSessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(rs => rs.Laps).WithOne(l => l.RunSession).HasForeignKey(l => l.RunSessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(rs => rs.Pauses).WithOne(p => p.RunSession).HasForeignKey(p => p.RunSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>EF Core Fluent API configuration for <see cref="GpsLocation"/>.</summary>
internal sealed class GpsLocationConfiguration : IEntityTypeConfiguration<GpsLocation>
{
    public void Configure(EntityTypeBuilder<GpsLocation> builder)
    {
        builder.ToTable("gps_locations");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Latitude).HasPrecision(10, 7);
        builder.Property(g => g.Longitude).HasPrecision(10, 7);
        builder.Property(g => g.AltitudeMeters).HasPrecision(8, 2);
        builder.Property(g => g.AccuracyMeters).HasPrecision(6, 2);
        builder.Property(g => g.SpeedMs).HasPrecision(6, 3);

        // Composite index for efficient GPS retrieval ordered by sequence
        builder.HasIndex(g => new { g.RunSessionId, g.Sequence })
            .HasDatabaseName("ix_gps_locations_session_sequence");
    }
}

/// <summary>EF Core Fluent API configuration for <see cref="RunLap"/>.</summary>
internal sealed class RunLapConfiguration : IEntityTypeConfiguration<RunLap>
{
    public void Configure(EntityTypeBuilder<RunLap> builder)
    {
        builder.ToTable("run_laps");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.DistanceMeters).HasPrecision(10, 2);
        builder.HasIndex(l => l.RunSessionId).HasDatabaseName("ix_run_laps_session_id");
    }
}

/// <summary>EF Core Fluent API configuration for <see cref="RunPause"/>.</summary>
internal sealed class RunPauseConfiguration : IEntityTypeConfiguration<RunPause>
{
    public void Configure(EntityTypeBuilder<RunPause> builder)
    {
        builder.ToTable("run_pauses");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.RunSessionId).HasDatabaseName("ix_run_pauses_session_id");
    }
}

/// <summary>EF Core Fluent API configuration for <see cref="Friendship"/>.</summary>
internal sealed class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.ToTable("friendships");
        builder.HasKey(f => f.Id);
        builder.HasIndex(f => new { f.RequesterId, f.AddresseeId }).IsUnique().HasDatabaseName("ix_friendships_pair");
        builder.HasIndex(f => f.Status).HasDatabaseName("ix_friendships_status");
        builder.HasIndex(f => f.AddresseeId).HasDatabaseName("ix_friendships_addressee");
    }
}

/// <summary>EF Core Fluent API configuration for <see cref="Referral"/>.</summary>
internal sealed class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> builder)
    {
        builder.ToTable("referrals");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ReferralCode).HasMaxLength(20).IsRequired();
        builder.HasIndex(r => r.ReferrerId).HasDatabaseName("ix_referrals_referrer");
        builder.HasIndex(r => r.ReferredUserId).IsUnique().HasDatabaseName("ix_referrals_referred_user");
        builder.HasOne(r => r.ReferredUser).WithMany().HasForeignKey(r => r.ReferredUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>EF Core Fluent API configuration for <see cref="Notification"/>.</summary>
internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.Data).HasColumnType("jsonb");
        builder.HasIndex(n => new { n.UserId, n.IsRead }).HasDatabaseName("ix_notifications_user_read");
        builder.HasIndex(n => n.CreatedAt).HasDatabaseName("ix_notifications_created_at");
    }
}

/// <summary>EF Core Fluent API configuration for <see cref="Achievement"/>.</summary>
internal sealed class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.ToTable("achievements");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Code).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.IconUrl).HasMaxLength(1024);
        builder.HasIndex(a => a.Code).IsUnique().HasDatabaseName("ix_achievements_code");
    }
}

/// <summary>EF Core Fluent API configuration for <see cref="UserAchievement"/>.</summary>
internal sealed class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement>
{
    public void Configure(EntityTypeBuilder<UserAchievement> builder)
    {
        builder.ToTable("user_achievements");
        builder.HasKey(ua => ua.Id);
        builder.HasIndex(ua => new { ua.UserId, ua.AchievementId }).IsUnique().HasDatabaseName("ix_user_achievements_pair");
        builder.HasOne(ua => ua.Achievement).WithMany(a => a.UserAchievements).HasForeignKey(ua => ua.AchievementId).OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>EF Core Fluent API configuration for <see cref="LeaderboardEntry"/>.</summary>
internal sealed class LeaderboardEntryConfiguration : IEntityTypeConfiguration<LeaderboardEntry>
{
    public void Configure(EntityTypeBuilder<LeaderboardEntry> builder)
    {
        builder.ToTable("leaderboard_entries");
        builder.HasKey(le => le.Id);
        builder.Property(le => le.ScopeValue).HasMaxLength(100);
        builder.Property(le => le.TotalDistanceMeters).HasPrecision(15, 2);
        builder.HasIndex(le => new { le.Period, le.Scope, le.ScopeValue, le.Rank })
            .HasDatabaseName("ix_leaderboard_period_scope_rank");
        builder.HasIndex(le => new { le.UserId, le.Period, le.Scope })
            .HasDatabaseName("ix_leaderboard_user_period_scope");
    }
}

/// <summary>EF Core Fluent API configuration for <see cref="Announcement"/>.</summary>
internal sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("announcements");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).HasMaxLength(300).IsRequired();
        builder.Property(a => a.Body).HasMaxLength(5000).IsRequired();
        builder.HasIndex(a => a.IsPublished).HasDatabaseName("ix_announcements_published");
    }
}

/// <summary>EF Core Fluent API configuration for <see cref="SystemSetting"/>.</summary>
internal sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Key).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Label).HasMaxLength(300).IsRequired();
        builder.Property(s => s.Group).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Value).IsRequired();
        builder.HasIndex(s => s.Key).IsUnique().HasDatabaseName("ix_system_settings_key");
    }
}

/// <summary>EF Core Fluent API configuration for <see cref="AuditLog"/>.</summary>
internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.UserEmail).HasMaxLength(256);
        builder.Property(a => a.EntityType).HasMaxLength(200).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(100);
        builder.Property(a => a.IpAddress).HasMaxLength(50);
        builder.Property(a => a.UserAgent).HasMaxLength(500);
        builder.Property(a => a.OldValues).HasColumnType("jsonb");
        builder.Property(a => a.NewValues).HasColumnType("jsonb");
        builder.Property(a => a.Metadata).HasColumnType("jsonb");

        builder.HasIndex(a => a.UserId).HasDatabaseName("ix_audit_logs_user_id");
        builder.HasIndex(a => a.EntityType).HasDatabaseName("ix_audit_logs_entity_type");
        builder.HasIndex(a => a.CreatedAt).HasDatabaseName("ix_audit_logs_created_at");
    }
}
