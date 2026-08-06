using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RunningCompetition.Application.DTOs;
using RunningCompetition.Contracts.Repositories;
using RunningCompetition.Contracts.Services;
using RunningCompetition.Domain.Entities;
using RunningCompetition.Domain.Enums;
using RunningCompetition.Persistence.Context;
using RunningCompetition.Shared.Common;
using RunningCompetition.Shared.Constants;
using RunningCompetition.Shared.Exceptions;

namespace RunningCompetition.Application.Services;

/// <summary>Handles leaderboard retrieval with Redis caching.</summary>
public sealed class LeaderboardService
{
    private readonly ILeaderboardRepository _leaderboardRepository;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public LeaderboardService(ILeaderboardRepository leaderboardRepository,
        IFriendshipRepository friendshipRepository, ICacheService cacheService,
        ICurrentUserService currentUser, IMapper mapper)
    {
        _leaderboardRepository = leaderboardRepository;
        _friendshipRepository = friendshipRepository;
        _cacheService = cacheService;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    /// <summary>Gets the leaderboard for a given period and scope, served from Redis cache when available.</summary>
    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetLeaderboardAsync(
        LeaderboardPeriod period, LeaderboardScope scope, string? scopeValue = null,
        int top = 100, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Leaderboard(period.ToString(), $"{scope}:{scopeValue ?? "global"}");
        var cached = await _cacheService.GetAsync<List<LeaderboardEntryDto>>(cacheKey, cancellationToken);
        if (cached is not null) return cached.AsReadOnly();

        var entries = await _leaderboardRepository.GetLeaderboardAsync(period, scope, scopeValue, top, cancellationToken);
        var dtos = entries.Select(e => _mapper.Map<LeaderboardEntryDto>(e)).ToList();

        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(AppConstants.LeaderboardCacheTtlMinutes), cancellationToken);
        return dtos.AsReadOnly();
    }

    /// <summary>Gets the current user's rank for a given period and scope.</summary>
    public async Task<int?> GetMyRankAsync(LeaderboardPeriod period, LeaderboardScope scope,
        string? scopeValue = null, CancellationToken cancellationToken = default)
        => await _leaderboardRepository.GetUserRankAsync(_currentUser.UserId!.Value, period, scope, scopeValue, cancellationToken);

    /// <summary>Gets the friends leaderboard for the current user.</summary>
    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetFriendsLeaderboardAsync(
        LeaderboardPeriod period, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var friends = await _friendshipRepository.GetFriendsAsync(userId, cancellationToken);
        var friendIds = friends.Select(f => f.Id).Concat([userId]).ToHashSet();

        var all = await _leaderboardRepository.GetLeaderboardAsync(period, LeaderboardScope.Global, null, int.MaxValue, cancellationToken);
        return all.Where(e => friendIds.Contains(e.UserId))
                  .Select(e => _mapper.Map<LeaderboardEntryDto>(e))
                  .ToList().AsReadOnly();
    }
}

/// <summary>Handles admin panel operations: dashboard, roles, permissions, settings, announcements, audit logs.</summary>
public sealed class AdminService
{
    private readonly IUserRepository _userRepository;
    private readonly IRunSessionRepository _runRepository;
    private readonly ILeaderboardRepository _leaderboardRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUserService _currentUser;
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<AdminService> _logger;

    public AdminService(IUserRepository userRepository, IRunSessionRepository runRepository,
        ILeaderboardRepository leaderboardRepository, IAuditLogRepository auditLogRepository,
        ICacheService cacheService, ICurrentUserService currentUser,
        AppDbContext context, IMapper mapper, ILogger<AdminService> logger)
    {
        _userRepository = userRepository;
        _runRepository = runRepository;
        _leaderboardRepository = leaderboardRepository;
        _auditLogRepository = auditLogRepository;
        _cacheService = cacheService;
        _currentUser = currentUser;
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Gets dashboard statistics with a 2-minute cache.</summary>
    public async Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.DashboardStats();
        var cached = await _cacheService.GetAsync<DashboardStatsDto>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var (total, active, premium) = await _userRepository.GetUserStatsAsync(cancellationToken);
        var activeRuns = await _runRepository.GetActiveRunsCountAsync(cancellationToken);
        var todayDist = await _runRepository.GetTodayTotalDistanceAsync(cancellationToken);
        var todayRuns = await _context.RunSessions.CountAsync(rs =>
            rs.StartedAt != null && rs.StartedAt.Value.Date == DateTime.UtcNow.Date
            && rs.Status == RunStatus.Completed, cancellationToken);

        var topEntries = await _leaderboardRepository.GetLeaderboardAsync(LeaderboardPeriod.AllTime, LeaderboardScope.Global, null, 10, cancellationToken);
        var topRunners = topEntries.Select(e => _mapper.Map<LeaderboardEntryDto>(e)).ToList().AsReadOnly();

        var stats = new DashboardStatsDto(total, active, activeRuns, Math.Round(todayDist / 1000.0, 2), todayRuns, premium, topRunners);
        await _cacheService.SetAsync(cacheKey, stats, TimeSpan.FromMinutes(2), cancellationToken);
        return stats;
    }

    // ── Roles ──────────────────────────────────────────────────────────────

