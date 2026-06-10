using OWCE.Contracts;

namespace OWCE.Services;

/// <summary>
/// Persists user preferences using MAUI's Preferences API.
/// This is the only class that reads/writes Preferences — no other class should call Preferences directly.
/// </summary>
public sealed class AppSettingsService : IAppSettingsService
{
    private const string SpeedUnitKey = "pref_speed_unit";
    private const string TempUnitKey = "pref_temp_unit";
    private const string AutoStartRideKey = "pref_auto_start_ride";

    public SpeedUnit SpeedUnit
    {
        get => (SpeedUnit)Preferences.Default.Get(SpeedUnitKey, (int)SpeedUnit.Mph);
        set
        {
            Preferences.Default.Set(SpeedUnitKey, (int)value);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public TempUnit TempUnit
    {
        get => (TempUnit)Preferences.Default.Get(TempUnitKey, (int)TempUnit.Fahrenheit);
        set
        {
            Preferences.Default.Set(TempUnitKey, (int)value);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool AutoStartRideRecording
    {
        get => Preferences.Default.Get(AutoStartRideKey, false);
        set
        {
            Preferences.Default.Set(AutoStartRideKey, value);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? SettingsChanged;
}
