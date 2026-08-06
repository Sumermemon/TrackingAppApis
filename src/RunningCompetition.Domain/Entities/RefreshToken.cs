using RunningCompetition.Domain.Common;

namespace RunningCompetition.Domain.Entities;

/// <summary>Represents a JWT refresh token.</summary>
public class RefreshToken : BaseEntity
{
    /// <summary>Gets or sets the user ID this token belongs to.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the token value (hashed).</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC expiry time.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Gets or sets the time the token was revoked.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Gets or sets the replacement token (for token rotation).</summary>
    public string? ReplacedByToken { get; set; }

    /// <summary>Gets or sets the reason for revocation.</summary>
    public string? RevokedReason { get; set; }

    /// <summary>Gets or sets the client IP address.</summary>
    public string? CreatedByIp { get; set; }

    /// <summary>Gets or sets the device info.</summary>
    public string? DeviceInfo { get; set; }

    /// <summary>Gets a value indicating whether the token is expired.</summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>Gets a value indicating whether the token is revoked.</summary>
    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>Gets a value indicating whether the token is active.</summary>
    public bool IsActive => !IsRevoked && !IsExpired;

    // Navigation
    /// <summary>Gets or sets the associated user.</summary>
    public User User { get; set; } = null!;
}
