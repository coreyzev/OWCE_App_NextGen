using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using OWCE.Contracts;
using OWCE.Messages;

namespace OWCE.ViewModels;

/// <summary>
/// ViewModel for the board details page.
/// Shows static board info: serial, firmware, hardware, odometer.
/// Updates when board state changes.
/// </summary>
public sealed partial class BoardDetailsViewModel : BaseViewModel,
    IRecipient<BoardConnectedMessage>
{
    private readonly IBoardStateService _boardState;
    private readonly IAppSettingsService _settings;

    [ObservableProperty] private string _serialNumber = "—";
    [ObservableProperty] private string _firmwareRevision = "—";
    [ObservableProperty] private string _hardwareRevision = "—";
    [ObservableProperty] private string _lifetimeOdometer = "—";
    [ObservableProperty] private string _boardTypeName = "—";

    public BoardDetailsViewModel(IBoardStateService boardState, IAppSettingsService settings)
    {
        _boardState = boardState;
        _settings = settings;
        Title = "Board Details";

        WeakReferenceMessenger.Default.Register<BoardConnectedMessage>(this);
        LoadFromState(_boardState.CurrentState);
    }

    public void Receive(BoardConnectedMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() => LoadFromState(message.Value));
    }

    private void LoadFromState(BoardState? state)
    {
        if (state is null) return;

        SerialNumber = state.SerialNumber.Length > 0 ? state.SerialNumber : "—";
        FirmwareRevision = state.FirmwareRevision > 0 ? state.FirmwareRevision.ToString() : "—";
        HardwareRevision = state.HardwareRevision > 0 ? state.HardwareRevision.ToString() : "—";
        BoardTypeName = state.BoardType.ToString();

        LifetimeOdometer = _settings.SpeedUnit == SpeedUnit.Mph
            ? $"{state.LifetimeOdometerMiles:F1} mi"
            : $"{state.LifetimeOdometerMiles * 1.60934f:F1} km";
    }
}
