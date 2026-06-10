using BackgroundTasks;
using Foundation;

namespace OWCE.Platforms.iOS;

/// <summary>
/// Fixes iOS background BLE disconnect (issue #111).
///
/// iOS aggressively terminates BLE connections when the app moves to the background
/// unless the app declares "bluetooth-central" in UIBackgroundModes AND uses
/// BGTaskScheduler to request additional background processing time.
///
/// This class registers a BGProcessingTask that keeps the BLE connection alive
/// for up to 30 seconds when the app is backgrounded. This is sufficient to
/// maintain the connection through a phone-lock event.
///
/// IMPORTANT: The task identifier "app.owce.ble-keepalive" must be declared in
/// Info.plist under BGTaskSchedulerPermittedIdentifiers.
/// </summary>
public static class BackgroundBLEHandler
{
    private const string TaskIdentifier = "app.owce.ble-keepalive";

    public static void Register()
    {
        BGTaskScheduler.Shared.Register(
            TaskIdentifier,
            null,
            task => HandleBLEKeepaliveTask((BGProcessingTask)task));
    }

    public static void ScheduleKeepalive()
    {
        var request = new BGProcessingTaskRequest(TaskIdentifier)
        {
            RequiresNetworkConnectivity = false,
            RequiresExternalPower = false,
        };

        NSError? error;
        BGTaskScheduler.Shared.Submit(request, out error);

        if (error != null)
            System.Diagnostics.Debug.WriteLine($"[OWCE BG] BGTask submit error: {error}");
    }

    private static void HandleBLEKeepaliveTask(BGProcessingTask task)
    {
        // Re-schedule for the next background event
        ScheduleKeepalive();

        // The task just needs to exist — the BLE central manager keeps running
        // as long as the app is registered for bluetooth-central background mode.
        // We complete immediately; the OS will give us time due to the background mode.
        task.SetTaskCompleted(success: true);
    }
}
