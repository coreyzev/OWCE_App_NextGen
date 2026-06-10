using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OWCE.Contracts;
using OWCE.Messages;
using OWCE.Services;

namespace OWCE.ViewModels;

/// <summary>
/// ViewModel for the board discovery and connection page.
/// Drives BLE scanning via IBLEService and delegates connection to BoardConnectionService.
/// </summary>
public sealed partial class BoardListViewModel : BaseViewModel,
    IRecipient<BoardConnectedMessage>,
    IRecipient<HandshakeErrorMessage>
{
    private readonly IBLEService _bleService;
    private readonly BoardConnectionService _connectionService;
    private CancellationTokenSource? _scanCts;
    private CancellationTokenSource? _connectCts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScanButtonText))]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private string _statusMessage = "Tap Scan to find your board.";

    public string ScanButtonText => IsScanning ? "Stop" : "Scan";

    public ObservableCollection<DiscoveredBoardViewModel> DiscoveredBoards { get; } = new();

    public BoardListViewModel(IBLEService bleService, BoardConnectionService connectionService)
    {
        _bleService = bleService;
        _connectionService = connectionService;
        _bleService.DeviceDiscovered += OnDeviceDiscovered;
        Title = "Find Your Board";

        WeakReferenceMessenger.Default.Register<BoardConnectedMessage>(this);
        WeakReferenceMessenger.Default.Register<HandshakeErrorMessage>(this);
    }

    [RelayCommand]
    private async Task ToggleScanAsync()
    {
        if (IsScanning)
        {
            await StopScanAsync();
        }
        else
        {
            await StartScanAsync();
        }
    }

    private async Task StartScanAsync()
    {
        DiscoveredBoards.Clear();
        ErrorMessage = string.Empty;
        StatusMessage = "Scanning for Onewheels…";
        _scanCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        IsScanning = true;

        try
        {
            await _bleService.StartScanAsync(_scanCts.Token);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = DiscoveredBoards.Count > 0
                ? $"Found {DiscoveredBoards.Count} board(s). Tap to connect."
                : "Scan complete. No boards found.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Scan failed: {ex.Message}";
            StatusMessage = "Scan failed.";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private async Task StopScanAsync()
    {
        _scanCts?.Cancel();
        await _bleService.StopScanAsync();
        IsScanning = false;
        StatusMessage = DiscoveredBoards.Count > 0
            ? $"Found {DiscoveredBoards.Count} board(s). Tap to connect."
            : "Scan stopped.";
    }

    [RelayCommand]
    private async Task ConnectToBoardAsync(DiscoveredBoardViewModel boardVm)
    {
        if (IsConnecting) return;

        IsConnecting = true;
        StatusMessage = $"Connecting to {boardVm.Name}…";
        _connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        try
        {
            await _connectionService.ConnectAsync(boardVm.Board.DeviceId, _connectCts.Token);
            // Navigation is handled by the BoardConnectedMessage recipient in AppShell
        }
        catch (HandshakeException ex)
        {
            ErrorMessage = $"Handshake failed: {ex.Message}";
            StatusMessage = "Connection failed.";
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Connection timed out.";
            StatusMessage = "Connection timed out.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Connection error: {ex.Message}";
            StatusMessage = "Connection failed.";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private void OnDeviceDiscovered(object? sender, DiscoveredBoard board)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!DiscoveredBoards.Any(b => b.Board.DeviceId == board.DeviceId))
            {
                DiscoveredBoards.Add(new DiscoveredBoardViewModel(board));
                StatusMessage = $"Found {DiscoveredBoards.Count} board(s). Tap to connect.";
            }
        });
    }

    public void Receive(BoardConnectedMessage message)
    {
        // Navigation to BoardPage is handled by AppShell which listens to this message
        StatusMessage = "Connected!";
    }

    public void Receive(HandshakeErrorMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ErrorMessage = message.Value;
            StatusMessage = "Handshake failed.";
            IsConnecting = false;
        });
    }
}

/// <summary>Lightweight ViewModel wrapper for a single discovered board list item.</summary>
public sealed class DiscoveredBoardViewModel : ObservableObject
{
    public DiscoveredBoard Board { get; }
    public string Name => Board.Name;
    public int Rssi => Board.Rssi;
    public string RssiDisplay => $"{Board.Rssi} dBm";

    public DiscoveredBoardViewModel(DiscoveredBoard board) => Board = board;
}
