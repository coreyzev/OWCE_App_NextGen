using FluentAssertions;
using OWCE.Contracts;
using OWCE.Services;
using Xunit;

namespace OWCE.Tests;

/// <summary>
/// Unit tests for BoardStateService.
/// These tests verify the core byte-parsing logic without any BLE or UI dependencies.
///
/// All UUID constants reference BLEUuids (OWCE.Contracts), not BoardStateService.
/// (Fixes code review finding #3.)
/// </summary>
public class BoardStateServiceTests
{
    private static BoardStateService CreateService() => new();

    private static byte[] ToBytes(ushort value) => [(byte)(value >> 8), (byte)(value & 0xFF)];

    // Hardware revision ranges per board type (from BoardStateService detection logic):
    //   V1:    1–999
    //   Plus:  2000–2999
    //   XR:    3000–3999
    //   Pint:  4000–4999
    //   PintX: 5000–5999
    //   GT:    6000–7999
    //   GTS:   8000–8999

    [Theory]
    [InlineData(500,  OWBoardType.V1)]
    [InlineData(2500, OWBoardType.Plus)]
    [InlineData(3500, OWBoardType.XR)]
    [InlineData(4200, OWBoardType.Pint)]
    [InlineData(5100, OWBoardType.PintX)]
    [InlineData(6500, OWBoardType.GT)]
    [InlineData(8100, OWBoardType.GTS)]
    public void HardwareRevision_SetsCorrectBoardType(int hwRev, OWBoardType expectedType)
    {
        var svc = CreateService();
        svc.ProcessValueUpdate(BLEUuids.HardwareRevision, ToBytes((ushort)hwRev));
        svc.CurrentState!.BoardType.Should().Be(expectedType);
    }

    [Fact]
    public void RpmUpdate_SetsSpeedMph_ForXR()
    {
        var svc = CreateService();
        // Set board type to XR first (hardware revision 3500)
        svc.ProcessValueUpdate(BLEUuids.HardwareRevision, ToBytes(3500));

        // 300 RPM on XR (11.5" wheel, circumference 0.9177m)
        // Expected: 300 × 0.9177 × 60 / 1609.34 ≈ 10.27 mph
        svc.ProcessValueUpdate(BLEUuids.Rpm, ToBytes(300));

        svc.CurrentState!.SpeedMph.Should().BeApproximately(10.27f, 0.1f);
    }

    [Fact]
    public void RpmUpdate_SetsSpeedMph_ForPint()
    {
        var svc = CreateService();
        // Set board type to Pint first (hardware revision 4200)
        svc.ProcessValueUpdate(BLEUuids.HardwareRevision, ToBytes(4200));

        // 300 RPM on Pint (10.5" wheel, circumference 0.8379m)
        // Expected: 300 × 0.8379 × 60 / 1609.34 ≈ 9.37 mph
        svc.ProcessValueUpdate(BLEUuids.Rpm, ToBytes(300));

        svc.CurrentState!.SpeedMph.Should().BeApproximately(9.37f, 0.1f);
    }

    [Fact]
    public void BatteryVoltage_IsScaledByPointOne()
    {
        var svc = CreateService();
        // Raw value 630 → 63.0V
        svc.ProcessValueUpdate(BLEUuids.BatteryVoltage, ToBytes(630));
        svc.CurrentState!.BatteryVoltage.Should().BeApproximately(63.0f, 0.01f);
    }

    [Fact]
    public void CurrentAmps_NegativeValue_SetsIsRegenTrue()
    {
        var svc = CreateService();
        svc.ProcessValueUpdate(BLEUuids.HardwareRevision, ToBytes(3500)); // XR

        // Negative current (regenerative braking) — two's complement: -100 = 65436 as ushort
        ushort negativeRaw = (ushort)(65536 - 100);
        svc.ProcessValueUpdate(BLEUuids.CurrentAmps, ToBytes(negativeRaw));

        svc.CurrentState!.IsRegen.Should().BeTrue();
        svc.CurrentState!.CurrentAmps.Should().BeLessThan(0);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        var svc = CreateService();
        svc.ProcessValueUpdate(BLEUuids.HardwareRevision, ToBytes(3500));
        svc.ProcessValueUpdate(BLEUuids.BatteryPercent, new byte[] { 80 });

        svc.Reset();

        svc.CurrentState.Should().BeNull();
    }

    [Fact]
    public void StateUpdated_EventRaised_OnEveryUpdate()
    {
        var svc = CreateService();
        int eventCount = 0;
        svc.StateUpdated += (_, _) => eventCount++;

        svc.ProcessValueUpdate(BLEUuids.BatteryPercent, new byte[] { 75 });
        svc.ProcessValueUpdate(BLEUuids.Rpm, ToBytes(200));

        eventCount.Should().Be(2);
    }

    [Theory]
    [InlineData(1, "Classic")]
    [InlineData(5, "Delirium")]
    public void RideMode_XR_ReturnsCorrectString(int mode, string expected)
    {
        var svc = CreateService();
        svc.ProcessValueUpdate(BLEUuids.HardwareRevision, ToBytes(3500)); // XR
        svc.ProcessValueUpdate(BLEUuids.RideMode, new byte[] { (byte)mode });
        svc.CurrentState!.RideModeString.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "Sequoia")]
    [InlineData(5, "Delirium")]
    public void RideMode_GT_ReturnsCorrectString(int mode, string expected)
    {
        var svc = CreateService();
        svc.ProcessValueUpdate(BLEUuids.HardwareRevision, ToBytes(6500)); // GT
        svc.ProcessValueUpdate(BLEUuids.RideMode, new byte[] { (byte)mode });
        svc.CurrentState!.RideModeString.Should().Be(expected);
    }
}
