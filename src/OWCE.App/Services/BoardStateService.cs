using OWCE.Contracts;

namespace OWCE.Services;

/// <summary>
/// Processes raw BLE characteristic bytes into a typed, immutable BoardState snapshot.
/// This is the spiritual successor to the SetValue() method in the original OWBoard.cs,
/// but with all UI, navigation, and HTTP concerns removed.
///
/// All UUID constants live in BLEUuids (OWCE.Contracts). This class no longer
/// exposes UUID constants — any code that previously referenced BoardStateService.*Uuid
/// must now reference BLEUuids.* instead. (Fixes code review finding #3.)
/// </summary>
public sealed class BoardStateService : IBoardStateService
{
    // ── Motor wheel circumference per board type (meters) ────────────────────
    private const float CircumferenceV1PlusXR = 0.9177f;  // 11.5" tire
    private const float CircumferencePint     = 0.8379f;  // 10.5" tire

    // ── Mutable internal state ────────────────────────────────────────────────
    private readonly object _lock = new();

    private OWBoardType _boardType = OWBoardType.Unknown;
    private string _serialNumber = string.Empty;
    private int _firmwareRevision;
    private int _hardwareRevision;
    private int _rpm;
    private float _speedMph;
    private int _batteryPercent;
    private float _batteryVoltage;
    private float _tripOdometerMiles;
    private float _lifetimeOdometerMiles;
    private float _tripAmpHours;
    private float _tripRegenAmpHours;
    private float _currentAmps;
    private bool _isRegen;
    private float _controllerTempC;
    private float _motorTempC;
    private float _batteryTempC;
    private int _rideMode;
    private string _rideModeString = string.Empty;
    private bool _lightMode;
    private int _frontLightMode;
    private int _rearLightMode;
    private int _rssi;
    private bool? _simpleStopEnabled;

    public BoardState? CurrentState { get; private set; }

    public event EventHandler<BoardState>? StateUpdated;

