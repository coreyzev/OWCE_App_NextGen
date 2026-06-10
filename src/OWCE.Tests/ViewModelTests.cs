using FluentAssertions;
using Moq;
using OWCE.Contracts;
using OWCE.ViewModels;
using Xunit;

namespace OWCE.Tests;

/// <summary>
/// Unit tests for ViewModels.
/// All MAUI/UI dependencies are mocked so these run headlessly.
/// </summary>
public class AppSettingsViewModelTests
{
    private static (AppSettingsViewModel vm, Mock<IAppSettingsService> settingsMock) Create()
    {
        var mock = new Mock<IAppSettingsService>();
        mock.Setup(s => s.SpeedUnit).Returns(SpeedUnit.Mph);
        mock.Setup(s => s.TempUnit).Returns(TempUnit.Fahrenheit);
        mock.Setup(s => s.AutoStartRideRecording).Returns(false);
        var vm = new AppSettingsViewModel(mock.Object);
        return (vm, mock);
    }

    [Fact]
    public void SpeedUnit_DefaultsToMph()
    {
        var (vm, _) = Create();
        vm.SpeedUnit.Should().Be(SpeedUnit.Mph);
        vm.IsMetric.Should().BeFalse();
        vm.SpeedUnitDisplay.Should().Be("mph");
    }

    [Fact]
    public void ToggleSpeedUnit_SwitchesToKmH()
    {
        var (vm, mock) = Create();
        mock.SetupSet(s => s.SpeedUnit = SpeedUnit.KmH).Verifiable();

        vm.ToggleSpeedUnitCommand.Execute(null);

        vm.SpeedUnit.Should().Be(SpeedUnit.KmH);
        vm.IsMetric.Should().BeTrue();
        vm.SpeedUnitDisplay.Should().Be("km/h");
    }

    [Fact]
    public void ToggleTempUnit_SwitchesToCelsius()
    {
        var (vm, mock) = Create();
        mock.SetupSet(s => s.TempUnit = TempUnit.Celsius).Verifiable();

        vm.ToggleTempUnitCommand.Execute(null);

        vm.TempUnit.Should().Be(TempUnit.Celsius);
        vm.TempUnitDisplay.Should().Be("°C");
    }

    [Fact]
    public void SpeedAndTempUnits_AreIndependent()
    {
        // Verifies that toggling speed unit does not affect temp unit and vice versa
        var (vm, mock) = Create();

        vm.ToggleSpeedUnitCommand.Execute(null);
        vm.TempUnit.Should().Be(TempUnit.Fahrenheit); // unchanged

        vm.ToggleTempUnitCommand.Execute(null);
        vm.SpeedUnit.Should().Be(SpeedUnit.KmH); // unchanged
    }
}

public class RideItemViewModelTests
{
    [Fact]
    public void DistanceDisplay_ConvertsMilesToKm_WhenMetric()
    {
        var settings = new Mock<IAppSettingsService>();
        settings.Setup(s => s.SpeedUnit).Returns(SpeedUnit.KmH);

        var ride = new RideSession
        {
            DistanceMiles = 10f,
            TopSpeedMph = 15f,
            AvgSpeedMph = 10f,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddMinutes(30),
        };

        var vm = new RideItemViewModel(ride, settings.Object);

        vm.DistanceDisplay.Should().Contain("km");
        vm.TopSpeedDisplay.Should().Contain("km/h");
    }

    [Fact]
    public void DurationDisplay_ShowsMinutesAndSeconds_ForShortRide()
    {
        var settings = new Mock<IAppSettingsService>();
        settings.Setup(s => s.SpeedUnit).Returns(SpeedUnit.Mph);

        var ride = new RideSession
        {
            StartTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            EndTime   = new DateTime(2026, 1, 1, 12, 5, 30, DateTimeKind.Utc),
        };

        var vm = new RideItemViewModel(ride, settings.Object);
        vm.DurationDisplay.Should().Be("5m 30s");
    }

    [Fact]
    public void DurationDisplay_ShowsHoursAndMinutes_ForLongRide()
    {
        var settings = new Mock<IAppSettingsService>();
        settings.Setup(s => s.SpeedUnit).Returns(SpeedUnit.Mph);

        var ride = new RideSession
        {
            StartTime = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            EndTime   = new DateTime(2026, 1, 1, 11, 30, 0, DateTimeKind.Utc),
        };

        var vm = new RideItemViewModel(ride, settings.Object);
        vm.DurationDisplay.Should().Be("1h 30m");
    }
}
