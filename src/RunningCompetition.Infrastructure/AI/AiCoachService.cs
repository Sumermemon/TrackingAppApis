using Microsoft.Extensions.Logging;
using RunningCompetition.Contracts.Services;

namespace RunningCompetition.Infrastructure.AI;

/// <summary>
/// AI Coach service placeholder — provides structured interfaces for
/// future AI provider integration (OpenAI, Anthropic, etc.).
/// </summary>
public sealed class AiCoachService : IAiCoachService
{
    private readonly ILogger<AiCoachService> _logger;

    /// <summary>Initializes a new instance of <see cref="AiCoachService"/>.</summary>
    public AiCoachService(ILogger<AiCoachService> logger) => _logger = logger;

    /// <inheritdoc />
    public async Task<string> AnalyzeRunAsync(Guid runSessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI Coach: Analyzing run {RunId} for user {UserId}", runSessionId, userId);
        // TODO: Fetch run data, build prompt, call AI API
        await Task.CompletedTask;
        return "Great effort! Your pace was consistent throughout the run. Consider increasing your cadence by 5% next time for better efficiency.";
    }

    /// <inheritdoc />
    public async Task<string> SuggestTrainingAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI Coach: Generating training suggestions for user {UserId}", userId);
        // TODO: Fetch user history, build prompt, call AI API
        await Task.CompletedTask;
        return "Based on your recent activity, we recommend 3 easy runs (30 min each) and 1 tempo run this week.";
    }

    /// <inheritdoc />
    public async Task<string> GenerateWeeklyReportAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI Coach: Generating weekly report for user {UserId}", userId);
        // TODO: Aggregate weekly stats, build prompt, call AI API
        await Task.CompletedTask;
        return "This week you ran 3 times covering 18.5 km. Your average pace improved by 8 seconds/km vs last week. Keep it up!";
    }
}
