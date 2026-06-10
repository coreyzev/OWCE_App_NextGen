using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OWCE.Contracts;

namespace OWCE.ViewModels;

/// <summary>
/// ViewModel for the ride history page.
/// Loads past rides from IRideService and exposes all-time top speed.
/// </summary>
public sealed partial class RideHistoryViewModel : BaseViewModel
{
    private readonly IRideService _rideService;
    private readonly IAppSettingsService _settings;

    [ObservableProperty] private float _allTimeTopSpeed;
    [ObservableProperty] private string _allTimeTopSpeedDisplay = "—";
    [ObservableProperty] private bool _hasRides;

    public ObservableCollection<RideItemViewModel> Rides { get; } = new();

    public RideHistoryViewModel(IRideService rideService, IAppSettingsService settings)
    {
        _rideService = rideService;
        _settings = settings;
        Title = "Ride History";

        // Reload when a new ride ends
        _rideService.RideEnded += (_, _) => _ = LoadRidesAsync();
    }

    [RelayCommand]
    private async Task LoadRidesAsync()
    {
        IsBusy = true;
        try
        {
            var rides = await _rideService.GetRideHistoryAsync();
            var topSpeed = await _rideService.GetAllTimeTopSpeedAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Rides.Clear();
                foreach (var ride in rides)
                    Rides.Add(new RideItemViewModel(ride, _settings));

                AllTimeTopSpeed = topSpeed;
                AllTimeTopSpeedDisplay = FormatSpeed(topSpeed);
                HasRides = Rides.Count > 0;
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load rides: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string FormatSpeed(float mph) => _settings.SpeedUnit == SpeedUnit.Mph
        ? $"{mph:F1} mph"
        : $"{mph * 1.60934f:F1} km/h";
}

/// <summary>Wrapper ViewModel for a single ride history list item.</summary>
public sealed class RideItemViewModel : ObservableObject
{
    private readonly IAppSettingsService _settings;
    public RideSession Ride { get; }

    public string DateDisplay => Ride.StartTime.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
    public string DurationDisplay
    {
        get
        {
            if (Ride.EndTime is null) return "In progress";
            var duration = Ride.EndTime.Value - Ride.StartTime;
            return duration.TotalHours >= 1
                ? $"{(int)duration.TotalHours}h {duration.Minutes}m"
                : $"{duration.Minutes}m {duration.Seconds}s";
        }
    }
    public string DistanceDisplay => _settings.SpeedUnit == SpeedUnit.Mph
        ? $"{Ride.DistanceMiles:F2} mi"
        : $"{Ride.DistanceMiles * 1.60934f:F2} km";
    public string TopSpeedDisplay => _settings.SpeedUnit == SpeedUnit.Mph
        ? $"{Ride.TopSpeedMph:F1} mph"
        : $"{Ride.TopSpeedMph * 1.60934f:F1} km/h";
    public string AvgSpeedDisplay => _settings.SpeedUnit == SpeedUnit.Mph
        ? $"{Ride.AvgSpeedMph:F1} mph"
        : $"{Ride.AvgSpeedMph * 1.60934f:F1} km/h";
    public string BoardTypeDisplay => Ride.BoardType.ToString();

    public RideItemViewModel(RideSession ride, IAppSettingsService settings)
    {
        Ride = ride;
        _settings = settings;
    }
}
