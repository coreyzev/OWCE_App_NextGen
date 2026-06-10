using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    public ObservableCollection<RideSession> Rides { get; } = new();

    public RideHistoryViewModel(IRideService rideService, IAppSettingsService settings)
    {
        _rideService = rideService;
        _settings = settings;
        Title = "Ride History";
    }

    [RelayCommand]
    private async Task LoadRidesAsync()
    {
        IsBusy = true;
        try
        {
            var rides = await _rideService.GetRideHistoryAsync();
            Rides.Clear();
            foreach (var ride in rides.OrderByDescending(r => r.StartTime))
                Rides.Add(ride);

            AllTimeTopSpeed = await _rideService.GetAllTimeTopSpeedAsync();
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
}
