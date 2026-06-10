using FluentAssertions;
using Moq;
using OWCE.Contracts;
using Xunit;

namespace OWCE.Tests;

/// <summary>
/// Unit tests for watch sync payload construction.
/// The actual WCSession/DataClient calls are platform-specific and tested
/// via integration tests on device. These tests verify the payload model.
/// </summary>
public class WatchPayloadTests
{
    [Fact]
    public void WatchPayload_DefaultValues_AreZero()
    {
        var payload = new WatchPayload();
        payload.CurrentSpeedMph.Should().Be(0f);
        payload.TopSpeedMph.Should().Be(0f);
        payload.BatteryPercent.Should().Be(0);
        payload.EstimatedRangeMiles.Should().Be(0f);
        payload.IsRiding.Should().BeFalse();
    }

    [Fact]
    public void WatchPayload_SpeedUnit_DefaultsToMph()
    {
        var payload = new WatchPayload();
        payload.SpeedUnit.Should().Be(SpeedUnit.Mph);
    }

    [Fact]
    public void WatchPayload_AllFields_CanBeSet()
    {
        var payload = new WatchPayload
        {
            CurrentSpeedMph     = 18.5f,
            TopSpeedMph         = 22.1f,
            BatteryPercent      = 75,
            EstimatedRangeMiles = 12.3f,
            SpeedUnit           = SpeedUnit.KmH,
            IsRiding            = true,
        };

        payload.CurrentSpeedMph.Should().Be(18.5f);
        payload.TopSpeedMph.Should().Be(22.1f);
        payload.BatteryPercent.Should().Be(75);
        payload.EstimatedRangeMiles.Should().Be(12.3f);
        payload.SpeedUnit.Should().Be(SpeedUnit.KmH);
        payload.IsRiding.Should().BeTrue();
    }

    [Fact]
    public async Task WatchSyncService_Mock_SyncAsync_IsCalled()
    {
        var mockSync = new Mock<IWatchSyncService>();
        mockSync.Setup(s => s.SyncAsync(It.IsAny<WatchPayload>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var payload = new WatchPayload { CurrentSpeedMph = 15f, BatteryPercent = 80 };
        await mockSync.Object.SyncAsync(payload, CancellationToken.None);

        mockSync.Verify(s => s.SyncAsync(
            It.Is<WatchPayload>(p => p.CurrentSpeedMph == 15f && p.BatteryPercent == 80),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
