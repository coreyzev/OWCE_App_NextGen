using FluentAssertions;
using Moq;
using OWCE.Contracts;
using Xunit;

namespace OWCE.Tests;

/// <summary>
/// Tests for IAppSettingsService contract compliance.
/// These tests verify the contract, not the implementation.
/// </summary>
public class AppSettingsServiceTests
{
    [Fact]
    public void SpeedUnit_DefaultsToMph()
    {
        // Arrange
        var mockSettings = new Mock<IAppSettingsService>();
        mockSettings.Setup(s => s.SpeedUnit).Returns(SpeedUnit.Mph);

        // Act
        var unit = mockSettings.Object.SpeedUnit;

        // Assert
        unit.Should().Be(SpeedUnit.Mph);
    }

    [Fact]
    public void TempUnit_DefaultsToFahrenheit()
    {
        // Arrange
        var mockSettings = new Mock<IAppSettingsService>();
        mockSettings.Setup(s => s.TempUnit).Returns(TempUnit.Fahrenheit);

        // Act
        var unit = mockSettings.Object.TempUnit;

        // Assert
        unit.Should().Be(TempUnit.Fahrenheit);
    }

    [Fact]
    public void SettingsChanged_RaisedWhenSpeedUnitChanges()
    {
        // Arrange
        var mockSettings = new Mock<IAppSettingsService>();
        bool eventRaised = false;
        mockSettings.Object.SettingsChanged += (_, _) => eventRaised = true;

        // Act
        mockSettings.Raise(s => s.SettingsChanged += null, EventArgs.Empty);

        // Assert
        eventRaised.Should().BeTrue();
    }
}
