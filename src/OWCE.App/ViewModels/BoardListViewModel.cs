using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OWCE.Contracts;
using OWCE.Messages;

namespace OWCE.ViewModels;

/// <summary>
/// ViewModel for the board discovery / scan page.
/// Drives IBLEService scanning and exposes discovered boards to the UI.
/// </summary>
public sealed partial class BoardListViewModel : BaseViewModel
{
    private readonly IBLEService _bleService;
    private CancellationTokenSource? _scanCts;

    [ObservableProperty]
    private bool _isScanning;

    public ObservableCollection<DiscoveredBoardViewModel> DiscoveredBoards { get; } = new();

    public BoardListViewModel(IBLEService bleService)
    {
        _bleService = bleService;
        _bleService.DeviceDiscovered += OnDeviceDiscovered;
        Title = "Find Your Board";
    }

    [RelayCommand]
    private async Task StartScanAsync()
    {
        if (IsScanning) return;

        DiscoveredBoards.Clear();
        _scanCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        IsScanning = true;

        try
        {
            await _bleService.StartScanAsync(_scanCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Scan timed out — normal
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task StopScanAsync()
    {
        _scanCts?.Cancel();
        await _bleService.StopScanAsync();
        IsScanning = false;
    }

    [RelayCommand]
    private void SelectBoard(DiscoveredBoardViewModel boardVm)
    {
        _scanCts?.Cancel();
        WeakReferenceMessenger.Default.Send(new ConnectToBoardMessage(boardVm.Board));
    }

    private void OnDeviceDiscovered(object? sender, DiscoveredBoard board)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!DiscoveredBoards.Any(b => b.Board.DeviceId == board.DeviceId))
            {
                DiscoveredBoards.Add(new DiscoveredBoardViewModel(board));
            }
        });
    }
}

/// <summary>
/// Lightweight wrapper ViewModel for a single discovered board item in the list.
/// </summary>
public sealed class DiscoveredBoardViewModel : ObservableObject
{
    public DiscoveredBoard Board { get; }
    public string Name => Board.Name;
    public int Rssi => Board.Rssi;

    public DiscoveredBoardViewModel(DiscoveredBoard board)
    {
        Board = board;
    }
}
