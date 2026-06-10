using System.Net.Http.Json;
using OWCE.Contracts;

namespace OWCE.Services;

/// <summary>
/// Implements the BLE handshake authentication protocol for all Onewheel board types.
///
/// Strategy pattern: each board type uses a different handshake sequence.
/// - V1 / Plus / XR (Gemini): No handshake required.
/// - Pint / Pint X / GT: OWCE API-assisted challenge-response (Rewheel patch required for GT).
/// - GT-S (Polaris 6215): Static 20-byte token + 15-second keep-alive.
///
/// Keep-alive is fully self-managed internally via IDispatcher (injected).
/// KeepAliveAsync is NOT on IHandshakeService — callers must not call it externally.
/// See ADR-002 for full context.
/// </summary>
public sealed class HandshakeService : IHandshakeService, IDisposable
{
    private readonly IBLEService _bleService;
    private readonly HttpClient _httpClient;
    private readonly IDispatcher _dispatcher;
    private IDispatcherTimer? _keepAliveTimer;
    private bool _disposed;

    // GT-S Polaris 6215 static token (community-documented, issue #121)
    // This is a 20-byte token written to SerialWrite UUID to unlock the board.
    private static readonly byte[] GtsStaticToken =
    [
        0x43, 0x52, 0x58, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00
    ];

    public HandshakeService(
        IBLEService bleService,
        IHttpClientFactory httpClientFactory,
        IDispatcher dispatcher)
    {
        _bleService = bleService;
        _httpClient = httpClientFactory.CreateClient("owce");
        _dispatcher = dispatcher;
    }

    public async Task PerformHandshakeAsync(
        OWBoardType boardType,
        int firmwareRevision,
        CancellationToken cancellationToken)
    {
        switch (boardType)
        {
            case OWBoardType.V1:
            case OWBoardType.Plus:
            case OWBoardType.XR:
                // No handshake required for older boards
                return;

            case OWBoardType.Pint:
            case OWBoardType.PintX:
                await PerformGeminiHandshakeAsync(cancellationToken);
                break;

            case OWBoardType.GT:
                // GT requires Rewheel patch. We attempt the handshake but
                // surface a clear error if it fails so the user knows to use Rewheel.
                await PerformGeminiHandshakeAsync(cancellationToken);
                break;

            case OWBoardType.GTS:
                await PerformPolarisHandshakeAsync(cancellationToken);
                break;

            default:
                throw new NotSupportedException(
                    $"Board type {boardType} does not have a known handshake strategy.");
        }
    }

    /// <summary>
    /// Gemini handshake: read a challenge from the board, send it to the OWCE API,
    /// and write the response back to the board to unlock BLE telemetry.
    /// Used by Pint, Pint X, and GT (with Rewheel patch).
    /// </summary>
    private async Task PerformGeminiHandshakeAsync(CancellationToken cancellationToken)
    {
        // Step 1: Read the challenge bytes from SerialRead UUID
        var challenge = await _bleService.ReadCharacteristicAsync(
            BLEUuids.SerialRead, cancellationToken);

        if (challenge is null || challenge.Length == 0)
            throw new HandshakeException("Board did not provide a handshake challenge.");

        // Step 2: Send challenge to OWCE API to get the response token
        var requestBody = new { challenge = Convert.ToHexString(challenge) };
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(
                "/handshake", requestBody, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new HandshakeException($"OWCE API request failed: {ex.Message}", ex);
        }

        if (!response.IsSuccessStatusCode)
            throw new HandshakeException($"OWCE API returned {(int)response.StatusCode}.");

        var result = await response.Content.ReadFromJsonAsync<HandshakeResponse>(
            cancellationToken: cancellationToken);
        if (result?.Token is null)
            throw new HandshakeException("OWCE API returned an empty token.");

        // Step 3: Write the response token back to the board
        var tokenBytes = Convert.FromHexString(result.Token);
        await _bleService.WriteCharacteristicAsync(
            BLEUuids.SerialWrite, tokenBytes, cancellationToken);
    }

    /// <summary>
    /// Polaris 6215 handshake (GT-S): write the static 20-byte token to SerialWrite UUID.
    /// Starts a self-managed 15-second keep-alive timer via the injected IDispatcher.
    /// Keep-alive is internal — callers must not invoke it externally.
    /// </summary>
    private async Task PerformPolarisHandshakeAsync(CancellationToken cancellationToken)
    {
        await _bleService.WriteCharacteristicAsync(
            BLEUuids.SerialWrite,
            GtsStaticToken,
            cancellationToken);

        // Start the 15-second keep-alive timer via injected IDispatcher (not Application.Current)
        _dispatcher.Dispatch(() =>
        {
            _keepAliveTimer?.Stop();
            _keepAliveTimer = _dispatcher.CreateTimer();
            _keepAliveTimer.Interval = TimeSpan.FromSeconds(15);
            _keepAliveTimer.Tick += async (_, _) =>
            {
                try
                {
                    await _bleService.WriteCharacteristicAsync(
                        BLEUuids.SerialWrite,
                        GtsStaticToken,
                        CancellationToken.None);
                }
                catch
                {
                    // Swallow — board may have disconnected between ticks
                }
            };
            _keepAliveTimer.Start();
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _keepAliveTimer?.Stop();
        _disposed = true;
    }

    private sealed record HandshakeResponse(string? Token);
}

/// <summary>
/// Thrown when the BLE handshake fails for any reason.
/// </summary>
public sealed class HandshakeException : Exception
{
    public HandshakeException(string message) : base(message) { }
    public HandshakeException(string message, Exception inner) : base(message, inner) { }
}
