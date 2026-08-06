using AutoMapper;
using Microsoft.Extensions.Logging;
using RunningCompetition.Application.DTOs;
using RunningCompetition.Contracts.Repositories;
using RunningCompetition.Contracts.Services;
using RunningCompetition.Domain.Entities;
using RunningCompetition.Domain.Enums;
using RunningCompetition.Infrastructure.Hubs;
using RunningCompetition.Shared.Exceptions;

namespace RunningCompetition.Application.Services;

/// <summary>Handles social / friend-request business logic.</summary>
public sealed class SocialService
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly RunHubService _hubService;
    private readonly IMapper _mapper;
    private readonly ILogger<SocialService> _logger;

    public SocialService(
        IFriendshipRepository friendshipRepository, IUserRepository userRepository,
        INotificationRepository notificationRepository, ICurrentUserService currentUser,
        RunHubService hubService, IMapper mapper, ILogger<SocialService> logger)
    {
        _friendshipRepository = friendshipRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
        _hubService = hubService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Sends a friend request to another user.</summary>
    public async Task SendFriendRequestAsync(Guid addresseeId, CancellationToken cancellationToken = default)
    {
        var requesterId = _currentUser.UserId!.Value;
        if (requesterId == addresseeId)
            throw new BusinessRuleException("You cannot send a friend request to yourself.");

        var addressee = await _userRepository.GetByIdAsync(addresseeId, cancellationToken)
            ?? throw new NotFoundException("User", addresseeId);

        var existing = await _friendshipRepository.GetFriendshipAsync(requesterId, addresseeId, cancellationToken);
        if (existing is not null)
        {
            if (existing.Status == FriendRequestStatus.Accepted)
                throw new ConflictException("You are already friends with this user.");
            if (existing.Status == FriendRequestStatus.Pending)
                throw new ConflictException("A friend request is already pending.");
        }

        var friendship = new Friendship
        {
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            Status = FriendRequestStatus.Pending,
            CreatedById = requesterId
        };
        await _friendshipRepository.AddAsync(friendship, cancellationToken);

        var requester = await _userRepository.GetByIdAsync(requesterId, cancellationToken);
        var notification = new Notification
        {
            UserId = addresseeId,
            Type = NotificationType.FriendRequest,
            Title = "New Friend Request",
            Body = $"{requester?.FullName ?? "Someone"} sent you a friend request.",
            CreatedById = requesterId
        };
        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _friendshipRepository.SaveChangesAsync(cancellationToken);

        await _hubService.SendNotificationAsync(addresseeId.ToString(), new { notification.Title, notification.Body });
    }

    /// <summary>Accepts a pending friend request.</summary>
    public async Task AcceptFriendRequestAsync(Guid friendshipId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var friendship = await _friendshipRepository.GetByIdAsync(friendshipId, cancellationToken)
            ?? throw new NotFoundException("FriendRequest", friendshipId);

        if (friendship.AddresseeId != userId) throw new ForbiddenException();
        if (friendship.Status != FriendRequestStatus.Pending)
            throw new BusinessRuleException("This request cannot be accepted.");

        friendship.Status = FriendRequestStatus.Accepted;
        friendship.RespondedAt = DateTime.UtcNow;
        friendship.SetUpdated(userId);
        _friendshipRepository.Update(friendship);

        var notification = new Notification
        {
            UserId = friendship.RequesterId,
            Type = NotificationType.FriendRequestAccepted,
            Title = "Friend Request Accepted",
            Body = $"Your friend request was accepted.",
            CreatedById = userId
        };
        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _friendshipRepository.SaveChangesAsync(cancellationToken);
        await _hubService.SendNotificationAsync(friendship.RequesterId.ToString(), new { notification.Title, notification.Body });
    }

    /// <summary>Rejects a pending friend request.</summary>
    public async Task RejectFriendRequestAsync(Guid friendshipId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var friendship = await _friendshipRepository.GetByIdAsync(friendshipId, cancellationToken)
            ?? throw new NotFoundException("FriendRequest", friendshipId);

        if (friendship.AddresseeId != userId) throw new ForbiddenException();
        friendship.Status = FriendRequestStatus.Rejected;
        friendship.RespondedAt = DateTime.UtcNow;
        friendship.SetUpdated(userId);
        _friendshipRepository.Update(friendship);
        await _friendshipRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Removes an existing friendship.</summary>
    public async Task RemoveFriendAsync(Guid friendUserId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var friendship = await _friendshipRepository.GetFriendshipAsync(userId, friendUserId, cancellationToken)
            ?? throw new NotFoundException("Friendship not found.");

        if (friendship.Status != FriendRequestStatus.Accepted)
            throw new BusinessRuleException("You are not friends with this user.");

        _friendshipRepository.Delete(friendship);
        await _friendshipRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Gets the authenticated user's friend list.</summary>
    public async Task<IReadOnlyList<FriendDto>> GetFriendsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var friends = await _friendshipRepository.GetFriendsAsync(userId, cancellationToken);
        return friends.Select(f => _mapper.Map<FriendDto>(f)).ToList().AsReadOnly();
    }

    /// <summary>Gets pending friend requests received by the current user.</summary>
    public async Task<IReadOnlyList<object>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var pending = await _friendshipRepository.GetPendingRequestsAsync(userId, cancellationToken);
        return pending.Select(f => (object)new
        {
            f.Id,
            RequesterId = f.RequesterId,
            RequesterName = f.Requester?.FullName,
            f.Requester?.ProfilePictureUrl,
            SentAt = f.CreatedAt
        }).ToList().AsReadOnly();
    }
}

