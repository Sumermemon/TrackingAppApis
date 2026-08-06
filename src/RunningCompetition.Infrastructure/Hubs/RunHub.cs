using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace RunningCompetition.Infrastructure.Hubs;

/// <summary>
/// SignalR hub for real-time run tracking updates and live leaderboard pushes.
/// </summary>
public sealed class RunHub : Hub
{
    private readonly ILogger<RunHub> _logger;

    /// <summary>Initializes a new instance of <see cref="RunHub"/>.</summary>
    public RunHub(ILogger<RunHub> logger) => _logger = logger;

    /// <summary>Called when a client connects.</summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("RunHub: User {UserId} connected (Connection: {ConnId})", userId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>Called when a client disconnects.</summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("RunHub: User {UserId} disconnected", userId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Allows a client to join a run group for live updates.</summary>
    public async Task JoinRunGroup(string runSessionId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"run:{runSessionId}");

    /// <summary>Allows a client to leave a run group.</summary>
    public async Task LeaveRunGroup(string runSessionId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"run:{runSessionId}");

    /// <summary>Allows a client to subscribe to a leaderboard group.</summary>
    public async Task JoinLeaderboard(string period, string scope)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"leaderboard:{period}:{scope}");

    /// <summary>Allows a client to leave a leaderboard group.</summary>
    public async Task LeaveLeaderboard(string period, string scope)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"leaderboard:{period}:{scope}");
}

/// <summary>
/// Service for pushing SignalR events to connected clients.
/// </summary>
public sealed class RunHubService
{
    private readonly IHubContext<RunHub> _hubContext;

    /// <summary>Initializes a new instance of <see cref="RunHubService"/>.</summary>
    public RunHubService(IHubContext<RunHub> hubContext) => _hubContext = hubContext;

    /// <summary>Broadcasts a GPS location update to all clients in the run group.</summary>
    public async Task SendGpsUpdateAsync(string runSessionId, double latitude, double longitude, double? speed)
        => await _hubContext.Clients.Group($"run:{runSessionId}")
            .SendAsync("GpsUpdate", new { runSessionId, latitude, longitude, speed, timestamp = DateTime.UtcNow });

    /// <summary>Broadcasts run completion to the run group.</summary>
    public async Task SendRunCompletedAsync(string runSessionId, object summary)
        => await _hubContext.Clients.Group($"run:{runSessionId}")
            .SendAsync("RunCompleted", summary);

    /// <summary>Broadcasts a leaderboard update to all leaderboard subscribers.</summary>
    public async Task SendLeaderboardUpdateAsync(string period, string scope, object leaderboard)
        => await _hubContext.Clients.Group($"leaderboard:{period}:{scope}")
            .SendAsync("LeaderboardUpdate", leaderboard);

    /// <summary>Sends a notification to a specific user.</summary>
    public async Task SendNotificationAsync(string userId, object notification)
        => await _hubContext.Clients.User(userId).SendAsync("Notification", notification);

    /// <summary>Sends an achievement unlocked event to a user.</summary>
    public async Task SendAchievementUnlockedAsync(string userId, object achievement)
        => await _hubContext.Clients.User(userId).SendAsync("AchievementUnlocked", achievement);
}
