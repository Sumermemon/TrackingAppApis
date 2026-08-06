using AutoMapper;
using Microsoft.Extensions.Logging;
using RunningCompetition.Application.DTOs.Users;
using RunningCompetition.Contracts.Repositories;
using RunningCompetition.Contracts.Services;
using RunningCompetition.Shared.Common;
using RunningCompetition.Shared.Constants;
using RunningCompetition.Shared.Exceptions;

namespace RunningCompetition.Application.Services;

/// <summary>Handles user profile and management business logic.</summary>
public sealed class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ICacheService cacheService,
        ICurrentUserService currentUser, IMapper mapper, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _cacheService = cacheService;
        _currentUser = currentUser;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Gets the authenticated user's full profile (cached 10 min).</summary>
    public async Task<UserProfileDto> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var cacheKey = CacheKeys.UserProfile(userId);
        var cached = await _cacheService.GetAsync<UserProfileDto>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        var dto = _mapper.Map<UserProfileDto>(user);
        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10), cancellationToken);
        return dto;
    }

    /// <summary>Gets another user's profile, respecting privacy.</summary>
    public async Task<UserProfileDto> GetProfileAsync(Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(targetUserId, cancellationToken)
            ?? throw new NotFoundException("User", targetUserId);

        if (user.ProfilePrivacy == Domain.Enums.PrivacyLevel.Private && user.Id != _currentUser.UserId)
            throw new ForbiddenException("This profile is private.");

        return _mapper.Map<UserProfileDto>(user);
    }

    /// <summary>Updates the authenticated user's personal details.</summary>
    public async Task<UserProfileDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.Gender = request.Gender;
        user.DateOfBirth = request.DateOfBirth;
        user.City = request.City;
        user.State = request.State;
        user.Country = request.Country;
        user.HeightCm = request.HeightCm;
        user.WeightKg = request.WeightKg;
        user.GoalType = request.GoalType;
        user.GoalValue = request.GoalValue;
        user.SetUpdated(userId);

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync(CacheKeys.UserProfile(userId), cancellationToken);
        return _mapper.Map<UserProfileDto>(user);
    }

    /// <summary>Updates privacy settings.</summary>
    public async Task UpdatePrivacyAsync(UpdatePrivacyRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        user.ProfilePrivacy = request.ProfilePrivacy;
        user.ActivityPrivacy = request.ActivityPrivacy;
        user.SetUpdated(userId);
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync(CacheKeys.UserProfile(userId), cancellationToken);
    }

    /// <summary>Updates push notification settings.</summary>
    public async Task UpdatePushSettingsAsync(UpdatePushSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);
        user.PushNotificationsEnabled = request.PushNotificationsEnabled;
        user.PushToken = request.PushToken;
        user.SetUpdated(userId);
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Searches users by name/email.</summary>
    public async Task<PagedList<UserCardDto>> SearchUsersAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.SearchAsync(query, page, pageSize, cancellationToken);
        var dtos = users.Items.Select(u => _mapper.Map<UserCardDto>(u)).ToList();
        return new PagedList<UserCardDto>(dtos, users.TotalCount, page, pageSize);
    }

    /// <summary>Gets all users with pagination (admin only).</summary>
    public async Task<PagedList<UserCardDto>> GetAllUsersAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        PagedList<Domain.Entities.User> pagedUsers = string.IsNullOrEmpty(search)
            ? await PagedList<Domain.Entities.User>.CreateAsync(_userRepository.Query().OrderByDescending(u => u.CreatedAt), page, pageSize, cancellationToken)
            : await _userRepository.SearchAsync(search, page, pageSize, cancellationToken);

        var dtos = pagedUsers.Items.Select(u => _mapper.Map<UserCardDto>(u)).ToList();
        return new PagedList<UserCardDto>(dtos, pagedUsers.TotalCount, page, pageSize);
    }

    /// <summary>Updates user status (admin only).</summary>
    public async Task UpdateUserStatusAsync(Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);
        user.Status = request.Status;
        user.SetUpdated(_currentUser.UserId!.Value);
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync(CacheKeys.UserProfile(userId), cancellationToken);
    }

    /// <summary>Soft deletes a user (admin only).</summary>
    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _userRepository.SoftDeleteAsync(userId, _currentUser.UserId!.Value, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync(CacheKeys.UserProfile(userId), cancellationToken);
    }
}
