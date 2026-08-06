using AutoMapper;
using Microsoft.Extensions.Logging;
using RunningCompetition.Application.DTOs.Runs;
using RunningCompetition.Contracts.Repositories;
using RunningCompetition.Contracts.Services;
using RunningCompetition.Domain.Entities;
using RunningCompetition.Domain.Enums;
using RunningCompetition.Infrastructure.Hubs;
using RunningCompetition.Shared.Common;
using RunningCompetition.Shared.Constants;
using RunningCompetition.Shared.Exceptions;

namespace RunningCompetition.Application.Services;

/// <summary>Handles running session business logic.</summary>
public sealed class RunService
{
    private readonly IRunSessionRepository _runRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAchievementRepository _achievementRepository;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUserService _currentUser;
    private readonly RunHubService _hubService;
    private readonly IMapper _mapper;
    private readonly ILogger<RunService> _logger;

    public RunService(
        IRunSessionRepository runRepository, IUserRepository userRepository,
        IAchievementRepository achievementRepository, ICacheService cacheService,
        ICurrentUserService currentUser, RunHubService hubService,
        IMapper mapper, ILogger<RunService> logger)
    {
        _runRepository = runRepository;
        _userRepository = userRepository;
        _achievementRepository = achievementRepository;
        _cacheService = cacheService;
        _currentUser = currentUser;
        _hubService = hubService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Starts a new run session. Fails if there is already an active session.</summary>
    public async Task<RunSessionDto> StartRunAsync(StartRunRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;

        var active = await _runRepository.GetActiveSessionAsync(userId, cancellationToken);
        if (active is not null)
            throw new BusinessRuleException("You already have an active run session. Finish or abandon it before starting a new one.");

        var session = new RunSession
        {
            UserId = userId,
            Status = RunStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            StartLatitude = request.StartLatitude,
            StartLongitude = request.StartLongitude,
            CreatedById = userId
        };

        await _runRepository.AddAsync(session, cancellationToken);
        await _runRepository.SaveChangesAsync(cancellationToken);

        await _cacheService.SetAsync(CacheKeys.ActiveRun(userId), session.Id, TimeSpan.FromHours(12), cancellationToken);
        _logger.LogInformation("User {UserId} started run {RunId}", userId, session.Id);
        return _mapper.Map<RunSessionDto>(session);
    }

    /// <summary>Pauses the active run session.</summary>
    public async Task<RunSessionDto> PauseRunAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var session = await GetActiveOrThrowAsync(userId, cancellationToken);

        if (session.Status != RunStatus.InProgress)
            throw new BusinessRuleException("Run is not in progress.");

        session.Status = RunStatus.Paused;
        session.Pauses.Add(new RunPause { RunSessionId = session.Id, PausedAt = DateTime.UtcNow });
        session.SetUpdated(userId);
        _runRepository.Update(session);
        await _runRepository.SaveChangesAsync(cancellationToken);
        return _mapper.Map<RunSessionDto>(session);
    }

    /// <summary>Resumes a paused run session.</summary>
    public async Task<RunSessionDto> ResumeRunAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var session = await GetActiveOrThrowAsync(userId, cancellationToken);

        if (session.Status != RunStatus.Paused)
            throw new BusinessRuleException("Run is not paused.");

