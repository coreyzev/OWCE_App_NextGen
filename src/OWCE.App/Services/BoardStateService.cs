using OWCE.Contracts;

namespace OWCE.Services;

/// <summary>
/// Processes raw BLE characteristic bytes into a typed, immutable BoardState snapshot.
/// This is the spiritual successor to the SetValue() method in the original OWBoard.cs,
/// but with all UI, navigation, and HTTP concerns removed.
///
/// All UUID constants are the well-known Onewheel BLE UUIDs from the original codebase.
/// </summary>
public sealed class BoardStateService : IBoardStateService
{
    // ── BLE Characteristic UUIDs ──────────────────────────────────────────────
    public const string SerialNumberUuid       = "E659F301-EA98-11E3-AC10-0800200C9A66";
    public const string RpmUuid                = "E659F302-EA98-11E3-AC10-0800200C9A66";
    public const string BatteryPercentUuid     = "E659F303-EA98-11E3-AC10-0800200C9A66";
    public const string TemperatureUuid        = "E659F304-EA98-11E3-AC10-0800200C9A66";
    public const string TripOdometerUuid       = "E659F305-EA98-11E3-AC10-0800200C9A66";
    public const string LightModeUuid          = "E659F306-EA98-11E3-AC10-0800200C9A66";
    public const string LightsFrontUuid        = "E659F307-EA98-11E3-AC10-0800200C9A66";
    public const string LightsBackUuid         = "E659F308-EA98-11E3-AC10-0800200C9A66";
    public const string RideModeUuid           = "E659F30C-EA98-11E3-AC10-0800200C9A66";
    public const string BatterySerialUuid      = "E659F30D-EA98-11E3-AC10-0800200C9A66";
    public const string BatteryTemperatureUuid = "E659F30E-EA98-11E3-AC10-0800200C9A66";
    public const string CurrentAmpsUuid        = "E659F312-EA98-11E3-AC10-0800200C9A66";
    public const string TripAmpHoursUuid       = "E659F313-EA98-11E3-AC10-0800200C9A66";
    public const string TripRegenAmpHoursUuid  = "E659F314-EA98-11E3-AC10-0800200C9A66";
    public const string BatteryVoltageUuid     = "E659F315-EA98-11E3-AC10-0800200C9A66";
    public const string SafetyHeadroomUuid     = "E659F317-EA98-11E3-AC10-0800200C9A66";
    public const string HardwareRevisionUuid   = "E659F318-EA98-11E3-AC10-0800200C9A66";
    public const string FirmwareRevisionUuid   = "E659F311-EA98-11E3-AC10-0800200C9A66";
    public const string LifetimeOdometerUuid   = "E659F319-EA98-11E3-AC10-0800200C9A66";
    public const string LifetimeAmpHoursUuid   = "E659F31A-EA98-11E3-AC10-0800200C9A66";
    public const string BatteryCellsUuid       = "E659F31B-EA98-11E3-AC10-0800200C9A66";
    public const string SerialReadUuid         = "E659F31C-EA98-11E3-AC10-0800200C9A66";
    public const string SerialWriteUuid        = "E659F31D-EA98-11E3-AC10-0800200C9A66";
    public const string SimpleStopUuid         = "E659F31E-EA98-11E3-AC10-0800200C9A66";

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
    private bool _lightMode;
    private int _frontLightMode;
    private int _rearLightMode;
    private int _rssi;
    private bool? _simpleStopEnabled;
    private int _cellCount = 16;
    private int _firmwareAtHwRead;

    public BoardState? CurrentState { get; private set; }

    public event EventHandler<BoardState>? StateUpdated;

