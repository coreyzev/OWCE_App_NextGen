using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OWCE.Contracts
{
    // ── Enums ─────────────────────────────────────────────────────────────────

    public enum SpeedUnit { Mph, KmH }
    public enum TempUnit { Fahrenheit, Celsius }

    public enum OWBoardType
    {
        Unknown,
        V1,
        Plus,
        XR,
        Pint,
        PintX,
        GT,
        GTS   // Polaris 6215 — hardware revision range 8000–8999
    }

    // ── Shared Data Models ────────────────────────────────────────────────────

    /// <summary>
    /// Payload pushed to the smartwatch companion apps at ~1 Hz during a ride.
    /// </summary>
    public class WatchPayload
    {
        public float CurrentSpeedMph { get; set; }
        public float TopSpeedMph { get; set; }
        public int BatteryPercent { get; set; }
        public float EstimatedRangeMiles { get; set; }
        public SpeedUnit SpeedUnit { get; set; }
        public bool IsRiding { get; set; }
    }

    /// <summary>
    /// A completed or in-progress ride session, persisted to SQLite.
    /// </summary>
    public class RideSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public float TopSpeedMph { get; set; }
        public float AvgSpeedMph { get; set; }
        public float DistanceMiles { get; set; }
        public string BoardSerial { get; set; } = string.Empty;
        public OWBoardType BoardType { get; set; }
    }

    /// <summary>
    /// A discovered BLE peripheral before full connection.
    /// </summary>
    public class DiscoveredBoard
    {
        public string DeviceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Rssi { get; set; }
    }

    /// <summary>
    /// The live telemetry state of a connected board. Immutable snapshot.
    /// </summary>
    public class BoardState
    {
        public OWBoardType BoardType { get; init; }
        public string SerialNumber { get; init; } = string.Empty;
        public int FirmwareRevision { get; init; }
        public int HardwareRevision { get; init; }

        // Ride metrics
        public float SpeedMph { get; init; }
        public int BatteryPercent { get; init; }
        public float BatteryVoltage { get; init; }
        public int Rpm { get; init; }
        public float TripOdometerMiles { get; init; }
        public float LifetimeOdometerMiles { get; init; }
        public float TripAmpHours { get; init; }
        public float TripRegenAmpHours { get; init; }
        public float CurrentAmps { get; init; }
        public bool IsRegen { get; init; }

        // Temperatures
        public float ControllerTempC { get; init; }
        public float MotorTempC { get; init; }
        public float BatteryTempC { get; init; }

        // Ride mode
        public int RideMode { get; init; }
        public string RideModeString { get; init; } = string.Empty;

        // Lights
        public bool LightMode { get; init; }
        public int FrontLightMode { get; init; }
        public int RearLightMode { get; init; }

        // Connection quality
        public int Rssi { get; init; }
        public bool? SimpleStopEnabled { get; init; }
    }

    // ── Service Interfaces ────────────────────────────────────────────────────

    /// <summary>
    /// Abstracts all Bluetooth LE operations. Platform implementations live in
    /// OWCE.App/Services/Platform/. Never access Plugin.BLE directly outside this interface.
    /// </summary>
    public interface IBLEService
    {
        bool IsScanning { get; }
        bool IsConnected { get; }

        Task StartScanAsync(CancellationToken cancellationToken);
        Task StopScanAsync();
        Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken);
        Task DisconnectAsync();
        Task<byte[]> ReadCharacteristicAsync(string characteristicUuid, CancellationToken cancellationToken);
        Task WriteCharacteristicAsync(string characteristicUuid, byte[] data, CancellationToken cancellationToken);
        Task SubscribeToCharacteristicAsync(string characteristicUuid, CancellationToken cancellationToken);

        event EventHandler<DiscoveredBoard> DeviceDiscovered;
        event EventHandler<bool> ConnectionStateChanged;
        event EventHandler<(string CharacteristicUuid, byte[] Data)> ValueUpdated;
        event EventHandler<int> RssiUpdated;
    }

    /// <summary>
    /// Processes raw BLE bytes into a typed, immutable BoardState snapshot.
    /// Raises StateUpdated whenever any property changes.
    /// </summary>
    public interface IBoardStateService
    {
        BoardState? CurrentState { get; }

        /// <summary>Processes a raw BLE value update from IBLEService.</summary>
        void ProcessValueUpdate(string characteristicUuid, byte[] data);

        /// <summary>Resets all state (called on disconnect).</summary>
        void Reset();

        event EventHandler<BoardState> StateUpdated;
    }

    /// <summary>
    /// Manages the BLE handshake protocol for boards that require authentication.
    /// Supports V1/Plus/XR (Gemini), Pint/PintX/GT, and GT-S (Polaris 6215).
    /// </summary>
    public interface IHandshakeService
    {
        Task PerformHandshakeAsync(OWBoardType boardType, int firmwareRevision, CancellationToken cancellationToken);
        Task KeepAliveAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Records ride sessions and persists them to SQLite.
    /// </summary>
    public interface IRideService
    {
        RideSession? CurrentRide { get; }
        bool IsRecording { get; }
        float TopSpeedThisRide { get; }

        Task StartRideAsync(string boardSerial, OWBoardType boardType, CancellationToken cancellationToken);
        Task EndRideAsync();
        Task<IReadOnlyList<RideSession>> GetRideHistoryAsync();
        Task<float> GetAllTimeTopSpeedAsync();

        event EventHandler<RideSession> RideEnded;
    }

    /// <summary>
    /// Pushes live ride data to the paired smartwatch (iOS: WCSession, Android: DataClient).
    /// Platform implementations live in OWCE.App/Services/Platform/.
    /// </summary>
    public interface IWatchSyncService
    {
        Task InitializeAsync();
        Task SyncAsync(WatchPayload payload, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Manages user preferences (units, auto-record, etc.).
    /// </summary>
    public interface IAppSettingsService
    {
        SpeedUnit SpeedUnit { get; set; }
        TempUnit TempUnit { get; set; }
        bool AutoStartRideRecording { get; set; }

        event EventHandler SettingsChanged;
    }
}
