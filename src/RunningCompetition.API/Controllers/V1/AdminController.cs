using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunningCompetition.Application.DTOs;
using RunningCompetition.Application.DTOs.Users;
using RunningCompetition.Application.Services;
using RunningCompetition.Contracts.Services;
using RunningCompetition.Shared.Common;
using RunningCompetition.Shared.Constants;

namespace RunningCompetition.API.Controllers.V1;

/// <summary>Super admin dashboard: statistics, user management, roles, permissions, settings, announcements, audit logs, and AI settings.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Admin}")]
[Produces("application/json")]
public sealed class AdminController : ControllerBase
{
    private readonly AdminService _adminService;
    private readonly UserService _userService;
    private readonly IAiCoachService _aiCoachService;

    public AdminController(AdminService adminService, UserService userService, IAiCoachService aiCoachService)
    {
        _adminService = adminService;
        _userService = userService;
        _aiCoachService = aiCoachService;
    }

    // ── Dashboard ────────────────────────────────────────────────────────────

    /// <summary>Gets real-time dashboard statistics widgets (2-min cache).</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsDto>), 200)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(ApiResponse<DashboardStatsDto>.Ok(await _adminService.GetDashboardStatsAsync(cancellationToken)));

    // ── User Management ───────────────────────────────────────────────────────

    /// <summary>Gets paginated list of all users.</summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResponse<PagedList<UserCardDto>>), 200)]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(ApiResponse<PagedList<UserCardDto>>.Ok(await _userService.GetAllUsersAsync(search, page, pageSize, cancellationToken)));

    /// <summary>Updates a user's account status (active, suspended, etc.).</summary>
    [HttpPatch("users/{userId:guid}/status")]
    [Authorize(Roles = RoleNames.SuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> UpdateUserStatus(Guid userId, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        await _userService.UpdateUserStatusAsync(userId, request, cancellationToken);
        return Ok(ApiResponse.Ok("User status updated."));
    }

    /// <summary>Soft-deletes a user account.</summary>
    [HttpDelete("users/{userId:guid}")]
    [Authorize(Roles = RoleNames.SuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken)
    {
        await _userService.DeleteUserAsync(userId, cancellationToken);
        return Ok(ApiResponse.Ok("User deleted."));
    }

    // ── Role Management ───────────────────────────────────────────────────────

    /// <summary>Gets all roles with their assigned permissions.</summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoleDto>>), 200)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
        => Ok(ApiResponse<IReadOnlyList<RoleDto>>.Ok(await _adminService.GetRolesAsync(cancellationToken)));

    /// <summary>Creates a new role with assigned permissions.</summary>
    [HttpPost("roles")]
    [Authorize(Roles = RoleNames.SuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), 201)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await _adminService.CreateRoleAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<RoleDto>.Created(role));
    }

    /// <summary>Deletes a non-system role.</summary>
    [HttpDelete("roles/{roleId:guid}")]
    [Authorize(Roles = RoleNames.SuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> DeleteRole(Guid roleId, CancellationToken cancellationToken)
    {
        await _adminService.DeleteRoleAsync(roleId, cancellationToken);
        return Ok(ApiResponse.Ok("Role deleted."));
    }

    // ── Permission Management ─────────────────────────────────────────────────

    /// <summary>Gets all available permissions grouped by module.</summary>
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionDto>>), 200)]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
        => Ok(ApiResponse<IReadOnlyList<PermissionDto>>.Ok(await _adminService.GetPermissionsAsync(cancellationToken)));

    // ── System Settings ───────────────────────────────────────────────────────

