using FluentAssertions;
using Moq;
using OWCE.Contracts;
using OWCE.Services;
using Xunit;

namespace OWCE.Tests;

/// <summary>
/// Unit tests for HandshakeService.
/// Verifies that the correct handshake strategy is selected per board type.
///
/// UUID constants now reference BLEUuids (OWCE.Contracts), not BoardStateService.
/// KeepAliveAsync is no longer on the public interface — the keep-alive timer is
/// self-managed internally. Tests verify the initial token write; the timer
/// behaviour is an integration concern.
/// </summary>
public class HandshakeServiceTests
{
    private static (HandshakeService service, Mock<IBLEService> bleMock) CreateService()
    {
        var bleMock = new Mock<IBLEService>();
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                   .Returns(new HttpClient());

        var dispatcherMock = new Mock<IDispatcher>();
        // Dispatcher.Dispatch executes synchronously in tests
        dispatcherMock
            .Setup(d => d.Dispatch(It.IsAny<Action>()))
            .Callback<Action>(a => a());
        var timerMock = new Mock<IDispatcherTimer>();
        dispatcherMock.Setup(d => d.CreateTimer()).Returns(timerMock.Object);

        var svc = new HandshakeService(bleMock.Object, httpFactory.Object, dispatcherMock.Object);
        return (svc, bleMock);
    }

    [Theory]
    [InlineData(OWBoardType.V1)]
    [InlineData(OWBoardType.Plus)]
    [InlineData(OWBoardType.XR)]
    public async Task OlderBoards_NoHandshakeRequired_NoBLEWriteOccurs(OWBoardType boardType)
    {
        var (svc, bleMock) = CreateService();

        await svc.PerformHandshakeAsync(boardType, 0, CancellationToken.None);

        bleMock.Verify(b => b.WriteCharacteristicAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GTS_WritesStaticToken_ToSerialWriteUuid()
    {
        var (svc, bleMock) = CreateService();
        byte[]? writtenData = null;

        bleMock.Setup(b => b.WriteCharacteristicAsync(
                BLEUuids.SerialWrite,
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], CancellationToken>((_, data, _) => writtenData = data)
            .Returns(Task.CompletedTask);

        await svc.PerformHandshakeAsync(OWBoardType.GTS, 0, CancellationToken.None);

        bleMock.Verify(b => b.WriteCharacteristicAsync(
            BLEUuids.SerialWrite,
            It.IsAny<byte[]>(),
            It.IsAny<CancellationToken>()), Times.Once);

        writtenData.Should().NotBeNull();
        writtenData!.Length.Should().Be(20);
        writtenData[0].Should().Be(0x43); // 'C' — first byte of Polaris token
        writtenData[1].Should().Be(0x52); // 'R'
        writtenData[2].Should().Be(0x58); // 'X'
    }

    [Fact]
    public async Task GTS_StartsKeepAliveTimer_AfterHandshake()
    {
        // Verify that the internal keep-alive timer is started after GT-S handshake.
        // KeepAliveAsync is NOT on the public interface — this test verifies the
        // timer is created and started via the injected IDispatcher.
        var bleMock = new Mock<IBLEService>();
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                   .Returns(new HttpClient());

        var dispatcherMock = new Mock<IDispatcher>();
        var timerMock = new Mock<IDispatcherTimer>();
        dispatcherMock.Setup(d => d.Dispatch(It.IsAny<Action>()))
                      .Callback<Action>(a => a());
        dispatcherMock.Setup(d => d.CreateTimer()).Returns(timerMock.Object);

        bleMock.Setup(b => b.WriteCharacteristicAsync(
                It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = new HandshakeService(bleMock.Object, httpFactory.Object, dispatcherMock.Object);
        await svc.PerformHandshakeAsync(OWBoardType.GTS, 0, CancellationToken.None);

        // Timer should have been created and started
        dispatcherMock.Verify(d => d.CreateTimer(), Times.Once);
        timerMock.VerifySet(t => t.Interval = TimeSpan.FromSeconds(15));
        timerMock.Verify(t => t.Start(), Times.Once);
    }

    [Fact]
    public async Task UnknownBoardType_ThrowsNotSupportedException()
    {
        var (svc, _) = CreateService();

        Func<Task> act = () => svc.PerformHandshakeAsync(OWBoardType.Unknown, 0, CancellationToken.None);

        await act.Should().ThrowAsync<NotSupportedException>();
    }
}
