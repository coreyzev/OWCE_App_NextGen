using Foundation;
using WatchConnectivity;
using OWCE.Contracts;

namespace OWCE.Platforms.iOS.Services;

/// <summary>
/// iOS phone-side WCSession implementation of IWatchSyncService.
/// Sends live telemetry to the paired Apple Watch via WCSession.SendMessage
/// (when watch is reachable) or UpdateApplicationContext (as fallback).
///
/// Registered in DI only on iOS via the platform-specific MauiProgram partial.
/// </summary>
public sealed class WatchSyncService : NSObject, IWatchSyncService, IWCSessionDelegate
{
    private WCSession? _session;
    private bool _initialized;

    public bool IsWatchReachable => _session?.Reachable ?? false;

    public Task InitializeAsync()
    {
        if (_initialized) return Task.CompletedTask;

        if (!WCSession.IsSupported)
        {
            System.Diagnostics.Debug.WriteLine("[OWCE Watch] WCSession not supported on this device.");
            return Task.CompletedTask;
        }

        _session = WCSession.DefaultSession;
        _session.Delegate = this;
        _session.ActivateSession();
        _initialized = true;

        System.Diagnostics.Debug.WriteLine("[OWCE Watch] WCSession activated.");
        return Task.CompletedTask;
    }

    public async Task SyncAsync(WatchPayload payload, CancellationToken cancellationToken)
    {
        if (_session is null || _session.ActivationState != WCSessionActivationState.Activated)
            return;

        var dict = PayloadToDictionary(payload);

        if (_session.Reachable)
        {
            // Watch is awake and reachable: use SendMessage for immediate delivery
            var tcs = new TaskCompletionSource<bool>();
            _session.SendMessage(dict,
                replyHandler: _ => tcs.TrySetResult(true),
                errorHandler: err =>
                {
                    System.Diagnostics.Debug.WriteLine($"[OWCE Watch] SendMessage error: {err}");
                    tcs.TrySetResult(false);
                });
            await tcs.Task.WaitAsync(cancellationToken);
        }
        else
        {
            // Watch is not reachable: update context so it gets data when it wakes
            NSError? error;
            _session.UpdateApplicationContext(dict, out error);
            if (error != null)
                System.Diagnostics.Debug.WriteLine($"[OWCE Watch] UpdateApplicationContext error: {error}");
        }
    }

    private static NSDictionary PayloadToDictionary(WatchPayload payload)
    {
        return new NSDictionary(
            new NSString("speed"),   new NSNumber(payload.CurrentSpeedMph),
            new NSString("topSpeed"), new NSNumber(payload.TopSpeedMph),
            new NSString("battery"), new NSNumber(payload.BatteryPercent),
            new NSString("range"),   new NSNumber(payload.EstimatedRangeMiles),
            new NSString("unit"),    new NSString(payload.SpeedUnit == SpeedUnit.Mph ? "mph" : "km/h"),
            new NSString("riding"),  new NSNumber(payload.IsRiding)
        );
    }

    // ── WCSessionDelegate ─────────────────────────────────────────────────────

    [Export("session:activationDidCompleteWithState:error:")]
    public void ActivationDidComplete(WCSession session, WCSessionActivationState activationState, NSError? error)
    {
        System.Diagnostics.Debug.WriteLine($"[OWCE Watch] WCSession activation: {activationState}, error: {error}");
    }

    [Export("sessionDidBecomeInactive:")]
    public void DidBecomeInactive(WCSession session)
    {
        System.Diagnostics.Debug.WriteLine("[OWCE Watch] WCSession became inactive.");
    }

    [Export("sessionDidDeactivate:")]
    public void DidDeactivate(WCSession session)
    {
        // Re-activate for Apple Watch switching
        session.ActivateSession();
    }
}