    /// <summary>Gets all roles with permission details.</summary>
    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _context.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return roles.Select(r => _mapper.Map<RoleDto>(r)).ToList().AsReadOnly();
    }

    /// <summary>Creates a new role with assigned permissions.</summary>
    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Roles.AnyAsync(r => r.NormalizedName == request.Name.ToUpperInvariant(), cancellationToken);
        if (exists) throw new ConflictException($"Role '{request.Name}' already exists.");

        var permissions = await _context.Permissions
            .Where(p => request.PermissionIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var role = new Role
        {
            Name = request.Name,
            NormalizedName = request.Name.ToUpperInvariant(),
            Description = request.Description,
            CreatedById = _currentUser.UserId
        };
        role.RolePermissions = permissions.Select(p => new RolePermission { Role = role, Permission = p }).ToList();

        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);
        return _mapper.Map<RoleDto>(role);
    }

    /// <summary>Deletes a non-system role.</summary>
    public async Task DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _context.Roles.FindAsync([roleId], cancellationToken)
            ?? throw new NotFoundException("Role", roleId);
        if (role.IsSystem) throw new BusinessRuleException("System roles cannot be deleted.");
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Gets all permissions grouped by module.</summary>
    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _context.Permissions.AsNoTracking().OrderBy(p => p.Group).ThenBy(p => p.Name).ToListAsync(cancellationToken);
        return permissions.Select(p => _mapper.Map<PermissionDto>(p)).ToList().AsReadOnly();
    }

    // ── System Settings ──────────────────────────────────────────────────

    /// <summary>Gets all system settings.</summary>
    public async Task<IReadOnlyList<SystemSettingDto>> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _context.SystemSettings.AsNoTracking().OrderBy(s => s.Group).ThenBy(s => s.Key).ToListAsync(cancellationToken);
        return settings.Select(s => _mapper.Map<SystemSettingDto>(s)).ToList().AsReadOnly();
    }

    /// <summary>Updates a system setting value.</summary>
    public async Task UpdateSettingAsync(string key, UpdateSettingRequest request, CancellationToken cancellationToken = default)
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken)
            ?? throw new NotFoundException($"Setting '{key}' not found.");
        setting.Value = request.Value;
        setting.SetUpdated(_currentUser.UserId!.Value);
        await _context.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync(CacheKeys.SystemSettings(), cancellationToken);
    }

    // ── Announcements ────────────────────────────────────────────────────

    /// <summary>Gets paginated announcements.</summary>
    public async Task<PagedList<AnnouncementDto>> GetAnnouncementsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var q = _context.Announcements.AsNoTracking().OrderByDescending(a => a.CreatedAt);
        var paged = await PagedList<Announcement>.CreateAsync(q, page, pageSize, cancellationToken);
        var dtos = paged.Items.Select(a => _mapper.Map<AnnouncementDto>(a)).ToList();
        return new PagedList<AnnouncementDto>(dtos, paged.TotalCount, page, pageSize);
    }

    /// <summary>Creates a new announcement.</summary>
    public async Task<AnnouncementDto> CreateAnnouncementAsync(CreateAnnouncementRequest request, CancellationToken cancellationToken = default)
    {
        var announcement = new Announcement
        {
            Title = request.Title,
            Body = request.Body,
            IsPublished = request.Publish,
            PublishedAt = request.Publish ? DateTime.UtcNow : null,
            ExpiresAt = request.ExpiresAt,
            CreatedById = _currentUser.UserId
        };
        _context.Announcements.Add(announcement);
        await _context.SaveChangesAsync(cancellationToken);
        return _mapper.Map<AnnouncementDto>(announcement);
    }

    /// <summary>Deletes an announcement.</summary>
    public async Task DeleteAnnouncementAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var announcement = await _context.Announcements.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Announcement", id);
        announcement.SoftDelete(_currentUser.UserId!.Value);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // ── Audit Logs ───────────────────────────────────────────────────────

    /// <summary>Gets paginated audit logs with optional filters.</summary>
    public async Task<PagedList<AuditLogDto>> GetAuditLogsAsync(int page, int pageSize,
        Guid? userId = null, string? entityType = null, DateTime? from = null, DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var paged = await _auditLogRepository.GetPagedAsync(page, pageSize, userId, entityType, from, to, cancellationToken);
        var dtos = paged.Items.Select(a => _mapper.Map<AuditLogDto>(a)).ToList();
        return new PagedList<AuditLogDto>(dtos, paged.TotalCount, page, pageSize);
    }

    // ── AI Settings ──────────────────────────────────────────────────────

    /// <summary>Gets AI coach settings.</summary>
    public async Task<AiSettingDto?> GetAiSettingAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _context.AiSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return setting is null ? null : _mapper.Map<AiSettingDto>(setting);
    }

    /// <summary>Creates or updates AI settings.</summary>
    public async Task<AiSettingDto> UpsertAiSettingAsync(UpdateAiSettingRequest request, CancellationToken cancellationToken = default)
    {
        var setting = await _context.AiSettings.FirstOrDefaultAsync(cancellationToken);
        if (setting is null)
        {
            setting = new AiSetting { CreatedById = _currentUser.UserId };
            _context.AiSettings.Add(setting);
        }
        setting.Provider = request.Provider;
        setting.Model = request.Model;
        setting.ApiKeyEncrypted = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.ApiKey)); // TODO: use proper encryption
        setting.SystemPrompt = request.SystemPrompt;
        setting.Temperature = request.Temperature;
        setting.IsEnabled = request.IsEnabled;
        setting.SetUpdated(_currentUser.UserId!.Value);
        await _context.SaveChangesAsync(cancellationToken);
        return _mapper.Map<AiSettingDto>(setting);
    }
}
