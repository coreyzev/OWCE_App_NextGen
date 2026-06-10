using Android.Gms.Wearable;
using OWCE.Contracts;

namespace OWCE.Platforms.Android.Services;

/// <summary>
/// Android phone-side Wearable DataClient implementation of IWatchSyncService.
/// Sends live telemetry to the paired Wear OS watch via DataClient.PutDataItem.
///
/// DataClient is obtained once in InitializeAsync and cached — not recreated on
/// every SyncAsync call. (Fixes code review finding #9.)
///
/// Registered in DI only on Android via Platforms/Android/MauiProgram.Android.cs.
/// </summary>
public sealed class WatchSyncService : IWatchSyncService
{
    private const string WearDataPath = "/owce/telemetry";
    private IDataClient? _dataClient;

    // DataClient is fire-and-forget; if the watch is not connected the item
    // is queued and delivered when it reconnects. Always returns true.
    public bool IsWatchReachable => _dataClient != null;

    public Task InitializeAsync()
    {
        if (_dataClient != null) return Task.CompletedTask;

        var context = Android.App.Application.Context;
        _dataClient = WearableClass.GetDataClient(context);

        System.Diagnostics.Debug.WriteLine("[OWCE Watch] Android WatchSyncService initialized.");
        return Task.CompletedTask;
    }

    public async Task SyncAsync(WatchPayload payload, CancellationToken cancellationToken)
    {
        if (_dataClient is null) return;

        var request = PutDataMapRequest.Create(WearDataPath);
        var dataMap = request.DataMap;

        dataMap.PutFloat("speed",    payload.CurrentSpeedMph);
        dataMap.PutFloat("topSpeed", payload.TopSpeedMph);
        dataMap.PutInt("battery",    payload.BatteryPercent);
        dataMap.PutFloat("range",    payload.EstimatedRangeMiles);
        dataMap.PutString("unit",    payload.SpeedUnit == SpeedUnit.Mph ? "mph" : "km/h");
        dataMap.PutBoolean("riding", payload.IsRiding);
        // Timestamp ensures the data item is always treated as new (forces DATA_CHANGED)
        dataMap.PutLong("ts", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        var putRequest = request.AsPutDataRequest();
        putRequest.SetUrgent(); // Deliver immediately, not batched

        try
        {
            await _dataClient.PutDataItemAsync(putRequest);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OWCE Watch] DataClient error: {ex.Message}");
        }
    }
}
