namespace RunningCompetition.Shared.Settings;

/// <summary>JWT configuration settings from appsettings.json.</summary>
public sealed class JwtSettings
{
    /// <summary>Gets or sets the secret signing key.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Gets or sets the token issuer.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Gets or sets the token audience.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Gets or sets the access token expiry in minutes.</summary>
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    /// <summary>Gets or sets the refresh token expiry in days.</summary>
    public int RefreshTokenExpiryDays { get; set; } = 7;
}

/// <summary>Email SMTP settings.</summary>
public sealed class EmailSettings
{
    /// <summary>Gets or sets the SMTP host.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Gets or sets the SMTP port.</summary>
    public int Port { get; set; } = 587;

    /// <summary>Gets or sets the sender email address.</summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>Gets or sets the sender display name.</summary>
    public string FromName { get; set; } = "Running Competition";

    /// <summary>Gets or sets the SMTP username.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Gets or sets the SMTP password.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to use SSL.</summary>
    public bool UseSsl { get; set; } = true;
}

/// <summary>Redis settings.</summary>
public sealed class RedisSettings
{
    /// <summary>Gets or sets the Redis connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets the instance name prefix.</summary>
    public string InstanceName { get; set; } = "RunningApp:";
}

/// <summary>Hangfire settings.</summary>
public sealed class HangfireSettings
{
    /// <summary>Gets or sets the worker count.</summary>
    public int WorkerCount { get; set; } = 5;

    /// <summary>Gets or sets the dashboard path.</summary>
    public string DashboardPath { get; set; } = "/hangfire";
}

/// <summary>Pagination defaults.</summary>
public sealed class PaginationSettings
{
    /// <summary>Gets or sets the default page size.</summary>
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>Gets or sets the maximum page size.</summary>
    public int MaxPageSize { get; set; } = 100;
}

/// <summary>Rate limiting settings.</summary>
public sealed class RateLimitSettings
{
    /// <summary>Gets or sets the requests per window.</summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>Gets or sets the window in seconds.</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>Gets or sets the queue limit.</summary>
    public int QueueLimit { get; set; } = 10;
}
