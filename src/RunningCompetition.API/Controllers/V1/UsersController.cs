using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunningCompetition.Application.DTOs.Users;
using RunningCompetition.Application.Services;
using RunningCompetition.Shared.Common;

namespace RunningCompetition.API.Controllers.V1;

/// <summary>Manages user profiles, privacy settings, and push notification preferences.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly UserService _userService;

    /// <summary>Initializes a new instance of <see cref="UsersController"/>.</summary>
    public UsersController(UserService userService) => _userService = userService;

    /// <summary>Gets the authenticated user's full profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), 200)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var profile = await _userService.GetMyProfileAsync(cancellationToken);
        return Ok(ApiResponse<UserProfileDto>.Ok(profile));
    }

    /// <summary>Gets another user's profile by ID.</summary>
    /// <param name="userId">Target user ID.</param>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetProfile(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await _userService.GetProfileAsync(userId, cancellationToken);
        return Ok(ApiResponse<UserProfileDto>.Ok(profile));
    }

    /// <summary>Updates the authenticated user's personal details.</summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 422)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await _userService.UpdateProfileAsync(request, cancellationToken);
        return Ok(ApiResponse<UserProfileDto>.Ok(profile, "Profile updated successfully."));
    }

    /// <summary>Updates the authenticated user's privacy settings.</summary>
    [HttpPatch("me/privacy")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> UpdatePrivacy([FromBody] UpdatePrivacyRequest request, CancellationToken cancellationToken)
    {
        await _userService.UpdatePrivacyAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok("Privacy settings updated."));
    }

    /// <summary>Updates push notification token and preference.</summary>
    [HttpPatch("me/push-settings")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> UpdatePushSettings([FromBody] UpdatePushSettingsRequest request, CancellationToken cancellationToken)
    {
        await _userService.UpdatePushSettingsAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok("Push settings updated."));
    }

    /// <summary>Searches users by name or email.</summary>
    /// <param name="q">Search query.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<PagedList<UserCardDto>>), 200)]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _userService.SearchUsersAsync(q, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedList<UserCardDto>>.Ok(result));
    }
}
