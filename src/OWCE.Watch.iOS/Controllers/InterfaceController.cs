using Foundation;
using WatchKit;
using WatchConnectivity;

namespace OWCE.Watch.iOS.Controllers;

/// <summary>
/// Main watch face controller for the OWCE Apple Watch app.
/// Displays: current speed, top speed, battery %, estimated range.
///
/// Receives data from the phone via WCSession.
/// Updates the UI on the main thread via InvokeOnMainThread.
/// </summary>
[Register("InterfaceController")]
public partial class InterfaceController : WKInterfaceController, IWCSessionDelegate
{
    // ── Outlets (wired in Interface.storyboard) ───────────────────────────────
    [Outlet] WKInterfaceLabel SpeedLabel { get; set; } = null!;
    [Outlet] WKInterfaceLabel SpeedUnitLabel { get; set; } = null!;
    [Outlet] WKInterfaceLabel TopSpeedLabel { get; set; } = null!;
    [Outlet] WKInterfaceLabel BatteryLabel { get; set; } = null!;
    [Outlet] WKInterfaceLabel RangeLabel { get; set; } = null!;
    [Outlet] WKInterfaceGroup SpeedGroup { get; set; } = null!;

    private WCSession? _session;

    public override void Awake(NSObject context)
    {
        base.Awake(context);
        SetTitle("OWCE");
        UpdateDisplay(speed: 0, topSpeed: 0, battery: 0, range: 0, unit: "MPH", isRiding: false);
    }

    public override void WillActivate()
    {
        base.WillActivate();

        if (WCSession.IsSupported)
        {
            _session = WCSession.DefaultSession;
            _session.Delegate = this;
            _session.ActivateSession();
        }
    }

    public override void DidDeactivate()
    {
        base.DidDeactivate();
    }

    // ── WCSessionDelegate ─────────────────────────────────────────────────────

    [Export("session:activationDidCompleteWithState:error:")]
    public void ActivationDidComplete(WCSession session, WCSessionActivationState activationState, NSError? error) { }

    [Export("session:didReceiveMessage:")]
    public void DidReceiveMessage(WCSession session, NSDictionary<NSString, NSObject> message)
    {
        ParseAndUpdate(message);
    }

    [Export("session:didReceiveApplicationContext:")]
    public void DidReceiveApplicationContext(WCSession session, NSDictionary<NSString, NSObject> applicationContext)
    {
        ParseAndUpdate(applicationContext);
    }

    private void ParseAndUpdate(NSDictionary<NSString, NSObject> dict)
    {
        float speed    = GetFloat(dict, "speed");
        float topSpeed = GetFloat(dict, "topSpeed");
        int battery    = (int)GetFloat(dict, "battery");
        float range    = GetFloat(dict, "range");
        string unit    = GetString(dict, "unit", "MPH").ToUpperInvariant();
        bool isRiding  = GetBool(dict, "riding");

        InvokeOnMainThread(() =>
            UpdateDisplay(speed, topSpeed, battery, range, unit, isRiding));
    }

    private void UpdateDisplay(float speed, float topSpeed, int battery, float range, string unit, bool isRiding)
    {
        SpeedLabel.SetText($"{speed:F1}");
        SpeedUnitLabel.SetText(unit);
        TopSpeedLabel.SetText($"TOP {topSpeed:F1}");
        BatteryLabel.SetText($"{battery}%");
        RangeLabel.SetText($"{range:F1} mi");

        // Dim the speed group when not riding
        SpeedGroup.SetAlpha(isRiding ? 1.0f : 0.5f);

        // Color-code battery
        var batteryColor = battery switch
        {
            > 50 => UIKit.UIColor.FromRGBA(0.18f, 0.80f, 0.44f, 1f),  // Green
            > 20 => UIKit.UIColor.FromRGBA(0.95f, 0.61f, 0.07f, 1f),  // Yellow
            _    => UIKit.UIColor.FromRGBA(0.91f, 0.30f, 0.24f, 1f),  // Red
        };
        BatteryLabel.SetTextColor(batteryColor);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static float GetFloat(NSDictionary<NSString, NSObject> dict, string key)
        => dict.TryGetValue(new NSString(key), out var val) && val is NSNumber n ? n.FloatValue : 0f;

    private static int GetInt(NSDictionary<NSString, NSObject> dict, string key)
        => dict.TryGetValue(new NSString(key), out var val) && val is NSNumber n ? n.Int32Value : 0;

    private static bool GetBool(NSDictionary<NSString, NSObject> dict, string key)
        => dict.TryGetValue(new NSString(key), out var val) && val is NSNumber n && n.BoolValue;

    private static string GetString(NSDictionary<NSString, NSObject> dict, string key, string fallback = "")
        => dict.TryGetValue(new NSString(key), out var val) && val is NSString s ? s.ToString() : fallback;
}
