using RunningCompetition.Domain.Common;
using RunningCompetition.Domain.Enums;

namespace RunningCompetition.Domain.Entities;

/// <summary>Represents an application user.</summary>
public class User : BaseEntity
{
    /// <summary>Gets or sets the first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets the last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address (unique).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the normalized email for lookups.</summary>
    public string EmailNormalized { get; set; } = string.Empty;

    /// <summary>Gets or sets the phone number.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Gets or sets the BCrypt hashed password.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's account status.</summary>
    public UserStatus Status { get; set; } = UserStatus.PendingVerification;

    /// <summary>Gets or sets whether the email has been verified.</summary>
    public bool IsEmailVerified { get; set; }

    /// <summary>Gets or sets the email verification token.</summary>
    public string? EmailVerificationToken { get; set; }

    /// <summary>Gets or sets the email verification token expiry.</summary>
    public DateTime? EmailVerificationTokenExpiry { get; set; }

    /// <summary>Gets or sets the password reset token.</summary>
    public string? PasswordResetToken { get; set; }

    /// <summary>Gets or sets the password reset token expiry.</summary>
    public DateTime? PasswordResetTokenExpiry { get; set; }

    /// <summary>Gets or sets the profile picture URL.</summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>Gets or sets the gender.</summary>
    public Gender Gender { get; set; } = Gender.NotSpecified;

    /// <summary>Gets or sets the date of birth.</summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Gets or sets the city.</summary>
    public string? City { get; set; }

    /// <summary>Gets or sets the state/province.</summary>
    public string? State { get; set; }

    /// <summary>Gets or sets the country code (ISO 3166-1 alpha-2).</summary>
    public string? Country { get; set; }

    /// <summary>Gets or sets height in centimeters.</summary>
    public decimal? HeightCm { get; set; }

    /// <summary>Gets or sets weight in kilograms.</summary>
    public decimal? WeightKg { get; set; }

    /// <summary>Gets or sets the user's primary goal type.</summary>
    public GoalType? GoalType { get; set; }

    /// <summary>Gets or sets the goal value (e.g., target distance in km).</summary>
    public decimal? GoalValue { get; set; }

    /// <summary>Gets or sets the profile privacy setting.</summary>
    public PrivacyLevel ProfilePrivacy { get; set; } = PrivacyLevel.Public;

    /// <summary>Gets or sets whether running activity is public.</summary>
    public PrivacyLevel ActivityPrivacy { get; set; } = PrivacyLevel.Public;

    /// <summary>Gets or sets the referral code for this user.</summary>
    public string? ReferralCode { get; set; }

    /// <summary>Gets or sets the ID of the user who referred this user.</summary>
    public Guid? ReferredById { get; set; }

    /// <summary>Gets or sets accumulated XP points.</summary>
    public int XpPoints { get; set; }

    /// <summary>Gets or sets the current level.</summary>
    public int Level { get; set; } = 1;

    /// <summary>Gets or sets the current running streak in days.</summary>
    public int CurrentStreak { get; set; }

    /// <summary>Gets or sets the all-time best streak in days.</summary>
    public int LongestStreak { get; set; }

    /// <summary>Gets or sets the total distance run in meters.</summary>
    public double TotalDistanceMeters { get; set; }

    /// <summary>Gets or sets the total number of completed runs.</summary>
    public int TotalRuns { get; set; }

    /// <summary>Gets or sets the total duration of all runs in seconds.</summary>
    public long TotalDurationSeconds { get; set; }

    /// <summary>Gets or sets the total calories burned.</summary>
    public double TotalCalories { get; set; }

    /// <summary>Gets or sets the date of the last run.</summary>
    public DateTime? LastRunAt { get; set; }

    /// <summary>Gets or sets the number of failed login attempts.</summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>Gets or sets the lockout end time.</summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>Gets or sets whether the user is locked out.</summary>
    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd > DateTime.UtcNow;

    /// <summary>Gets or sets whether push notifications are enabled.</summary>
    public bool PushNotificationsEnabled { get; set; } = true;

    /// <summary>Gets or sets the device push token.</summary>
    public string? PushToken { get; set; }

    /// <summary>Gets or sets whether this is a premium user.</summary>
    public bool IsPremium { get; set; }

    /// <summary>Gets or sets the premium expiry date.</summary>
    public DateTime? PremiumExpiresAt { get; set; }

    // Navigation properties
    /// <summary>Gets or sets the user's roles.</summary>
    public ICollection<UserRole> UserRoles { get; set; } = [];

    /// <summary>Gets or sets the user's refresh tokens.</summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    /// <summary>Gets or sets the user's run sessions.</summary>
    public ICollection<RunSession> RunSessions { get; set; } = [];

    /// <summary>Gets or sets friend requests sent by this user.</summary>
    public ICollection<Friendship> SentFriendRequests { get; set; } = [];

    /// <summary>Gets or sets friend requests received by this user.</summary>
    public ICollection<Friendship> ReceivedFriendRequests { get; set; } = [];

    /// <summary>Gets or sets the user's achievements.</summary>
    public ICollection<UserAchievement> Achievements { get; set; } = [];

    /// <summary>Gets or sets notifications for this user.</summary>
    public ICollection<Notification> Notifications { get; set; } = [];

    /// <summary>Gets or sets the user's referral history.</summary>
    public ICollection<Referral> Referrals { get; set; } = [];

    /// <summary>Gets the user's full name.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>Gets the user's age calculated from date of birth.</summary>
    public int? Age => DateOfBirth.HasValue
        ? (int)((DateTime.UtcNow - DateOfBirth.Value.ToDateTime(TimeOnly.MinValue)).TotalDays / 365.25)
        : null;
}
