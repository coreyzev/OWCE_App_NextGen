using Android.Gms.Wearable;
using OWCE.Contracts;

namespace OWCE.Platforms.Android.Services;

/// <summary>
/// Android phone-side Wearable DataClient implementation of IWatchSyncService.
/// Sends live telemetry to the paired Wear OS watch via DataClient.PutDataItem.
///
/// Uses the Wearable Data Layer API (Google Play Services Wearable).
/// The watch app reads the data item via a WearableListenerService.
///
/// Registered in DI only on Android via the platform-specific MauiProgram partial.
/// </summary>
public sealed class WatchSyncService : IWatchSyncService
{
    private const string WearDataPath = "/owce/telemetry";
    private bool _initialized;

    public bool IsWatchReachable => true; // DataClient is fire-and-forget; always attempt

    public Task InitializeAsync()
    {
        _initialized = true;
        System.Diagnostics.Debug.WriteLine("[OWCE Watch] Android WatchSyncService initialized.");
        return Task.CompletedTask;
    }

    public async Task SyncAsync(WatchPayload payload, CancellationToken cancellationToken)
    {
        if (!_initialized) return;

        var context = Android.App.Application.Context;
        var dataClient = WearableClass.GetDataClient(context);

        var request = PutDataMapRequest.Create(WearDataPath);
        var dataMap = request.DataMap;

        dataMap.PutFloat("speed",    payload.CurrentSpeedMph);
        dataMap.PutFloat("topSpeed", payload.TopSpeedMph);
        dataMap.PutInt("battery",    payload.BatteryPercent);
        dataMap.PutFloat("range",    payload.EstimatedRangeMiles);
        dataMap.PutString("unit",    payload.SpeedUnit == SpeedUnit.Mph ? "mph" : "km/h");
        dataMap.PutBoolean("riding", payload.IsRiding);
        // Timestamp ensures the data item is always treated as new
        dataMap.PutLong("ts", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        var putRequest = request.AsPutDataRequest();
        putRequest.SetUrgent(); // Deliver immediately, not batched

        try
        {
            await dataClient.PutDataItemAsync(putRequest);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OWCE Watch] DataClient error: {ex.Message}");
        }
    }
}
