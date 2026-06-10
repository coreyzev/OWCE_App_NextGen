using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OWCE.Contracts;

namespace OWCE.ViewModels;

/// <summary>
/// ViewModel for the live ride dashboard (BoardPage).
/// Subscribes to IBoardStateService and IRideService.
/// Phase 4 will flesh out all observable properties.
/// </summary>
public sealed partial class BoardViewModel : BaseViewModel
{
    private readonly IBoardStateService _boardState;
    private readonly IRideService _rideService;
    private readonly IAppSettingsService _settings;

    // ── Live Telemetry ────────────────────────────────────────────────────────
    [ObservableProperty] private float _currentSpeed;
    [ObservableProperty] private float _topSpeed;
    [ObservableProperty] private int _batteryPercent;
    [ObservableProperty] private float _batteryVoltage;
    [ObservableProperty] private float _tripDistance;
    [ObservableProperty] private string _rideMode = string.Empty;
    [ObservableProperty] private bool _isRegen;
    [ObservableProperty] private float _controllerTemp;
    [ObservableProperty] private float _motorTemp;
    [ObservableProperty] private bool _lightMode;
    [ObservableProperty] private int _frontLightMode;
    [ObservableProperty] private int _rearLightMode;
    [ObservableProperty] private int _rssi;

    // ── Ride State ────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isRecording;

    public BoardViewModel(
        IBoardStateService boardState,
        IRideService rideService,
        IAppSettingsService settings)
    {
        _boardState = boardState;
        _rideService = rideService;
        _settings = settings;

        _boardState.StateUpdated += OnStateUpdated;
        Title = "Ride";
    }

    [RelayCommand]
    private async Task ToggleRideRecordingAsync()
    {
        if (_rideService.IsRecording)
            await _rideService.EndRideAsync();
        else
            await _rideService.StartRideAsync(
                _boardState.CurrentState?.SerialNumber ?? string.Empty,
                _boardState.CurrentState?.BoardType ?? OWBoardType.Unknown,
                CancellationToken.None);

        IsRecording = _rideService.IsRecording;
    }

    private void OnStateUpdated(object? sender, BoardState state)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentSpeed = _settings.SpeedUnit == SpeedUnit.Mph
                ? state.SpeedMph
                : state.SpeedMph * 1.60934f;

            TopSpeed = _rideService.TopSpeedThisRide;
            BatteryPercent = state.BatteryPercent;
            BatteryVoltage = state.BatteryVoltage;
            TripDistance = state.TripOdometerMiles;
            RideMode = state.RideModeString;
            IsRegen = state.IsRegen;
            ControllerTemp = _settings.TempUnit == TempUnit.Fahrenheit
                ? (state.ControllerTempC * 9f / 5f) + 32f
                : state.ControllerTempC;
            MotorTemp = _settings.TempUnit == TempUnit.Fahrenheit
                ? (state.MotorTempC * 9f / 5f) + 32f
                : state.MotorTempC;
            LightMode = state.LightMode;
            FrontLightMode = state.FrontLightMode;
            RearLightMode = state.RearLightMode;
            Rssi = state.Rssi;
        });
    }
}