    /// <summary>Gets all system configuration settings.</summary>
    [HttpGet("settings")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SystemSettingDto>>), 200)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
        => Ok(ApiResponse<IReadOnlyList<SystemSettingDto>>.Ok(await _adminService.GetSettingsAsync(cancellationToken)));

    /// <summary>Updates a system setting value by key.</summary>
    [HttpPatch("settings/{key}")]
    [Authorize(Roles = RoleNames.SuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingRequest request, CancellationToken cancellationToken)
    {
        await _adminService.UpdateSettingAsync(key, request, cancellationToken);
        return Ok(ApiResponse.Ok("Setting updated."));
    }

    // ── Announcements ─────────────────────────────────────────────────────────

    /// <summary>Gets paginated announcements.</summary>
    [HttpGet("announcements")]
    [ProducesResponseType(typeof(ApiResponse<PagedList<AnnouncementDto>>), 200)]
    public async Task<IActionResult> GetAnnouncements([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(ApiResponse<PagedList<AnnouncementDto>>.Ok(await _adminService.GetAnnouncementsAsync(page, pageSize, cancellationToken)));

    /// <summary>Creates a new announcement.</summary>
    [HttpPost("announcements")]
    [ProducesResponseType(typeof(ApiResponse<AnnouncementDto>), 201)]
    public async Task<IActionResult> CreateAnnouncement([FromBody] CreateAnnouncementRequest request, CancellationToken cancellationToken)
    {
        var announcement = await _adminService.CreateAnnouncementAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<AnnouncementDto>.Created(announcement));
    }

    /// <summary>Deletes an announcement by ID.</summary>
    [HttpDelete("announcements/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> DeleteAnnouncement(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.DeleteAnnouncementAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Announcement deleted."));
    }

    // ── Audit Logs ────────────────────────────────────────────────────────────

    /// <summary>Gets paginated audit logs with optional filters.</summary>
    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(ApiResponse<PagedList<AuditLogDto>>), 200)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] Guid? userId = null, [FromQuery] string? entityType = null,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
        => Ok(ApiResponse<PagedList<AuditLogDto>>.Ok(await _adminService.GetAuditLogsAsync(page, pageSize, userId, entityType, from, to, cancellationToken)));

    // ── AI Settings ───────────────────────────────────────────────────────────

    /// <summary>Gets AI coach settings.</summary>
    [HttpGet("ai-settings")]
    [Authorize(Roles = RoleNames.SuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<AiSettingDto>), 200)]
    public async Task<IActionResult> GetAiSettings(CancellationToken cancellationToken)
        => Ok(ApiResponse<AiSettingDto?>.Ok(await _adminService.GetAiSettingAsync(cancellationToken)));

    /// <summary>Creates or updates AI coach settings.</summary>
    [HttpPut("ai-settings")]
    [Authorize(Roles = RoleNames.SuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<AiSettingDto>), 200)]
    public async Task<IActionResult> UpsertAiSettings([FromBody] UpdateAiSettingRequest request, CancellationToken cancellationToken)
        => Ok(ApiResponse<AiSettingDto>.Ok(await _adminService.UpsertAiSettingAsync(request, cancellationToken)));
}

/// <summary>AI Coach: run analysis, training suggestions, and weekly reports.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai-coach")]
[Authorize]
[Produces("application/json")]
public sealed class AiCoachController : ControllerBase
{
    private readonly IAiCoachService _aiCoachService;
    private readonly ICurrentUserService _currentUser;

    public AiCoachController(IAiCoachService aiCoachService, ICurrentUserService currentUser)
    {
        _aiCoachService = aiCoachService;
        _currentUser = currentUser;
    }

    /// <summary>Analyzes a completed run session and returns AI-generated insights.</summary>
    [HttpGet("analyze/{runSessionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> AnalyzeRun(Guid runSessionId, CancellationToken cancellationToken)
    {
        var result = await _aiCoachService.AnalyzeRunAsync(runSessionId, _currentUser.UserId!.Value, cancellationToken);
        return Ok(ApiResponse<string>.Ok(result));
    }

    /// <summary>Returns AI-generated training suggestions based on recent activity.</summary>
    [HttpGet("suggest-training")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> SuggestTraining(CancellationToken cancellationToken)
    {
        var result = await _aiCoachService.SuggestTrainingAsync(_currentUser.UserId!.Value, cancellationToken);
        return Ok(ApiResponse<string>.Ok(result));
    }

    /// <summary>Returns an AI-generated weekly performance report.</summary>
    [HttpGet("weekly-report")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> WeeklyReport(CancellationToken cancellationToken)
    {
        var result = await _aiCoachService.GenerateWeeklyReportAsync(_currentUser.UserId!.Value, cancellationToken);
        return Ok(ApiResponse<string>.Ok(result));
    }
}
