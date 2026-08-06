using RunningCompetition.Domain.Common;
using RunningCompetition.Domain.Enums;

namespace RunningCompetition.Domain.Entities;

/// <summary>Represents a single running session.</summary>
public class RunSession : BaseEntity
{
    /// <summary>Gets or sets the user who performed this run.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the run status.</summary>
    public RunStatus Status { get; set; } = RunStatus.NotStarted;

    /// <summary>Gets or sets when the run was started.</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>Gets or sets when the run was finished.</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>Gets or sets the total distance in meters.</summary>
    public double DistanceMeters { get; set; }

    /// <summary>Gets or sets the total active duration in seconds (excluding pauses).</summary>
    public long DurationSeconds { get; set; }

    /// <summary>Gets or sets the calories burned.</summary>
    public double CaloriesBurned { get; set; }

    /// <summary>Gets or sets the average pace in seconds per kilometer.</summary>
    public double? AveragePaceSecondsPerKm { get; set; }

    /// <summary>Gets or sets the best pace in seconds per kilometer.</summary>
    public double? BestPaceSecondsPerKm { get; set; }

    /// <summary>Gets or sets the average speed in km/h.</summary>
    public double? AverageSpeedKmh { get; set; }

    /// <summary>Gets or sets the maximum speed in km/h.</summary>
    public double? MaxSpeedKmh { get; set; }

    /// <summary>Gets or sets the elevation gain in meters.</summary>
    public double? ElevationGainMeters { get; set; }

    /// <summary>Gets or sets the elevation loss in meters.</summary>
    public double? ElevationLossMeters { get; set; }

    /// <summary>Gets or sets average heart rate in BPM.</summary>
    public int? AverageHeartRateBpm { get; set; }

    /// <summary>Gets or sets max heart rate in BPM.</summary>
    public int? MaxHeartRateBpm { get; set; }

    /// <summary>Gets or sets the starting latitude.</summary>
    public double? StartLatitude { get; set; }

    /// <summary>Gets or sets the starting longitude.</summary>
    public double? StartLongitude { get; set; }

    /// <summary>Gets or sets the ending latitude.</summary>
    public double? EndLatitude { get; set; }

    /// <summary>Gets or sets the ending longitude.</summary>
    public double? EndLongitude { get; set; }

    /// <summary>Gets or sets user notes about the run.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets whether the run is shared publicly.</summary>
    public bool IsShared { get; set; }

    /// <summary>Gets or sets the weather conditions during the run.</summary>
    public string? WeatherCondition { get; set; }

    /// <summary>Gets or sets the temperature during the run in Celsius.</summary>
    public double? TemperatureCelsius { get; set; }

    // Navigation
    /// <summary>Gets or sets the user who performed this run.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the GPS waypoints for this run.</summary>
    public ICollection<GpsLocation> GpsLocations { get; set; } = [];

    /// <summary>Gets or sets the lap splits for this run.</summary>
    public ICollection<RunLap> Laps { get; set; } = [];

    /// <summary>Gets or sets the pause events for this run.</summary>
    public ICollection<RunPause> Pauses { get; set; } = [];
}

/// <summary>Represents a GPS waypoint captured during a run.</summary>
public class GpsLocation : BaseEntity
{
    /// <summary>Gets or sets the run session ID.</summary>
    public Guid RunSessionId { get; set; }

    /// <summary>Gets or sets the latitude.</summary>
    public double Latitude { get; set; }

    /// <summary>Gets or sets the longitude.</summary>
    public double Longitude { get; set; }

    /// <summary>Gets or sets the altitude in meters.</summary>
    public double? AltitudeMeters { get; set; }

    /// <summary>Gets or sets the accuracy in meters.</summary>
    public double? AccuracyMeters { get; set; }

    /// <summary>Gets or sets the speed at this point in m/s.</summary>
    public double? SpeedMs { get; set; }

    /// <summary>Gets or sets the UTC timestamp of this waypoint.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Gets or sets the sequence number within the run.</summary>
    public int Sequence { get; set; }

    // Navigation
    /// <summary>Gets or sets the run session.</summary>
    public RunSession RunSession { get; set; } = null!;
}

/// <summary>Represents a lap split within a run.</summary>
public class RunLap : BaseEntity
{
    /// <summary>Gets or sets the run session ID.</summary>
    public Guid RunSessionId { get; set; }

    /// <summary>Gets or sets the lap number.</summary>
    public int LapNumber { get; set; }

    /// <summary>Gets or sets the lap distance in meters.</summary>
    public double DistanceMeters { get; set; }

    /// <summary>Gets or sets the lap duration in seconds.</summary>
    public long DurationSeconds { get; set; }

    /// <summary>Gets or sets the pace for this lap in seconds per km.</summary>
    public double? PaceSecondsPerKm { get; set; }

    /// <summary>Gets or sets the average heart rate for this lap.</summary>
    public int? AverageHeartRateBpm { get; set; }

    // Navigation
    /// <summary>Gets or sets the run session.</summary>
    public RunSession RunSession { get; set; } = null!;
}

/// <summary>Represents a pause event within a run session.</summary>
public class RunPause : BaseEntity
{
    /// <summary>Gets or sets the run session ID.</summary>
    public Guid RunSessionId { get; set; }

    /// <summary>Gets or sets when the pause started.</summary>
    public DateTime PausedAt { get; set; }

    /// <summary>Gets or sets when the run was resumed.</summary>
    public DateTime? ResumedAt { get; set; }

    /// <summary>Gets the pause duration in seconds.</summary>
    public long? DurationSeconds => ResumedAt.HasValue
        ? (long)(ResumedAt.Value - PausedAt).TotalSeconds
        : null;

    // Navigation
    /// <summary>Gets or sets the run session.</summary>
    public RunSession RunSession { get; set; } = null!;
}
