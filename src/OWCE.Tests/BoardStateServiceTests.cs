using FluentAssertions;
using OWCE.Contracts;
using OWCE.Services;
using Xunit;

namespace OWCE.Tests;

/// <summary>
/// Unit tests for BoardStateService.
/// These tests verify the core byte-parsing logic without any BLE or UI dependencies.
/// </summary>
public class BoardStateServiceTests
{
    private static BoardStateService CreateService() => new();

    private static byte[] ToBytes(ushort value) => [(byte)(value >> 8), (byte)(value & 0xFF)];

    [Theory]
    [InlineData(1000,    OWBoardType.V1)]
    [InlineData(3500,    OWBoardType.Plus)]
    [InlineData(4200,    OWBoardType.XR)]
    [InlineData(5100,    OWBoardType.Pint)]
    [InlineData(6050,    OWBoardType.GT)]
    [InlineData(7200,    OWBoardType.PintX)]
    [InlineData(8100,    OWBoardType.GTS)]
    public void HardwareRevision_SetsCorrectBoardType(int hwRev, OWBoardType expectedType)
    {
        var svc = CreateService();
        svc.ProcessValueUpdate(BoardStateService.HardwareRevisionUuid, ToBytes((ushort)hwRev));
        svc.CurrentState!.BoardType.Should().Be(expectedType);
    }

    [Fact]
    public void RpmUpdate_SetsSpeedMph_ForXR()
    {
        var svc = CreateService();
        // Set board type to XR first
        svc.ProcessValueUpdate(BoardStateService.HardwareRevisionUuid, ToBytes(4200));

        // 300 RPM on XR (11.5" wheel, circumference 0.9177m)
        // Expected: (300/60) * 0.9177 * 2.23694 = ~3.43 mph
        svc.ProcessValueUpdate(BoardStateService.RpmUuid, ToBytes(300));

        svc.CurrentState!.SpeedMph.Should().BeApproximately(3.43f, 0.1f);
    }

    [Fact]
    public void RpmUpdate_SetsSpeedMph_ForPint()
    {
        var svc = CreateService();
        svc.ProcessValueUpdate(BoardStateService.HardwareRevisionUuid, ToBytes(5100));

        // 300 RPM on Pint (10.5" wheel, circumference 0.8379m)
        // Expected: (300/60) * 0.8379 * 2.23694 = ~3.13 mph
        svc.ProcessValueUpdate(BoardStateService.RpmUuid, ToBytes(300));

        svc.CurrentState!.SpeedMph.Should().BeApproximately(3.13f, 0.1f);
    }

    [Fact]
    public void BatteryVoltage_IsScaledByPointOne()
    {
        var svc = CreateService();
        // Raw value 630 → 63.0V
        svc.ProcessValueUpdate(BoardStateService.BatteryVoltageUuid, ToBytes(630));
        svc.CurrentState!.BatteryVoltage.Should().BeApproximately(63.0f, 0.01f);
    }

    [Fact]
    public void ControllerAndMotorTemp_ParsedFromTwoBytes()
    {
        var svc = CreateService();
        // First byte = controller temp (35°C), second byte = motor temp (42°C)
        svc.ProcessValueUpdate(BoardStateService.TemperatureUuid, [35, 42]);
        svc.CurrentState!.ControllerTempC.Should().Be(35f);
        svc.CurrentState!.MotorTempC.Should().Be(42f);
    }

    [Fact]
    public void CurrentAmps_NegativeValue_SetsIsRegenTrue()
    {
        var svc = CreateService();
        svc.ProcessValueUpdate(BoardStateService.HardwareRevisionUuid, ToBytes(4200)); // XR

        // Negative current (regenerative braking) — two's complement: -100 = 65436 as ushort
        ushort negativeRaw = (ushort)(65536 - 100);
        svc.ProcessValueUpdate(BoardStateService.CurrentAmpsUuid, ToBytes(negativeRaw));

        svc.CurrentState!.IsRegen.Should().BeTrue();
        svc.CurrentState!.CurrentAmps.Should().BeLessThan(0);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        var svc = CreateService();
        svc.ProcessValueUpdate(BoardStateService.HardwareRevisionUuid, ToBytes(4200));
        svc.ProcessValueUpdate(BoardStateService.BatteryPercentUuid, ToBytes(80));

        svc.Reset();

        svc.CurrentState.Should().BeNull();
    }

    [Fact]
    public void StateUpdated_EventRaised_OnEveryUpdate()
    {
        var svc = CreateService();
        int eventCount = 0;
        svc.StateUpdated += (_, _) => eventCount++;

        svc.ProcessValueUpdate(BoardStateService.BatteryPercentUuid, ToBytes(75));
        svc.ProcessValueUpdate(BoardStateService.RpmUuid, ToBytes(200));

        eventCount.Should().Be(2);
    }

    [Theory]
    [InlineData(4, "Sequoia")]
    [InlineData(5, "Cruz")]
    [InlineData(8, "Delirium")]
    public void RideMode_XR_ReturnsCorrectString(int mode, string expected)
    {
        var svc = CreateService();
        svc.ProcessValueUpdate(BoardStateService.HardwareRevisionUuid, ToBytes(4200)); // XR
        svc.ProcessValueUpdate(BoardStateService.RideModeUuid, ToBytes((ushort)mode));
        svc.CurrentState!.RideModeString.Should().Be(expected);
    }

    [Theory]
    [InlineData(3, "Bay")]
    [InlineData(8, "Apex")]
    public void RideMode_GT_ReturnsCorrectString(int mode, string expected)
    {
        var svc = CreateService();
        svc.ProcessValueUpdate(BoardStateService.HardwareRevisionUuid, ToBytes(6050)); // GT
        svc.ProcessValueUpdate(BoardStateService.RideModeUuid, ToBytes((ushort)mode));
        svc.CurrentState!.RideModeString.Should().Be(expected);
    }
}
