using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunningCompetition.Application.DTOs;
using RunningCompetition.Application.Services;
using RunningCompetition.Domain.Enums;
using RunningCompetition.Shared.Common;

namespace RunningCompetition.API.Controllers.V1;

/// <summary>Provides leaderboard data across multiple time periods and scopes.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/leaderboards")]
[Authorize]
[Produces("application/json")]
public sealed class LeaderboardsController : ControllerBase
{
    private readonly LeaderboardService _leaderboardService;

    public LeaderboardsController(LeaderboardService leaderboardService)
        => _leaderboardService = leaderboardService;

    /// <summary>Gets the global leaderboard for a given period.</summary>
    /// <param name="period">daily | weekly | monthly | yearly | alltime</param>
    /// <param name="scope">global | city | state | country | friends</param>
    /// <param name="scopeValue">Value for city/state/country scope (e.g., "NYC", "NY", "US").</param>
    /// <param name="top">Number of entries to return (max 100).</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaderboardEntryDto>>), 200)]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] LeaderboardPeriod period = LeaderboardPeriod.Weekly,
        [FromQuery] LeaderboardScope scope = LeaderboardScope.Global,
        [FromQuery] string? scopeValue = null,
        [FromQuery] int top = 100,
        CancellationToken cancellationToken = default)
    {
        var entries = await _leaderboardService.GetLeaderboardAsync(period, scope, scopeValue, top, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<LeaderboardEntryDto>>.Ok(entries));
    }

    /// <summary>Gets the current user's rank for a given period and scope.</summary>
    [HttpGet("my-rank")]
    [ProducesResponseType(typeof(ApiResponse<int?>), 200)]
    public async Task<IActionResult> GetMyRank(
        [FromQuery] LeaderboardPeriod period = LeaderboardPeriod.Weekly,
        [FromQuery] LeaderboardScope scope = LeaderboardScope.Global,
        [FromQuery] string? scopeValue = null,
        CancellationToken cancellationToken = default)
    {
        var rank = await _leaderboardService.GetMyRankAsync(period, scope, scopeValue, cancellationToken);
        return Ok(ApiResponse<int?>.Ok(rank));
    }

    /// <summary>Gets the friends leaderboard for the current user.</summary>
    [HttpGet("friends")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaderboardEntryDto>>), 200)]
    public async Task<IActionResult> GetFriendsLeaderboard(
        [FromQuery] LeaderboardPeriod period = LeaderboardPeriod.Weekly,
        CancellationToken cancellationToken = default)
    {
        var entries = await _leaderboardService.GetFriendsLeaderboardAsync(period, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<LeaderboardEntryDto>>.Ok(entries));
    }
}

/// <summary>Manages friend requests and the user's friend list.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/social")]
[Authorize]
[Produces("application/json")]
public sealed class SocialController : ControllerBase
{
    private readonly SocialService _socialService;

    public SocialController(SocialService socialService) => _socialService = socialService;

