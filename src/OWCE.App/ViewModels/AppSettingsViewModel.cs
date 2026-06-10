using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using OWCE.Contracts;
using OWCE.Messages;

namespace OWCE.ViewModels;

/// <summary>
/// ViewModel for the app settings page.
/// Separate toggles for speed unit and temperature unit (per issue #115).
/// </summary>
public sealed partial class AppSettingsViewModel : BaseViewModel
{
    private readonly IAppSettingsService _settings;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMetric))]
    private SpeedUnit _speedUnit;

    [ObservableProperty]
    private TempUnit _tempUnit;

    [ObservableProperty]
    private bool _autoStartRideRecording;

    public bool IsMetric => SpeedUnit == SpeedUnit.KmH;

    public AppSettingsViewModel(IAppSettingsService settings)
    {
        _settings = settings;
        Title = "Settings";

        // Load current values
        _speedUnit = settings.SpeedUnit;
        _tempUnit = settings.TempUnit;
        _autoStartRideRecording = settings.AutoStartRideRecording;
    }

    partial void OnSpeedUnitChanged(SpeedUnit value)
    {
        _settings.SpeedUnit = value;
        WeakReferenceMessenger.Default.Send(new UnitsChangedMessage(value, _settings.TempUnit));
    }

    partial void OnTempUnitChanged(TempUnit value)
    {
        _settings.TempUnit = value;
        WeakReferenceMessenger.Default.Send(new UnitsChangedMessage(_settings.SpeedUnit, value));
    }

    partial void OnAutoStartRideRecordingChanged(bool value)
    {
        _settings.AutoStartRideRecording = value;
    }
}
