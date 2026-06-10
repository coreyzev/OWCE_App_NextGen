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

    // ── BLE UUID Constants ────────────────────────────────────────────────────
    // All well-known Onewheel BLE characteristic UUIDs.
    // Centralised here in Contracts so no service implementation needs to import
    // another service's type to reference a UUID. (Fixes code review finding #3.)

    public static class BLEUuids
    {
        public const string SerialNumber       = "E659F301-EA98-11E3-AC10-0800200C9A66";
        public const string Rpm                = "E659F302-EA98-11E3-AC10-0800200C9A66";
        public const string BatteryPercent     = "E659F303-EA98-11E3-AC10-0800200C9A66";
        public const string Temperature        = "E659F304-EA98-11E3-AC10-0800200C9A66";
        public const string TripOdometer       = "E659F305-EA98-11E3-AC10-0800200C9A66";
        public const string LightMode          = "E659F306-EA98-11E3-AC10-0800200C9A66";
        public const string LightsFront        = "E659F307-EA98-11E3-AC10-0800200C9A66";
        public const string LightsBack         = "E659F308-EA98-11E3-AC10-0800200C9A66";
        public const string RideMode           = "E659F30C-EA98-11E3-AC10-0800200C9A66";
        public const string BatterySerial      = "E659F30D-EA98-11E3-AC10-0800200C9A66";
        public const string BatteryTemperature = "E659F30E-EA98-11E3-AC10-0800200C9A66";
        public const string CurrentAmps        = "E659F312-EA98-11E3-AC10-0800200C9A66";
        public const string TripAmpHours       = "E659F313-EA98-11E3-AC10-0800200C9A66";
        public const string TripRegenAmpHours  = "E659F314-EA98-11E3-AC10-0800200C9A66";
        public const string BatteryVoltage     = "E659F315-EA98-11E3-AC10-0800200C9A66";
        public const string SafetyHeadroom     = "E659F317-EA98-11E3-AC10-0800200C9A66";
        public const string HardwareRevision   = "E659F318-EA98-11E3-AC10-0800200C9A66";
        public const string FirmwareRevision   = "E659F311-EA98-11E3-AC10-0800200C9A66";
        public const string LifetimeOdometer   = "E659F319-EA98-11E3-AC10-0800200C9A66";
        public const string LifetimeAmpHours   = "E659F31A-EA98-11E3-AC10-0800200C9A66";
        public const string BatteryCells       = "E659F31B-EA98-11E3-AC10-0800200C9A66";
        public const string SerialRead         = "E659F31C-EA98-11E3-AC10-0800200C9A66";
        public const string SerialWrite        = "E659F31D-EA98-11E3-AC10-0800200C9A66";
        public const string SimpleStop         = "E659F31E-EA98-11E3-AC10-0800200C9A66";
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
    /// Supports V1/Plus/XR (no handshake), Pint/PintX/GT (Gemini), and GT-S (Polaris 6215).
    ///
    /// Keep-alive is fully self-managed internally after PerformHandshakeAsync completes.
    /// Callers MUST NOT invoke keep-alive externally. (Fixes code review finding #5.)
    /// </summary>
    public interface IHandshakeService
    {
        Task PerformHandshakeAsync(OWBoardType boardType, int firmwareRevision, CancellationToken cancellationToken);
        // NOTE: KeepAliveAsync is intentionally NOT on this interface.
        // The GT-S 15-second keep-alive is managed internally by the implementation
        // via an injected IDispatcher timer. Exposing it publicly caused ambiguity
        // about ownership and risked double-firing. See ADR-002.
    }

    /// <summary>
    /// Orchestrates the full board connection lifecycle:
    /// BLE connect → identify → handshake → subscribe → telemetry.
    /// ViewModels depend on this interface, not the concrete class. (Fixes finding #2.)
    /// </summary>
    public interface IBoardConnectionService
    {
        Task ConnectAsync(string deviceId, CancellationToken cancellationToken);
        Task DisconnectAsync();
    }

    /// <summary>
    /// Records ride sessions and persists them to SQLite.
    /// EstimatedRangeMiles is computed here, not in the ViewModel. (Fixes finding #7.)
    /// </summary>
    public interface IRideService
    {
        RideSession? CurrentRide { get; }
        bool IsRecording { get; }
        float TopSpeedThisRide { get; }

        /// <summary>
        /// Simple linear range estimate based on battery percentage and board type.
        /// Exposed here so ViewModels and the watch sync payload can use it without
        /// duplicating the board-type lookup table.
        /// </summary>
        float EstimateRangeMiles(OWBoardType boardType, int batteryPercent);

        Task StartRideAsync(string boardSerial, OWBoardType boardType, CancellationToken cancellationToken);
        Task EndRideAsync();
        Task<IReadOnlyList<RideSession>> GetRideHistoryAsync();
        Task<float> GetAllTimeTopSpeedAsync();

        event EventHandler<RideSession> RideEnded;
    }

    /// <summary>
    /// Pushes live ride data to the paired smartwatch (iOS: WCSession, Android: DataClient).
    /// Platform implementations live in OWCE.App/Platforms/{iOS,Android}/Services/.
    /// Registered via platform-specific MauiProgram partial files.
    /// </summary>
    public interface IWatchSyncService
    {
        /// <summary>Whether a watch is currently paired and reachable.</summary>
        bool IsWatchReachable { get; }

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