        session.Status = RunStatus.InProgress;
        var lastPause = session.Pauses.OrderByDescending(p => p.PausedAt).FirstOrDefault(p => p.ResumedAt is null);
        if (lastPause is not null) lastPause.ResumedAt = DateTime.UtcNow;
        session.SetUpdated(userId);
        _runRepository.Update(session);
        await _runRepository.SaveChangesAsync(cancellationToken);
        return _mapper.Map<RunSessionDto>(session);
    }

    /// <summary>Finishes a run session and processes achievements, streak, and stats.</summary>
    public async Task<RunSessionDto> FinishRunAsync(FinishRunRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var session = await GetActiveOrThrowAsync(userId, cancellationToken);

        session.Status = RunStatus.Completed;
        session.FinishedAt = DateTime.UtcNow;
        session.EndLatitude = request.EndLatitude;
        session.EndLongitude = request.EndLongitude;
        session.Notes = request.Notes;
        session.WeatherCondition = request.WeatherCondition;
        session.TemperatureCelsius = request.TemperatureCelsius;

        // Calculate duration excluding pauses
        var totalPauseSeconds = session.Pauses.Sum(p => p.DurationSeconds ?? 0);
        session.DurationSeconds = session.StartedAt.HasValue
            ? (long)(DateTime.UtcNow - session.StartedAt.Value).TotalSeconds - totalPauseSeconds
            : 0;

        if (session.DurationSeconds > 0 && session.DistanceMeters > 0)
        {
            session.AveragePaceSecondsPerKm = session.DurationSeconds / (session.DistanceMeters / 1000.0);
            session.AverageSpeedKmh = (session.DistanceMeters / 1000.0) / (session.DurationSeconds / 3600.0);
        }

        session.SetUpdated(userId);
        _runRepository.Update(session);

        // Update user aggregate stats
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is not null)
        {
            user.TotalDistanceMeters += session.DistanceMeters;
            user.TotalRuns++;
            user.TotalDurationSeconds += session.DurationSeconds;
            user.TotalCalories += session.CaloriesBurned;
            user.LastRunAt = DateTime.UtcNow;

            // Update streak
            var yesterday = DateTime.UtcNow.Date.AddDays(-1);
            if (user.LastRunAt?.Date == yesterday || user.LastRunAt?.Date == DateTime.UtcNow.Date)
                user.CurrentStreak++;
            else
                user.CurrentStreak = 1;

            if (user.CurrentStreak > user.LongestStreak)
                user.LongestStreak = user.CurrentStreak;

            user.SetUpdated(userId);
            _userRepository.Update(user);
        }

        await _runRepository.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync(CacheKeys.ActiveRun(userId), cancellationToken);
        await _cacheService.RemoveAsync(CacheKeys.UserProfile(userId), cancellationToken);

        // Fire achievement check (non-blocking)
        _ = CheckAchievementsAsync(userId, session, cancellationToken);

        // Notify via SignalR
        await _hubService.SendRunCompletedAsync(session.Id.ToString(), _mapper.Map<RunSessionDto>(session));

        _logger.LogInformation("User {UserId} finished run {RunId}: {Distance}m in {Duration}s", userId, session.Id, session.DistanceMeters, session.DurationSeconds);
        return _mapper.Map<RunSessionDto>(session);
    }

    /// <summary>Batch-uploads GPS waypoints for an active session.</summary>
    public async Task UploadGpsAsync(Guid sessionId, GpsBatchRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var session = await _runRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new NotFoundException("RunSession", sessionId);

        if (session.UserId != userId) throw new ForbiddenException();
        if (session.Status != RunStatus.InProgress) throw new BusinessRuleException("Session is not in progress.");

        var locations = request.Locations.Select(l => new GpsLocation
        {
            RunSessionId = sessionId,
            Latitude = l.Latitude,
            Longitude = l.Longitude,
            AltitudeMeters = l.AltitudeMeters,
            AccuracyMeters = l.AccuracyMeters,
            SpeedMs = l.SpeedMs,
            Timestamp = l.Timestamp,
            Sequence = l.Sequence,
            CreatedById = userId
        });

        await _runRepository.BatchInsertGpsLocationsAsync(locations, cancellationToken);

        // Update distance (rough Haversine approximation via last GPS point speed)
        var lastLocation = request.Locations.OrderByDescending(l => l.Sequence).First();
        if (lastLocation.SpeedMs.HasValue)
        {
            await _hubService.SendGpsUpdateAsync(sessionId.ToString(), lastLocation.Latitude, lastLocation.Longitude, lastLocation.SpeedMs);
        }
    }

    /// <summary>Gets a paginated list of the user's run sessions.</summary>
    public async Task<PagedList<RunSessionDto>> GetMyRunsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var sessions = await _runRepository.GetByUserAsync(userId, page, pageSize, cancellationToken);
        var dtos = sessions.Items.Select(s => _mapper.Map<RunSessionDto>(s)).ToList();
        return new PagedList<RunSessionDto>(dtos, sessions.TotalCount, page, pageSize);
    }

    /// <summary>Gets a detailed run session by ID.</summary>
    public async Task<RunSessionDetailDto> GetRunDetailAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _runRepository.GetWithDetailsAsync(sessionId, cancellationToken)
            ?? throw new NotFoundException("RunSession", sessionId);

        if (session.UserId != _currentUser.UserId && !_currentUser.IsSuperAdmin)
            throw new ForbiddenException();

        return new RunSessionDetailDto(
            _mapper.Map<RunSessionDto>(session),
            session.GpsLocations.Select(g => _mapper.Map<GpsLocationDto>(g)).ToList().AsReadOnly(),
            session.Laps.Select(l => _mapper.Map<RunLapDto>(l)).ToList().AsReadOnly());
    }

    /// <summary>Gets run stats for a user over a time period.</summary>
    public async Task<RunStatsDto> GetStatsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var (dist, dur, runs, cal) = await _runRepository.GetStatsAsync(userId, from, to, cancellationToken);
        var avgPace = dist > 0 && dur > 0 ? dur / (dist / 1000.0) : (double?)null;
        return new RunStatsDto(dist, dur, runs, cal, avgPace);
    }

    private async Task<RunSession> GetActiveOrThrowAsync(Guid userId, CancellationToken cancellationToken)
    {
        var session = await _runRepository.GetActiveSessionAsync(userId, cancellationToken);
        return session ?? throw new NotFoundException("No active run session found.");
    }

    private async Task CheckAchievementsAsync(Guid userId, RunSession session, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) return;

            var checks = new List<(BadgeType type, double threshold, Func<bool> condition)>
            {
                (BadgeType.FirstRun, 1, () => user.TotalRuns == 1),
                (BadgeType.Distance5K, 5000, () => session.DistanceMeters >= 5000),
                (BadgeType.Distance10K, 10000, () => session.DistanceMeters >= 10000),
                (BadgeType.HalfMarathon, 21100, () => session.DistanceMeters >= 21100),
                (BadgeType.Marathon, 42200, () => session.DistanceMeters >= 42200),
                (BadgeType.Streak7Days, 7, () => user.CurrentStreak >= 7),
                (BadgeType.Streak30Days, 30, () => user.CurrentStreak >= 30),
                (BadgeType.Streak100Days, 100, () => user.CurrentStreak >= 100),
            };

            foreach (var (type, _, condition) in checks)
            {
                if (!condition()) continue;
                var alreadyEarned = await _achievementRepository.HasEarnedAsync(userId, type, cancellationToken);
                if (alreadyEarned) continue;

                var achievement = await _achievementRepository.FindFirstAsync(a => a.BadgeType == type, cancellationToken);
                if (achievement is null) continue;

                var userAchievement = new UserAchievement
                {
                    UserId = userId, AchievementId = achievement.Id,
                    EarnedAt = DateTime.UtcNow, RunSessionId = session.Id
                };
                user.XpPoints += achievement.XpReward;
                user.Level = CalculateLevel(user.XpPoints);
                user.SetUpdated(userId);
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync(cancellationToken);
                await _hubService.SendAchievementUnlockedAsync(userId.ToString(), new { achievement.Name, achievement.XpReward });
                _logger.LogInformation("User {UserId} earned achievement '{Achievement}'", userId, achievement.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Achievement check failed for user {UserId}", userId);
        }
    }

    private static int CalculateLevel(int xp) => xp switch
    {
        < 100 => 1, < 300 => 2, < 600 => 3, < 1000 => 4, < 1500 => 5,
        < 2500 => 6, < 4000 => 7, < 6000 => 8, < 9000 => 9, _ => 10
    };
}
