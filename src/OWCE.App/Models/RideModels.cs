using SQLite;
using OWCE.Contracts;

namespace OWCE.Models;

/// <summary>
/// SQLite table: one row per completed ride session.
/// Maps to the RideSession contract model.
/// </summary>
[Table("RideSessions")]
public class RideSessionEntity
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    /// <summary>Top speed achieved during this ride, in mph.</summary>
    public float TopSpeedMph { get; set; }

    /// <summary>Average speed for the ride (computed from data points), in mph.</summary>
    public float AvgSpeedMph { get; set; }

    /// <summary>Total distance from the board's trip odometer, in miles.</summary>
    public float DistanceMiles { get; set; }

    public string BoardSerial { get; set; } = string.Empty;

    /// <summary>Stored as int; cast to OWBoardType on read.</summary>
    public int BoardTypeInt { get; set; }

    [Ignore]
    public OWBoardType BoardType
    {
        get => (OWBoardType)BoardTypeInt;
        set => BoardTypeInt = (int)value;
    }

    public RideSession ToContract() => new()
    {
        Id           = Id,
        StartTime    = StartTime,
        EndTime      = EndTime,
        TopSpeedMph  = TopSpeedMph,
        AvgSpeedMph  = AvgSpeedMph,
        DistanceMiles = DistanceMiles,
        BoardSerial  = BoardSerial,
        BoardType    = BoardType,
    };

    public static RideSessionEntity FromContract(RideSession r) => new()
    {
        Id           = r.Id,
        StartTime    = r.StartTime,
        EndTime      = r.EndTime,
        TopSpeedMph  = r.TopSpeedMph,
        AvgSpeedMph  = r.AvgSpeedMph,
        DistanceMiles = r.DistanceMiles,
        BoardSerial  = r.BoardSerial,
        BoardType    = r.BoardType,
    };
}

/// <summary>
/// SQLite table: one row per 1-Hz telemetry sample during a ride.
/// Nullable GPS fields allow ride recording even without location permission.
/// </summary>
[Table("RideDataPoints")]
public class RideDataPointEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string RideSessionId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }
    public float SpeedMph { get; set; }
    public int BatteryPercent { get; set; }

    /// <summary>Nullable — GPS may not be available or permitted.</summary>
    public double? LatitudeDeg { get; set; }
    public double? LongitudeDeg { get; set; }
    public double? AltitudeMeters { get; set; }
}
