using RunningCompetition.Domain.Enums;

namespace RunningCompetition.Application.DTOs.Runs;

// ─── Request DTOs ────────────────────────────────────────────────────────────

/// <summary>Request to start a new run session.</summary>
public sealed record StartRunRequest(
    double? StartLatitude = null,
    double? StartLongitude = null);

/// <summary>Batch GPS location upload request.</summary>
public sealed record GpsBatchRequest(IReadOnlyList<GpsLocationRequest> Locations);

/// <summary>A single GPS waypoint in a batch upload.</summary>
public sealed record GpsLocationRequest(
    double Latitude,
    double Longitude,
    double? AltitudeMeters,
    double? AccuracyMeters,
    double? SpeedMs,
    DateTime Timestamp,
    int Sequence);

/// <summary>Request to finish a run session.</summary>
public sealed record FinishRunRequest(
    double? EndLatitude = null,
    double? EndLongitude = null,
    string? Notes = null,
    string? WeatherCondition = null,
    double? TemperatureCelsius = null);

// ─── Response DTOs ───────────────────────────────────────────────────────────

/// <summary>Run session summary response.</summary>
public sealed record RunSessionDto(
    Guid Id,
    RunStatus Status,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    double DistanceMeters,
    long DurationSeconds,
    double CaloriesBurned,
    double? AveragePaceSecondsPerKm,
    double? BestPaceSecondsPerKm,
    double? AverageSpeedKmh,
    double? MaxSpeedKmh,
    double? ElevationGainMeters,
    int? AverageHeartRateBpm,
    string? Notes,
    bool IsShared,
    DateTime CreatedAt);

/// <summary>Detailed run session including GPS and laps.</summary>
public sealed record RunSessionDetailDto(
    RunSessionDto Session,
    IReadOnlyList<GpsLocationDto> GpsLocations,
    IReadOnlyList<RunLapDto> Laps);

/// <summary>GPS location DTO.</summary>
public sealed record GpsLocationDto(
    double Latitude,
    double Longitude,
    double? AltitudeMeters,
    double? SpeedMs,
    DateTime Timestamp,
    int Sequence);

/// <summary>Lap split DTO.</summary>
public sealed record RunLapDto(
    int LapNumber,
    double DistanceMeters,
    long DurationSeconds,
    double? PaceSecondsPerKm,
    int? AverageHeartRateBpm);

/// <summary>Dashboard run statistics.</summary>
public sealed record RunStatsDto(
    double TotalDistanceMeters,
    long TotalDurationSeconds,
    int TotalRuns,
    double TotalCalories,
    double? AveragePaceSecondsPerKm);