    public void ProcessValueUpdate(string characteristicUuid, byte[] data)
    {
        if (data is null || data.Length < 1) return;

        lock (_lock)
        {
            ApplyUpdate(characteristicUuid.ToUpperInvariant(), data);
            var newState = BuildSnapshot();
            CurrentState = newState;
            StateUpdated?.Invoke(this, newState);
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
            _lightMode = false;
            _frontLightMode = 0;
            _rearLightMode = 0;
            _rssi = 0;
            _simpleStopEnabled = null;
            _cellCount = 16;
            CurrentState = null;
        }
    }

    private void ApplyUpdate(string uuid, byte[] data)
    {
        // Most characteristics are 2-byte big-endian unsigned shorts
        ushort value = data.Length >= 2
            ? (ushort)((data[0] << 8) | data[1])
            : data[0];

        switch (uuid)
        {
            case var u when u.Equals(HardwareRevisionUuid, StringComparison.OrdinalIgnoreCase):
                _hardwareRevision = value;
                _firmwareAtHwRead = _firmwareRevision;
                SetBoardTypeFromHardwareRevision(value);
                break;

            case var u when u.Equals(FirmwareRevisionUuid, StringComparison.OrdinalIgnoreCase):
                _firmwareRevision = value;
                break;

            case var u when u.Equals(SerialNumberUuid, StringComparison.OrdinalIgnoreCase):
                _serialNumber = BitConverter.ToUInt32(data.Reverse().ToArray(), 0).ToString();
                break;

            case var u when u.Equals(RpmUuid, StringComparison.OrdinalIgnoreCase):
                _rpm = (short)value; // signed — negative RPM = reverse
                _speedMph = RpmToMph(_rpm, _boardType);
                break;

            case var u when u.Equals(BatteryPercentUuid, StringComparison.OrdinalIgnoreCase):
                _batteryPercent = value;
                break;

            case var u when u.Equals(BatteryVoltageUuid, StringComparison.OrdinalIgnoreCase):
                _batteryVoltage = 0.1f * value;
                break;

            case var u when u.Equals(TemperatureUuid, StringComparison.OrdinalIgnoreCase):
                // Controller temp in first byte, motor temp in second byte (Celsius)
                _controllerTempC = data[0];
                _motorTempC = data.Length > 1 ? data[1] : 0;
                break;

            case var u when u.Equals(BatteryTemperatureUuid, StringComparison.OrdinalIgnoreCase):
                _batteryTempC = data[0];
                break;

            case var u when u.Equals(TripOdometerUuid, StringComparison.OrdinalIgnoreCase):
                _tripOdometerMiles = RotationsToMiles(value, _boardType);
                break;

            case var u when u.Equals(LifetimeOdometerUuid, StringComparison.OrdinalIgnoreCase):
                _lifetimeOdometerMiles = RotationsToMiles(value, _boardType);
                break;

            case var u when u.Equals(CurrentAmpsUuid, StringComparison.OrdinalIgnoreCase):
            {
                float scaleFactor = _boardType switch
                {
                    OWBoardType.V1   => 0.0009f,
                    OWBoardType.Plus => 0.0018f,
                    _                => 0.002f,
                };
                // Two's complement for signed value
                int signed = value > 32767 ? (int)value - 65536 : value;
                _currentAmps = signed * scaleFactor;
                _isRegen = _currentAmps < 0;
                break;
            }

            case var u when u.Equals(TripAmpHoursUuid, StringComparison.OrdinalIgnoreCase):
                _tripAmpHours = _boardType == OWBoardType.V1
                    ? value * 0.00009f
                    : value * 0.00018f;
                break;

            case var u when u.Equals(TripRegenAmpHoursUuid, StringComparison.OrdinalIgnoreCase):
                _tripRegenAmpHours = _boardType == OWBoardType.V1
                    ? value * 0.00009f
                    : value * 0.00018f;
                break;

            case var u when u.Equals(RideModeUuid, StringComparison.OrdinalIgnoreCase):
                _rideMode = value;
                break;

            case var u when u.Equals(LightModeUuid, StringComparison.OrdinalIgnoreCase):
                _lightMode = value != 0;
                break;

            case var u when u.Equals(LightsFrontUuid, StringComparison.OrdinalIgnoreCase):
                _frontLightMode = value;
                break;

            case var u when u.Equals(LightsBackUuid, StringComparison.OrdinalIgnoreCase):
                _rearLightMode = value;
                break;

            case var u when u.Equals(SimpleStopUuid, StringComparison.OrdinalIgnoreCase):
                _simpleStopEnabled = value != 0;
                break;
        }
    }

