using CommunityToolkit.Mvvm.ComponentModel;
using OWCE.Contracts;

namespace OWCE.ViewModels;

/// <summary>
/// ViewModel for the board details page (serial, firmware, hardware, odometer).
/// </summary>
public sealed partial class BoardDetailsViewModel : BaseViewModel
{
    private readonly IBoardStateService _boardState;

    [ObservableProperty] private string _serialNumber = string.Empty;
    [ObservableProperty] private int _firmwareRevision;
    [ObservableProperty] private int _hardwareRevision;
    [ObservableProperty] private float _lifetimeOdometerMiles;
    [ObservableProperty] private string _boardTypeName = string.Empty;

    public BoardDetailsViewModel(IBoardStateService boardState)
    {
        _boardState = boardState;
        Title = "Board Details";
        LoadFromState(_boardState.CurrentState);
    }

    private void LoadFromState(BoardState? state)
    {
        if (state is null) return;
        SerialNumber = state.SerialNumber;
        FirmwareRevision = state.FirmwareRevision;
        HardwareRevision = state.HardwareRevision;
        LifetimeOdometerMiles = state.LifetimeOdometerMiles;
        BoardTypeName = state.BoardType.ToString();
    }
}
