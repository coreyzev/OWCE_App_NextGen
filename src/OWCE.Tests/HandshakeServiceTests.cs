using FluentAssertions;
using Moq;
using OWCE.Contracts;
using OWCE.Services;
using Xunit;

namespace OWCE.Tests;

/// <summary>
/// Unit tests for HandshakeService.
/// Verifies that the correct handshake strategy is selected per board type.
/// </summary>
public class HandshakeServiceTests
{
    private static (HandshakeService service, Mock<IBLEService> bleMock) CreateService()
    {
        var bleMock = new Mock<IBLEService>();
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                   .Returns(new HttpClient());

        var svc = new HandshakeService(bleMock.Object, httpFactory.Object);
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
                BoardStateService.SerialWriteUuid,
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], CancellationToken>((_, data, _) => writtenData = data)
            .Returns(Task.CompletedTask);

        await svc.PerformHandshakeAsync(OWBoardType.GTS, 0, CancellationToken.None);

        bleMock.Verify(b => b.WriteCharacteristicAsync(
            BoardStateService.SerialWriteUuid,
            It.IsAny<byte[]>(),
            It.IsAny<CancellationToken>()), Times.Once);

        writtenData.Should().NotBeNull();
        writtenData!.Length.Should().Be(20);
        writtenData[0].Should().Be(0x43); // 'C' — first byte of Polaris token
        writtenData[1].Should().Be(0x52); // 'R'
        writtenData[2].Should().Be(0x58); // 'X'
    }

    [Fact]
    public async Task KeepAlive_GTS_WritesTokenAgain()
    {
        var (svc, bleMock) = CreateService();
        bleMock.Setup(b => b.WriteCharacteristicAsync(
                It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // First: perform handshake
        await svc.PerformHandshakeAsync(OWBoardType.GTS, 0, CancellationToken.None);

        // Then: keep alive
        await svc.KeepAliveAsync(CancellationToken.None);

        // Should have been called twice: once for handshake, once for keep-alive
        bleMock.Verify(b => b.WriteCharacteristicAsync(
            BoardStateService.SerialWriteUuid,
            It.IsAny<byte[]>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UnknownBoardType_ThrowsNotSupportedException()
    {
        var (svc, _) = CreateService();

        Func<Task> act = () => svc.PerformHandshakeAsync(OWBoardType.Unknown, 0, CancellationToken.None);

        await act.Should().ThrowAsync<NotSupportedException>();
    }
}