    private void SetBoardTypeFromHardwareRevision(int hwRev)
    {
        (_boardType, _cellCount) = hwRev switch
        {
            >= 1    and <= 2999 => (OWBoardType.V1,    16),
            >= 3000 and <= 3999 => (OWBoardType.Plus,  16),
            >= 4000 and <= 4999 => (OWBoardType.XR,    15),
            >= 5000 and <= 5999 => (OWBoardType.Pint,  15),
            >= 6000 and <= 6999 => (OWBoardType.GT,    18),
            >= 7000 and <= 7999 => (OWBoardType.PintX, 15),
            >= 8000 and <= 8999 => (OWBoardType.GTS,   18),  // GT-S / Polaris 6215
            _                   => (OWBoardType.Unknown, 16),
        };

        if (_simpleStopEnabled is null && _boardType != OWBoardType.V1 && _boardType != OWBoardType.Plus && _boardType != OWBoardType.XR)
            _simpleStopEnabled = false;
    }

    /// <summary>
    /// Converts RPM to mph using the board's wheel circumference.
    /// Speed is stored internally in mph; the UI converts to km/h if needed.
    /// </summary>
    private static float RpmToMph(int rpm, OWBoardType boardType)
    {
        float circumferenceMeters = boardType switch
        {
            OWBoardType.Pint  => CircumferencePint,
            OWBoardType.PintX => CircumferencePint,
            _                 => CircumferenceV1PlusXR,
        };

        // RPM → m/s → mph
        float metersPerSecond = (Math.Abs(rpm) / 60f) * circumferenceMeters;
        return metersPerSecond * 2.23694f;
    }

    /// <summary>
    /// Converts wheel rotations (from TripOdometer / LifetimeOdometer) to miles.
    /// </summary>
    private static float RotationsToMiles(float rotations, OWBoardType boardType)
    {
        float circumferenceMeters = boardType switch
        {
            OWBoardType.Pint  => CircumferencePint,
            OWBoardType.PintX => CircumferencePint,
            _                 => CircumferenceV1PlusXR,
        };
        float meters = rotations * circumferenceMeters;
        return meters * 0.000621371f;
    }

    private string GetRideModeString()
    {
        return _boardType switch
        {
            OWBoardType.V1 => _rideMode switch
            {
                1 => "Classic", 2 => "Extreme", 3 => "Elevated", _ => "Unknown"
            },
            OWBoardType.Plus or OWBoardType.XR => _rideMode switch
            {
                4 => "Sequoia", 5 => "Cruz", 6 => "Mission",
                7 => "Elevated", 8 => "Delirium", 9 => "Custom", _ => "Unknown"
            },
            OWBoardType.Pint or OWBoardType.PintX => _rideMode switch
            {
                5 => "Redwood", 6 => "Pacific", 7 => "Elevated", 8 => "Skyline", _ => "Unknown"
            },
            OWBoardType.GT or OWBoardType.GTS => _rideMode switch
            {
                3 => "Bay", 4 => "Roam", 5 => "Flow",
                6 => "Highline", 7 => "Elevated", 8 => "Apex", 9 => "Custom", _ => "Unknown"
            },
            _ => "Unknown"
        };
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
        RideModeString        = GetRideModeString(),
        LightMode             = _lightMode,
        FrontLightMode        = _frontLightMode,
        RearLightMode         = _rearLightMode,
        Rssi                  = _rssi,
        SimpleStopEnabled     = _simpleStopEnabled,
    };
}
