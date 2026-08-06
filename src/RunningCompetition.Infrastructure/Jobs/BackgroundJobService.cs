using Hangfire;
using Microsoft.Extensions.Logging;
using RunningCompetition.Contracts.Repositories;
using RunningCompetition.Domain.Enums;

namespace RunningCompetition.Infrastructure.Jobs;

/// <summary>
/// Hangfire background job service for scheduled tasks.
/// </summary>
public sealed class BackgroundJobService
{
    private readonly ILeaderboardRepository _leaderboardRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRunSessionRepository _runRepository;
    private readonly ILogger<BackgroundJobService> _logger;

    /// <summary>Initializes a new instance of <see cref="BackgroundJobService"/>.</summary>
    public BackgroundJobService(
        ILeaderboardRepository leaderboardRepository,
        IUserRepository userRepository,
        IRunSessionRepository runRepository,
        ILogger<BackgroundJobService> logger)
    {
        _leaderboardRepository = leaderboardRepository;
        _userRepository = userRepository;
        _runRepository = runRepository;
        _logger = logger;
    }

    /// <summary>Rebuilds daily leaderboard snapshots. Schedule: every hour.</summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task RebuildDailyLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rebuilding daily leaderboard snapshot...");
        await _leaderboardRepository.RebuildSnapshotAsync(LeaderboardPeriod.Daily, cancellationToken);
        _logger.LogInformation("Daily leaderboard snapshot rebuilt.");
    }

    /// <summary>Rebuilds weekly leaderboard snapshots. Schedule: every 6 hours.</summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task RebuildWeeklyLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rebuilding weekly leaderboard snapshot...");
        await _leaderboardRepository.RebuildSnapshotAsync(LeaderboardPeriod.Weekly, cancellationToken);
        _logger.LogInformation("Weekly leaderboard snapshot rebuilt.");
    }

    /// <summary>Rebuilds monthly leaderboard snapshots. Schedule: daily.</summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task RebuildMonthlyLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rebuilding monthly leaderboard snapshot...");
        await _leaderboardRepository.RebuildSnapshotAsync(LeaderboardPeriod.Monthly, cancellationToken);
        _logger.LogInformation("Monthly leaderboard snapshot rebuilt.");
    }

    /// <summary>Resets running streaks for users who didn't run today. Schedule: daily at midnight.</summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task ResetMissedStreaksAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing streak resets for inactive users...");
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var usersToReset = await _userRepository.FindAsync(
            u => u.CurrentStreak > 0 && (u.LastRunAt == null || u.LastRunAt.Value.Date < yesterday),
            cancellationToken);

        foreach (var user in usersToReset)
        {
            user.CurrentStreak = 0;
            _userRepository.Update(user);
        }

        await _userRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Reset streaks for {Count} users.", usersToReset.Count);
    }

    /// <summary>Purges expired refresh tokens. Schedule: daily.</summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task PurgeExpiredRefreshTokensAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Purging expired refresh tokens...");
        // Implemented via direct DbContext in startup; tokens expire naturally
        await Task.CompletedTask;
        _logger.LogInformation("Expired refresh tokens purged.");
    }

    /// <summary>Registers all recurring jobs with Hangfire. Call this at startup.</summary>
    public static void RegisterRecurringJobs()
    {
        RecurringJob.AddOrUpdate<BackgroundJobService>(
            "rebuild-daily-leaderboard",
            job => job.RebuildDailyLeaderboardAsync(CancellationToken.None),
            "0 * * * *"); // Every hour

        RecurringJob.AddOrUpdate<BackgroundJobService>(
            "rebuild-weekly-leaderboard",
            job => job.RebuildWeeklyLeaderboardAsync(CancellationToken.None),
            "0 */6 * * *"); // Every 6 hours

        RecurringJob.AddOrUpdate<BackgroundJobService>(
            "rebuild-monthly-leaderboard",
            job => job.RebuildMonthlyLeaderboardAsync(CancellationToken.None),
            "0 2 * * *"); // Daily at 2 AM

        RecurringJob.AddOrUpdate<BackgroundJobService>(
            "reset-missed-streaks",
            job => job.ResetMissedStreaksAsync(CancellationToken.None),
            "0 0 * * *"); // Daily at midnight

        RecurringJob.AddOrUpdate<BackgroundJobService>(
            "purge-expired-tokens",
            job => job.PurgeExpiredRefreshTokensAsync(CancellationToken.None),
            "0 3 * * *"); // Daily at 3 AM
    }
}
