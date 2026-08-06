using RunningCompetition.Domain.Common;
using RunningCompetition.Domain.Enums;

namespace RunningCompetition.Domain.Entities;

/// <summary>Represents a friendship or friend request between two users.</summary>
public class Friendship : BaseEntity
{
    /// <summary>Gets or sets the ID of the user who sent the request.</summary>
    public Guid RequesterId { get; set; }

    /// <summary>Gets or sets the ID of the user who received the request.</summary>
    public Guid AddresseeId { get; set; }

    /// <summary>Gets or sets the current status of the friendship.</summary>
    public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;

    /// <summary>Gets or sets when the request was accepted or rejected.</summary>
    public DateTime? RespondedAt { get; set; }

    // Navigation
    /// <summary>Gets or sets the requester user.</summary>
    public User Requester { get; set; } = null!;

    /// <summary>Gets or sets the addressee user.</summary>
    public User Addressee { get; set; } = null!;
}

/// <summary>Represents a referral relationship between two users.</summary>
public class Referral : BaseEntity
{
    /// <summary>Gets or sets the ID of the user who generated the referral.</summary>
    public Guid ReferrerId { get; set; }

    /// <summary>Gets or sets the ID of the user who was referred.</summary>
    public Guid ReferredUserId { get; set; }

    /// <summary>Gets or sets the referral code that was used.</summary>
    public string ReferralCode { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the reward has been granted.</summary>
    public bool RewardGranted { get; set; }

    /// <summary>Gets or sets when the reward was granted.</summary>
    public DateTime? RewardGrantedAt { get; set; }

    /// <summary>Gets or sets the XP points awarded.</summary>
    public int XpAwarded { get; set; }

    // Navigation
    /// <summary>Gets or sets the referring user.</summary>
    public User Referrer { get; set; } = null!;

    /// <summary>Gets or sets the referred user.</summary>
    public User ReferredUser { get; set; } = null!;
}

/// <summary>Represents a system-wide announcement.</summary>
public class Announcement : BaseEntity
{
    /// <summary>Gets or sets the announcement title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the announcement body.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the announcement is published.</summary>
    public bool IsPublished { get; set; }

    /// <summary>Gets or sets the publish date.</summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>Gets or sets the expiry date.</summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>Represents a user notification.</summary>
public class Notification : BaseEntity
{
    /// <summary>Gets or sets the recipient user ID.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the notification type.</summary>
    public NotificationType Type { get; set; }

    /// <summary>Gets or sets the notification title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the notification body.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets optional data payload (JSON).</summary>
    public string? Data { get; set; }

    /// <summary>Gets or sets whether the notification has been read.</summary>
    public bool IsRead { get; set; }

    /// <summary>Gets or sets when the notification was read.</summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>Gets or sets whether the push was delivered.</summary>
    public bool IsPushDelivered { get; set; }

    // Navigation
    /// <summary>Gets or sets the recipient user.</summary>
    public User User { get; set; } = null!;
}

/// <summary>Represents a system configuration setting.</summary>
public class SystemSetting : BaseEntity
{
    /// <summary>Gets or sets the setting key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the setting value (serialized).</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the data type.</summary>
    public SettingType Type { get; set; } = SettingType.String;

    /// <summary>Gets or sets the setting group.</summary>
    public string Group { get; set; } = "General";

    /// <summary>Gets or sets whether this setting is visible in the UI.</summary>
    public bool IsPublic { get; set; }
}

/// <summary>Represents an AI-related configuration setting.</summary>
public class AiSetting : BaseEntity
{
    /// <summary>Gets or sets the AI provider (e.g., OpenAI, Anthropic).</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the API key (stored encrypted).</summary>
    public string ApiKeyEncrypted { get; set; } = string.Empty;

    /// <summary>Gets or sets the model identifier.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Gets or sets the system prompt for the AI coach.</summary>
    public string? SystemPrompt { get; set; }

    /// <summary>Gets or sets the temperature parameter.</summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>Gets or sets whether AI features are enabled.</summary>
    public bool IsEnabled { get; set; }
}

/// <summary>Represents an audit log entry.</summary>
public class AuditLog : BaseEntity
{
    /// <summary>Gets or sets the user ID who performed the action.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Gets or sets the user's email at the time of the action.</summary>
    public string? UserEmail { get; set; }

    /// <summary>Gets or sets the action performed.</summary>
    public AuditAction Action { get; set; }

    /// <summary>Gets or sets the entity type affected.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity ID affected.</summary>
    public string? EntityId { get; set; }

    /// <summary>Gets or sets the old values (JSON).</summary>
    public string? OldValues { get; set; }

    /// <summary>Gets or sets the new values (JSON).</summary>
    public string? NewValues { get; set; }

    /// <summary>Gets or sets the client IP address.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Gets or sets the user agent string.</summary>
    public string? UserAgent { get; set; }

    /// <summary>Gets or sets additional metadata (JSON).</summary>
    public string? Metadata { get; set; }
}
