namespace RunningCompetition.Contracts.Services;

/// <summary>Defines the email service contract.</summary>
public interface IEmailService
{
    /// <summary>Sends an email verification link to the user.</summary>
    Task SendEmailVerificationAsync(string toEmail, string toName, string token, CancellationToken cancellationToken = default);

    /// <summary>Sends a password reset email.</summary>
    Task SendPasswordResetAsync(string toEmail, string toName, string token, CancellationToken cancellationToken = default);

    /// <summary>Sends a welcome email after successful registration.</summary>
    Task SendWelcomeEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default);

    /// <summary>Sends a generic transactional email.</summary>
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

/// <summary>Defines the token service contract.</summary>
public interface ITokenService
{
    /// <summary>Generates a JWT access token for a user.</summary>
    Task<string> GenerateAccessTokenAsync(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions);

    /// <summary>Generates a secure refresh token string.</summary>
    string GenerateRefreshToken();

    /// <summary>Gets the user ID from an expired access token.</summary>
    Guid? GetUserIdFromExpiredToken(string token);
}

/// <summary>Defines the cache service contract.</summary>
public interface ICacheService
{
    /// <summary>Gets a cached value.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>Sets a value in the cache with an expiry.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>Removes a value from the cache.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Removes all cache keys matching a pattern.</summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a key exists in the cache.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>Defines the push notification service contract.</summary>
public interface IPushNotificationService
{
    /// <summary>Sends a push notification to a device token.</summary>
    Task SendAsync(string deviceToken, string title, string body, object? data = null, CancellationToken cancellationToken = default);

    /// <summary>Sends a push notification to multiple device tokens.</summary>
    Task SendBulkAsync(IEnumerable<string> deviceTokens, string title, string body, object? data = null, CancellationToken cancellationToken = default);
}

/// <summary>Defines the current user context service.</summary>
public interface ICurrentUserService
{
    /// <summary>Gets the authenticated user's ID.</summary>
    Guid? UserId { get; }

    /// <summary>Gets the authenticated user's email.</summary>
    string? Email { get; }

    /// <summary>Gets the authenticated user's roles.</summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>Gets the authenticated user's permissions.</summary>
    IReadOnlyList<string> Permissions { get; }

    /// <summary>Gets a value indicating whether the user is authenticated.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Gets a value indicating whether the user is a super admin.</summary>
    bool IsSuperAdmin { get; }

    /// <summary>Gets the client IP address.</summary>
    string? IpAddress { get; }

    /// <summary>Checks whether the current user has a given permission.</summary>
    bool HasPermission(string permission);
}

/// <summary>Defines the AI coach service contract.</summary>
public interface IAiCoachService
{
    /// <summary>Analyzes a run session and returns insights.</summary>
    Task<string> AnalyzeRunAsync(Guid runSessionId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Generates training suggestions based on user history.</summary>
    Task<string> SuggestTrainingAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Generates a weekly performance report.</summary>
    Task<string> GenerateWeeklyReportAsync(Guid userId, CancellationToken cancellationToken = default);
}
