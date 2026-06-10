using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OWCE.Contracts;
using OWCE.Messages;
using OWCE.Services;

namespace OWCE.ViewModels;

/// <summary>
/// ViewModel for the live ride dashboard (BoardPage).
///
/// Subscribes to IBoardStateService.StateUpdated and IRideService events.
/// Pushes WatchPayload to IWatchSyncService at 1 Hz via a throttle timer.
/// All unit conversions (mph → km/h, °C → °F) happen here, not in the service layer.
/// </summary>
public sealed partial class BoardViewModel : BaseViewModel,
    IRecipient<BoardDisconnectedMessage>,
    IRecipient<UnitsChangedMessage>
{
    private readonly IBoardStateService _boardState;
    private readonly IRideService _rideService;
    private readonly IAppSettingsService _settings;
    private readonly BoardConnectionService _connectionService;
    private readonly IWatchSyncService? _watchSync;
    private IDispatcherTimer? _watchSyncTimer;

    // ── Live Telemetry ────────────────────────────────────────────────────────
    [ObservableProperty] private float _currentSpeed;
    [ObservableProperty] private float _topSpeedThisRide;
    [ObservableProperty] private int _batteryPercent;
    [ObservableProperty] private float _batteryVoltage;
    [ObservableProperty] private float _tripDistanceMiles;
    [ObservableProperty] private float _lifetimeOdometerMiles;
    [ObservableProperty] private string _rideMode = string.Empty;
    [ObservableProperty] private bool _isRegen;
    [ObservableProperty] private float _currentAmps;
    [ObservableProperty] private float _controllerTemp;
    [ObservableProperty] private float _motorTemp;
    [ObservableProperty] private bool _lightMode;
    [ObservableProperty] private int _frontLightMode;
    [ObservableProperty] private int _rearLightMode;
    [ObservableProperty] private int _rssi;
    [ObservableProperty] private string _boardName = string.Empty;
    [ObservableProperty] private string _speedUnit = "MPH";
    [ObservableProperty] private string _tempUnit = "°F";

    // ── Ride State ────────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RecordButtonText))]
    private bool _isRecording;

    public string RecordButtonText => IsRecording ? "End Ride" : "Start Ride";

    public BoardViewModel(
        IBoardStateService boardState,
        IRideService rideService,
        IAppSettingsService settings,
        BoardConnectionService connectionService,
        IWatchSyncService? watchSync = null)
    {
        _boardState = boardState;
        _rideService = rideService;
        _settings = settings;
        _connectionService = connectionService;
        _watchSync = watchSync;

        _boardState.StateUpdated += OnStateUpdated;
        _rideService.RideEnded += OnRideEnded;

        UpdateUnitLabels();
        Title = "Ride";

        WeakReferenceMessenger.Default.Register<BoardDisconnectedMessage>(this);
        WeakReferenceMessenger.Default.Register<UnitsChangedMessage>(this);

        // Initialize watch sync
        if (_watchSync != null)
        {
            _ = _watchSync.InitializeAsync();
            StartWatchSyncTimer();
        }

        // Auto-start ride recording if setting is enabled
        if (_settings.AutoStartRideRecording)
        {
            _ = StartRideRecordingAsync();
        }
    }

    [RelayCommand]
    private async Task ToggleRideRecordingAsync()
    {
        if (_rideService.IsRecording)
            await _rideService.EndRideAsync();
        else
            await StartRideRecordingAsync();

        IsRecording = _rideService.IsRecording;
    }

    private async Task StartRideRecordingAsync()
    {
        var state = _boardState.CurrentState;
        if (state is null) return;

        await _rideService.StartRideAsync(
            state.SerialNumber,
            state.BoardType,
            CancellationToken.None);
        IsRecording = true;
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        if (_rideService.IsRecording)
            await _rideService.EndRideAsync();

        await _connectionService.DisconnectAsync();
    }

    [RelayCommand]
    private async Task SetLightModeAsync(bool enabled)
    {
        // Write light mode to the board via BLE
        // The BLE write is handled through BoardConnectionService → IBLEService
        // Light mode: 0 = off, 1 = on
        // TODO: Phase 5 — wire up to IBLEService.WriteCharacteristicAsync
        await Task.CompletedTask;
    }

    private void OnStateUpdated(object? sender, BoardState state)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentSpeed = _settings.SpeedUnit == SpeedUnit.Mph
                ? state.SpeedMph
                : state.SpeedMph * 1.60934f;

            TopSpeedThisRide = _settings.SpeedUnit == SpeedUnit.Mph
                ? _rideService.TopSpeedThisRide
                : _rideService.TopSpeedThisRide * 1.60934f;

            BatteryPercent = state.BatteryPercent;
            BatteryVoltage = state.BatteryVoltage;
            TripDistanceMiles = state.TripOdometerMiles;
            LifetimeOdometerMiles = state.LifetimeOdometerMiles;
            RideMode = state.RideModeString;
            IsRegen = state.IsRegen;
            CurrentAmps = state.CurrentAmps;

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
            BoardName = $"Onewheel {state.BoardType} #{state.SerialNumber}";
        });
    }

    private void OnRideEnded(object? sender, RideSession session)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsRecording = false;
        });
    }

    public void Receive(BoardDisconnectedMessage message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _watchSyncTimer?.Stop();
            if (_rideService.IsRecording)
                await _rideService.EndRideAsync();
            await Shell.Current.GoToAsync("..");
        });
    }

    public void Receive(UnitsChangedMessage message)
    {
        UpdateUnitLabels();
        // Trigger a re-render by re-processing the current state
        if (_boardState.CurrentState is not null)
            OnStateUpdated(this, _boardState.CurrentState);
    }

    private void UpdateUnitLabels()
    {
        SpeedUnit = _settings.SpeedUnit == Contracts.SpeedUnit.Mph ? "MPH" : "KM/H";
        TempUnit = _settings.TempUnit == Contracts.TempUnit.Fahrenheit ? "°F" : "°C";
    }

    private void StartWatchSyncTimer()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _watchSyncTimer = Application.Current!.Dispatcher.CreateTimer();
            _watchSyncTimer.Interval = TimeSpan.FromSeconds(1);
            _watchSyncTimer.Tick += async (_, _) => await SyncWatchAsync();
            _watchSyncTimer.Start();
        });
    }

    private async Task SyncWatchAsync()
    {
        if (_watchSync is null || _boardState.CurrentState is null) return;

        var payload = new WatchPayload
        {
            CurrentSpeedMph    = _boardState.CurrentState.SpeedMph,
            TopSpeedMph        = _rideService.TopSpeedThisRide,
            BatteryPercent     = _boardState.CurrentState.BatteryPercent,
            EstimatedRangeMiles = EstimateRange(_boardState.CurrentState),
            SpeedUnit          = _settings.SpeedUnit,
            IsRiding           = _rideService.IsRecording,
        };

        try
        {
            await _watchSync.SyncAsync(payload, CancellationToken.None);
        }
        catch
        {
            // Watch sync is non-critical — swallow errors silently
        }
    }

    /// <summary>
    /// Simple linear range estimate based on battery percentage and board type.
    /// A more accurate model would use trip amp-hours and current voltage.
    /// </summary>
    private static float EstimateRange(BoardState state)
    {
        float maxRangeMiles = state.BoardType switch
        {
            OWBoardType.XR    => 18f,
            OWBoardType.GT    => 20f,
            OWBoardType.GTS   => 20f,
            OWBoardType.PintX => 12f,
            OWBoardType.Pint  => 8f,
            OWBoardType.Plus  => 7f,
            OWBoardType.V1    => 6f,
            _                 => 10f,
        };
        return maxRangeMiles * (state.BatteryPercent / 100f);
    }
}
