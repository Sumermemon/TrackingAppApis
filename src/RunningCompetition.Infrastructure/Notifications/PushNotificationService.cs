using Microsoft.Extensions.Logging;
using RunningCompetition.Contracts.Services;

namespace RunningCompetition.Infrastructure.Notifications;

/// <summary>
/// Firebase Cloud Messaging (FCM) push notification service.
/// Placeholder implementation — replace with FCM SDK or APNs as needed.
/// </summary>
public sealed class PushNotificationService : IPushNotificationService
{
    private readonly ILogger<PushNotificationService> _logger;

    /// <summary>Initializes a new instance of <see cref="PushNotificationService"/>.</summary>
    public PushNotificationService(ILogger<PushNotificationService> logger) => _logger = logger;

    /// <inheritdoc />
    public async Task SendAsync(string deviceToken, string title, string body, object? data = null, CancellationToken cancellationToken = default)
    {
        // TODO: Integrate FirebaseAdmin SDK or APNS for production push delivery
        _logger.LogInformation(
            "Push notification queued → Token: {Token}, Title: {Title}",
            deviceToken[..Math.Min(10, deviceToken.Length)] + "...", title);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SendBulkAsync(IEnumerable<string> deviceTokens, string title, string body, object? data = null, CancellationToken cancellationToken = default)
    {
        var tokens = deviceTokens.ToList();
        _logger.LogInformation("Bulk push notification to {Count} devices. Title: {Title}", tokens.Count, title);
        await Task.CompletedTask;
    }
}
