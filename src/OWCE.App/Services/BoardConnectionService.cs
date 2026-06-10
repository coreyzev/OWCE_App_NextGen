using CommunityToolkit.Mvvm.Messaging;
using OWCE.Contracts;
using OWCE.Messages;

namespace OWCE.Services;

/// <summary>
/// Orchestrates the full board connection lifecycle:
/// 1. BLE connect
/// 2. Read hardware/firmware revision to identify board type
/// 3. Perform handshake (if required)
/// 4. Subscribe to all telemetry characteristics
/// 5. Start pushing state updates to IBoardStateService
///
/// Implements IBoardConnectionService so ViewModels depend on the interface,
/// not this concrete class. (Fixes code review finding #2.)
///
/// UUID constants come from BLEUuids in OWCE.Contracts, not from BoardStateService.
/// (Fixes code review finding #3.)
/// </summary>
public sealed class BoardConnectionService : IBoardConnectionService, IDisposable
{
    private readonly IBLEService _bleService;
    private readonly IBoardStateService _boardState;
    private readonly IHandshakeService _handshake;
    private bool _disposed;

    // Characteristics to subscribe to for live telemetry (ordered by priority)
    private static readonly string[] TelemetrySubscriptions =
    [
        BLEUuids.Rpm,
        BLEUuids.BatteryPercent,
        BLEUuids.BatteryVoltage,
        BLEUuids.Temperature,
        BLEUuids.BatteryTemperature,
        BLEUuids.TripOdometer,
        BLEUuids.CurrentAmps,
        BLEUuids.TripAmpHours,
        BLEUuids.TripRegenAmpHours,
        BLEUuids.RideMode,
        BLEUuids.LightMode,
        BLEUuids.LightsFront,
        BLEUuids.LightsBack,
        BLEUuids.SimpleStop,
    ];

    // Characteristics to read once on connect (static board info)
    private static readonly string[] StaticReads =
    [
        BLEUuids.HardwareRevision,
        BLEUuids.FirmwareRevision,
        BLEUuids.SerialNumber,
        BLEUuids.LifetimeOdometer,
    ];

    public BoardConnectionService(
        IBLEService bleService,
        IBoardStateService boardState,
        IHandshakeService handshake)
    {
        _bleService = bleService;
        _boardState = boardState;
        _handshake = handshake;

        _bleService.ValueUpdated += OnValueUpdated;
        _bleService.ConnectionStateChanged += OnConnectionStateChanged;
    }

    /// <summary>
    /// Connects to the board identified by deviceId and performs the full
    /// connect → identify → handshake → subscribe sequence.
    /// </summary>
    public async Task ConnectAsync(string deviceId, CancellationToken cancellationToken)
    {
        bool connected = await _bleService.ConnectAsync(deviceId, cancellationToken);
        if (!connected)
            throw new InvalidOperationException($"Failed to connect to device {deviceId}.");

        // Read static characteristics to identify the board type
        foreach (var uuid in StaticReads)
        {
            try
            {
                var data = await _bleService.ReadCharacteristicAsync(uuid, cancellationToken);
                _boardState.ProcessValueUpdate(uuid, data);
            }
            catch (Exception ex)
            {
                // Non-fatal: some boards may not expose all characteristics
                System.Diagnostics.Debug.WriteLine($"[OWCE] Could not read {uuid}: {ex.Message}");
            }
        }

        var boardType = _boardState.CurrentState?.BoardType ?? OWBoardType.Unknown;
        var firmwareRevision = _boardState.CurrentState?.FirmwareRevision ?? 0;

        // Perform handshake if required (keep-alive is self-managed inside HandshakeService)
        await _handshake.PerformHandshakeAsync(boardType, firmwareRevision, cancellationToken);

        // Subscribe to live telemetry
        foreach (var uuid in TelemetrySubscriptions)
        {
            try
            {
                await _bleService.SubscribeToCharacteristicAsync(uuid, cancellationToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OWCE] Could not subscribe to {uuid}: {ex.Message}");
            }
        }

        // Notify the rest of the app that a board is connected
        if (_boardState.CurrentState is not null)
            WeakReferenceMessenger.Default.Send(new BoardConnectedMessage(_boardState.CurrentState));
    }

    public async Task DisconnectAsync()
    {
        await _bleService.DisconnectAsync();
        _boardState.Reset();
        WeakReferenceMessenger.Default.Send(new BoardDisconnectedMessage("User disconnected."));
    }

    private void OnValueUpdated(object? sender, (string CharacteristicUuid, byte[] Data) e)
    {
        _boardState.ProcessValueUpdate(e.CharacteristicUuid, e.Data);
    }

    private void OnConnectionStateChanged(object? sender, bool isConnected)
    {
        if (!isConnected)
        {
            _boardState.Reset();
            WeakReferenceMessenger.Default.Send(new BoardDisconnectedMessage("Connection lost."));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _bleService.ValueUpdated -= OnValueUpdated;
        _bleService.ConnectionStateChanged -= OnConnectionStateChanged;
        _disposed = true;
    }
}
