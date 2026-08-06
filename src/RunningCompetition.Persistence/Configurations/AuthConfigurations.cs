using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RunningCompetition.Domain.Entities;

namespace RunningCompetition.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="User"/>.</summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.EmailNormalized).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.ProfilePictureUrl).HasMaxLength(1024);
        builder.Property(u => u.City).HasMaxLength(100);
        builder.Property(u => u.State).HasMaxLength(100);
        builder.Property(u => u.Country).HasMaxLength(2);
        builder.Property(u => u.ReferralCode).HasMaxLength(20);
        builder.Property(u => u.PushToken).HasMaxLength(512);
        builder.Property(u => u.HeightCm).HasPrecision(5, 2);
        builder.Property(u => u.WeightKg).HasPrecision(5, 2);
        builder.Property(u => u.GoalValue).HasPrecision(10, 2);
        builder.Property(u => u.EmailVerificationToken).HasMaxLength(512);
        builder.Property(u => u.PasswordResetToken).HasMaxLength(512);

        // Indexes
        builder.HasIndex(u => u.EmailNormalized).IsUnique().HasDatabaseName("ix_users_email_normalized");
        builder.HasIndex(u => u.ReferralCode).IsUnique().HasFilter("\"ReferralCode\" IS NOT NULL").HasDatabaseName("ix_users_referral_code");
        builder.HasIndex(u => u.Status).HasDatabaseName("ix_users_status");
        builder.HasIndex(u => u.Country).HasDatabaseName("ix_users_country");
        builder.HasIndex(u => u.City).HasDatabaseName("ix_users_city");
        builder.HasIndex(u => u.IsDeleted).HasDatabaseName("ix_users_is_deleted");

        // Relationships
        builder.HasMany(u => u.UserRoles).WithOne(ur => ur.User).HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.RefreshTokens).WithOne(rt => rt.User).HasForeignKey(rt => rt.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.RunSessions).WithOne(rs => rs.User).HasForeignKey(rs => rs.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.SentFriendRequests).WithOne(f => f.Requester).HasForeignKey(f => f.RequesterId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.ReceivedFriendRequests).WithOne(f => f.Addressee).HasForeignKey(f => f.AddresseeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(u => u.Achievements).WithOne(ua => ua.User).HasForeignKey(ua => ua.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.Notifications).WithOne(n => n.User).HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.Referrals).WithOne(r => r.Referrer).HasForeignKey(r => r.ReferrerId).OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>EF Core Fluent API configuration for <see cref="Role"/>.</summary>
internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.Property(r => r.NormalizedName).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.HasIndex(r => r.NormalizedName).IsUnique().HasDatabaseName("ix_roles_name");
    }
}

/// <summary>EF Core Fluent API configuration for <see cref="Permission"/>.</summary>
internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Group).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.HasIndex(p => p.Name).IsUnique().HasDatabaseName("ix_permissions_name");
    }
}

/// <summary>EF Core Fluent API configuration for <see cref="RolePermission"/>.</summary>
internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });
        builder.HasOne(rp => rp.Role).WithMany(r => r.RolePermissions).HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions).HasForeignKey(rp => rp.PermissionId).OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>EF Core Fluent API configuration for <see cref="UserRole"/>.</summary>
internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });
        builder.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>EF Core Fluent API configuration for <see cref="RefreshToken"/>.</summary>
internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Token).HasMaxLength(512).IsRequired();
        builder.Property(rt => rt.CreatedByIp).HasMaxLength(50);
        builder.Property(rt => rt.DeviceInfo).HasMaxLength(500);
        builder.Property(rt => rt.RevokedReason).HasMaxLength(200);
        builder.Property(rt => rt.ReplacedByToken).HasMaxLength(512);
        builder.HasIndex(rt => rt.Token).HasDatabaseName("ix_refresh_tokens_token");
        builder.HasIndex(rt => rt.UserId).HasDatabaseName("ix_refresh_tokens_user_id");
    }
}