    public void ProcessValueUpdate(string characteristicUuid, byte[] data)
    {
        if (data is null || data.Length == 0) return;

        lock (_lock)
        {
            bool changed = true;

            if (characteristicUuid.Equals(BLEUuids.SerialNumber, StringComparison.OrdinalIgnoreCase))
            {
                _serialNumber = System.Text.Encoding.UTF8.GetString(data).Trim('\0');
            }
            else if (characteristicUuid.Equals(BLEUuids.HardwareRevision, StringComparison.OrdinalIgnoreCase))
            {
                _hardwareRevision = ParseUInt16BigEndian(data);
                _boardType = DetectBoardType(_hardwareRevision);
            }
            else if (characteristicUuid.Equals(BLEUuids.FirmwareRevision, StringComparison.OrdinalIgnoreCase))
            {
                _firmwareRevision = ParseUInt16BigEndian(data);
            }
            else if (characteristicUuid.Equals(BLEUuids.Rpm, StringComparison.OrdinalIgnoreCase))
            {
                _rpm = ParseInt16BigEndian(data);
                // Speed in mph: RPM × circumference (m) × 60 / 1609.34
                float circumference = _boardType is OWBoardType.Pint or OWBoardType.PintX
                    ? CircumferencePint
                    : CircumferenceV1PlusXR;
                _speedMph = Math.Abs(_rpm) * circumference * 60f / 1609.34f;
            }
            else if (characteristicUuid.Equals(BLEUuids.BatteryPercent, StringComparison.OrdinalIgnoreCase))
            {
                _batteryPercent = data[0];
            }
            else if (characteristicUuid.Equals(BLEUuids.BatteryVoltage, StringComparison.OrdinalIgnoreCase))
            {
                _batteryVoltage = ParseUInt16BigEndian(data) / 10f;
            }
            else if (characteristicUuid.Equals(BLEUuids.Temperature, StringComparison.OrdinalIgnoreCase))
            {
                // Controller temp in low byte, motor temp in high byte (°C × 10)
                if (data.Length >= 2)
                {
                    _controllerTempC = ParseInt16BigEndian(data) / 10f;
                }
                if (data.Length >= 4)
                {
                    _motorTempC = BitConverter.ToInt16(new[] { data[3], data[2] }) / 10f;
                }
            }
            else if (characteristicUuid.Equals(BLEUuids.BatteryTemperature, StringComparison.OrdinalIgnoreCase))
            {
                _batteryTempC = ParseInt16BigEndian(data) / 10f;
            }
            else if (characteristicUuid.Equals(BLEUuids.TripOdometer, StringComparison.OrdinalIgnoreCase))
            {
                // Trip odometer in meters, convert to miles
                var meters = ParseUInt32BigEndian(data);
                _tripOdometerMiles = meters / 1609.34f;
            }
            else if (characteristicUuid.Equals(BLEUuids.LifetimeOdometer, StringComparison.OrdinalIgnoreCase))
            {
                var meters = ParseUInt32BigEndian(data);
                _lifetimeOdometerMiles = meters / 1609.34f;
            }
            else if (characteristicUuid.Equals(BLEUuids.CurrentAmps, StringComparison.OrdinalIgnoreCase))
            {
                var raw = ParseInt16BigEndian(data);
                _currentAmps = raw / 10f;
                _isRegen = raw < 0;
            }
            else if (characteristicUuid.Equals(BLEUuids.TripAmpHours, StringComparison.OrdinalIgnoreCase))
            {
                _tripAmpHours = ParseUInt16BigEndian(data) / 1000f;
            }
            else if (characteristicUuid.Equals(BLEUuids.TripRegenAmpHours, StringComparison.OrdinalIgnoreCase))
            {
                _tripRegenAmpHours = ParseUInt16BigEndian(data) / 1000f;
            }
            else if (characteristicUuid.Equals(BLEUuids.RideMode, StringComparison.OrdinalIgnoreCase))
            {
                _rideMode = data[0];
                _rideModeString = MapRideMode(_boardType, _rideMode);
            }
            else if (characteristicUuid.Equals(BLEUuids.LightMode, StringComparison.OrdinalIgnoreCase))
            {
                _lightMode = data[0] != 0;
            }
            else if (characteristicUuid.Equals(BLEUuids.LightsFront, StringComparison.OrdinalIgnoreCase))
            {
                _frontLightMode = data[0];
            }
            else if (characteristicUuid.Equals(BLEUuids.LightsBack, StringComparison.OrdinalIgnoreCase))
            {
                _rearLightMode = data[0];
            }
            else if (characteristicUuid.Equals(BLEUuids.SimpleStop, StringComparison.OrdinalIgnoreCase))
            {
                _simpleStopEnabled = data[0] != 0;
            }
            else
            {
                changed = false;
            }

            if (changed)
            {
                CurrentState = BuildSnapshot();
                StateUpdated?.Invoke(this, CurrentState);
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _boardType = OWBoardType.Unknown;
            _serialNumber = string.Empty;
            _firmwareRevision = 0;
            _hardwareRevision = 0;
            _rpm = 0;
            _speedMph = 0;
            _batteryPercent = 0;
            _batteryVoltage = 0;
            _tripOdometerMiles = 0;
            _lifetimeOdometerMiles = 0;
            _tripAmpHours = 0;
            _tripRegenAmpHours = 0;
            _currentAmps = 0;
            _isRegen = false;
            _controllerTempC = 0;
            _motorTempC = 0;
            _batteryTempC = 0;
            _rideMode = 0;
            _rideModeString = string.Empty;
            _lightMode = false;
            _frontLightMode = 0;
            _rearLightMode = 0;
            _rssi = 0;
            _simpleStopEnabled = null;
            CurrentState = null;
        }
    }

    private BoardState BuildSnapshot() => new()
    {
        BoardType             = _boardType,
        SerialNumber          = _serialNumber,
        FirmwareRevision      = _firmwareRevision,
        HardwareRevision      = _hardwareRevision,
        SpeedMph              = _speedMph,
        BatteryPercent        = _batteryPercent,
        BatteryVoltage        = _batteryVoltage,
        Rpm                   = _rpm,
        TripOdometerMiles     = _tripOdometerMiles,
        LifetimeOdometerMiles = _lifetimeOdometerMiles,
        TripAmpHours          = _tripAmpHours,
        TripRegenAmpHours     = _tripRegenAmpHours,
        CurrentAmps           = _currentAmps,
        IsRegen               = _isRegen,
        ControllerTempC       = _controllerTempC,
        MotorTempC            = _motorTempC,
        BatteryTempC          = _batteryTempC,
        RideMode              = _rideMode,
        RideModeString        = _rideModeString,
        LightMode             = _lightMode,
        FrontLightMode        = _frontLightMode,
        RearLightMode         = _rearLightMode,
        Rssi                  = _rssi,
        SimpleStopEnabled     = _simpleStopEnabled,
    };

    // ── Board type detection from hardware revision ───────────────────────────

    private static OWBoardType DetectBoardType(int hardwareRevision) => hardwareRevision switch
    {
        >= 1 and <= 999   => OWBoardType.V1,
        >= 2000 and <= 2999 => OWBoardType.Plus,
        >= 3000 and <= 3999 => OWBoardType.XR,
        >= 4000 and <= 4999 => OWBoardType.Pint,
        >= 5000 and <= 5999 => OWBoardType.PintX,
        >= 6000 and <= 7999 => OWBoardType.GT,
        >= 8000 and <= 8999 => OWBoardType.GTS,
        _ => OWBoardType.Unknown,
    };

    // ── Ride mode string mapping ──────────────────────────────────────────────

    private static string MapRideMode(OWBoardType boardType, int mode) =>
        boardType switch
        {
            OWBoardType.V1 or OWBoardType.Plus or OWBoardType.XR => mode switch
            {
                1 => "Classic",
                2 => "Elevated",
                3 => "Cruz",
                4 => "Mission",
                5 => "Delirium",
                6 => "Custom",
                _ => $"Mode {mode}",
            },
            OWBoardType.Pint or OWBoardType.PintX => mode switch
            {
                1 => "Redwood",
                2 => "Pacific",
                3 => "Skyline",
                4 => "Custom",
                _ => $"Mode {mode}",
            },
            OWBoardType.GT or OWBoardType.GTS => mode switch
            {
                1 => "Sequoia",
                2 => "Cruz",
                3 => "Highline",
                4 => "Elevated",
                5 => "Delirium",
                6 => "Custom",
                _ => $"Mode {mode}",
            },
            _ => $"Mode {mode}",
        };

    // ── Byte parsing helpers ──────────────────────────────────────────────────

    private static int ParseUInt16BigEndian(byte[] data) =>
        data.Length >= 2 ? (data[0] << 8) | data[1] : data[0];

    private static int ParseInt16BigEndian(byte[] data) =>
        data.Length >= 2 ? (short)((data[0] << 8) | data[1]) : (sbyte)data[0];

    private static uint ParseUInt32BigEndian(byte[] data) =>
        data.Length >= 4
            ? ((uint)data[0] << 24) | ((uint)data[1] << 16) | ((uint)data[2] << 8) | data[3]
            : 0;
}