/// <summary>Handles referral business logic.</summary>
public sealed class ReferralService
{
    private readonly IReferralRepository _referralRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public ReferralService(IReferralRepository referralRepository, IUserRepository userRepository,
        ICurrentUserService currentUser, IMapper mapper)
    {
        _referralRepository = referralRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    /// <summary>Gets the current user's referral statistics.</summary>
    public async Task<ReferralStatsDto> GetMyStatsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);
        var (total, rewarded, xp) = await _referralRepository.GetStatsAsync(userId, cancellationToken);
        return new ReferralStatsDto(total, rewarded, xp, user.ReferralCode ?? string.Empty);
    }

    /// <summary>Gets the current user's referral history.</summary>
    public async Task<IReadOnlyList<ReferralDto>> GetMyReferralsAsync(CancellationToken cancellationToken = default)
    {
        var referrals = await _referralRepository.GetByReferrerAsync(_currentUser.UserId!.Value, cancellationToken);
        return referrals.Select(r => _mapper.Map<ReferralDto>(r)).ToList().AsReadOnly();
    }
}

/// <summary>Handles achievement and leaderboard logic.</summary>
public sealed class AchievementService
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public AchievementService(IAchievementRepository achievementRepository,
        ICurrentUserService currentUser, IMapper mapper)
    {
        _achievementRepository = achievementRepository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    /// <summary>Gets the current user's earned achievements.</summary>
    public async Task<IReadOnlyList<UserAchievementDto>> GetMyAchievementsAsync(CancellationToken cancellationToken = default)
    {
        var earned = await _achievementRepository.GetUserAchievementsAsync(_currentUser.UserId!.Value, cancellationToken);
        return earned.Select(ua => _mapper.Map<UserAchievementDto>(ua)).ToList().AsReadOnly();
    }

    /// <summary>Gets all achievement definitions.</summary>
    public async Task<IReadOnlyList<AchievementDto>> GetAllAchievementsAsync(CancellationToken cancellationToken = default)
    {
        var all = await _achievementRepository.GetAllAsync(cancellationToken);
        return all.Select(a => _mapper.Map<AchievementDto>(a)).ToList().AsReadOnly();
    }
}

/// <summary>Handles notification logic.</summary>
public sealed class NotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public NotificationService(INotificationRepository notificationRepository,
        ICurrentUserService currentUser, IMapper mapper)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    /// <summary>Gets paginated notifications for the current user.</summary>
    public async Task<Shared.Common.PagedList<NotificationDto>> GetMyNotificationsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var notifications = await _notificationRepository.GetByUserAsync(userId, page, pageSize, cancellationToken);
        var dtos = notifications.Items.Select(n => _mapper.Map<NotificationDto>(n)).ToList();
        return new Shared.Common.PagedList<NotificationDto>(dtos, notifications.TotalCount, page, pageSize);
    }

    /// <summary>Gets unread notification count.</summary>
    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
        => await _notificationRepository.GetUnreadCountAsync(_currentUser.UserId!.Value, cancellationToken);

    /// <summary>Marks all notifications as read.</summary>
    public async Task MarkAllReadAsync(CancellationToken cancellationToken = default)
        => await _notificationRepository.MarkAllAsReadAsync(_currentUser.UserId!.Value, cancellationToken);

    /// <summary>Marks a single notification as read.</summary>
    public async Task MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken)
            ?? throw new NotFoundException("Notification", notificationId);
        if (notification.UserId != _currentUser.UserId) throw new ForbiddenException();
        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        _notificationRepository.Update(notification);
        await _notificationRepository.SaveChangesAsync(cancellationToken);
    }
}
