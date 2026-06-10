namespace OWCE.Contracts;

/// <summary>
/// Platform-agnostic interface for syncing telemetry to a paired smartwatch.
/// Implemented on iOS via WCSession, on Android via Wearable DataClient.
/// </summary>
public interface IWatchSyncService
{
    /// <summary>Whether a watch is currently paired and reachable.</summary>
    bool IsWatchReachable { get; }

    /// <summary>Initialize the watch communication session. Call once on app start.</summary>
    Task InitializeAsync();

    /// <summary>Push a telemetry snapshot to the watch.</summary>
    Task SyncAsync(WatchPayload payload, CancellationToken cancellationToken);
}