    /// <summary>Sends a friend request to another user.</summary>
    [HttpPost("friend-request/{addresseeId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> SendFriendRequest(Guid addresseeId, CancellationToken cancellationToken)
    {
        await _socialService.SendFriendRequestAsync(addresseeId, cancellationToken);
        return Ok(ApiResponse.Ok("Friend request sent."));
    }

    /// <summary>Accepts a pending friend request.</summary>
    [HttpPost("friend-request/{friendshipId:guid}/accept")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> AcceptFriendRequest(Guid friendshipId, CancellationToken cancellationToken)
    {
        await _socialService.AcceptFriendRequestAsync(friendshipId, cancellationToken);
        return Ok(ApiResponse.Ok("Friend request accepted."));
    }

    /// <summary>Rejects a pending friend request.</summary>
    [HttpPost("friend-request/{friendshipId:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> RejectFriendRequest(Guid friendshipId, CancellationToken cancellationToken)
    {
        await _socialService.RejectFriendRequestAsync(friendshipId, cancellationToken);
        return Ok(ApiResponse.Ok("Friend request rejected."));
    }

    /// <summary>Removes an existing friend.</summary>
    [HttpDelete("friends/{friendUserId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> RemoveFriend(Guid friendUserId, CancellationToken cancellationToken)
    {
        await _socialService.RemoveFriendAsync(friendUserId, cancellationToken);
        return Ok(ApiResponse.Ok("Friend removed."));
    }

    /// <summary>Gets the authenticated user's friend list.</summary>
    [HttpGet("friends")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<FriendDto>>), 200)]
    public async Task<IActionResult> GetFriends(CancellationToken cancellationToken)
        => Ok(ApiResponse<IReadOnlyList<FriendDto>>.Ok(await _socialService.GetFriendsAsync(cancellationToken)));

    /// <summary>Gets pending friend requests received by the current user.</summary>
    [HttpGet("friend-requests/pending")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<object>>), 200)]
    public async Task<IActionResult> GetPendingRequests(CancellationToken cancellationToken)
        => Ok(ApiResponse<IReadOnlyList<object>>.Ok(await _socialService.GetPendingRequestsAsync(cancellationToken)));
}

/// <summary>Provides referral code management and referral history.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/referrals")]
[Authorize]
[Produces("application/json")]
public sealed class ReferralsController : ControllerBase
{
    private readonly ReferralService _referralService;

    public ReferralsController(ReferralService referralService) => _referralService = referralService;

    /// <summary>Gets the current user's referral statistics and code.</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<ReferralStatsDto>), 200)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
        => Ok(ApiResponse<ReferralStatsDto>.Ok(await _referralService.GetMyStatsAsync(cancellationToken)));

    /// <summary>Gets the current user's referral history.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ReferralDto>>), 200)]
    public async Task<IActionResult> GetReferrals(CancellationToken cancellationToken)
        => Ok(ApiResponse<IReadOnlyList<ReferralDto>>.Ok(await _referralService.GetMyReferralsAsync(cancellationToken)));
}

/// <summary>Provides achievement definitions and user earned badges.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/achievements")]
[Authorize]
[Produces("application/json")]
public sealed class AchievementsController : ControllerBase
{
    private readonly AchievementService _achievementService;

    public AchievementsController(AchievementService achievementService) => _achievementService = achievementService;

    /// <summary>Gets all achievement definitions.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AchievementDto>>), 200)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok(ApiResponse<IReadOnlyList<AchievementDto>>.Ok(await _achievementService.GetAllAchievementsAsync(cancellationToken)));

    /// <summary>Gets the current user's earned achievements.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserAchievementDto>>), 200)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
        => Ok(ApiResponse<IReadOnlyList<UserAchievementDto>>.Ok(await _achievementService.GetMyAchievementsAsync(cancellationToken)));
}

/// <summary>Manages user notifications.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
[Authorize]
[Produces("application/json")]
public sealed class NotificationsController : ControllerBase
{
    private readonly NotificationService _notificationService;

    public NotificationsController(NotificationService notificationService) => _notificationService = notificationService;

    /// <summary>Gets paginated notifications for the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedList<NotificationDto>>), 200)]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(ApiResponse<PagedList<NotificationDto>>.Ok(await _notificationService.GetMyNotificationsAsync(page, pageSize, cancellationToken)));

    /// <summary>Gets the unread notification count.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<int>), 200)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
        => Ok(ApiResponse<int>.Ok(await _notificationService.GetUnreadCountAsync(cancellationToken)));

    /// <summary>Marks all notifications as read.</summary>
    [HttpPost("mark-all-read")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllReadAsync(cancellationToken);
        return Ok(ApiResponse.Ok("All notifications marked as read."));
    }

    /// <summary>Marks a single notification as read.</summary>
    [HttpPost("{notificationId:guid}/read")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        await _notificationService.MarkReadAsync(notificationId, cancellationToken);
        return Ok(ApiResponse.Ok("Notification marked as read."));
    }
}
