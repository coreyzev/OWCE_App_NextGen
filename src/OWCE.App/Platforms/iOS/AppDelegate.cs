using Foundation;
using UIKit;

namespace OWCE.Platforms.iOS;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        // Register the BGTaskScheduler task for BLE keepalive
        BackgroundBLEHandler.Register();
        return base.FinishedLaunching(application, launchOptions);
    }

    public override void DidEnterBackground(UIApplication application)
    {
        base.DidEnterBackground(application);
        // Schedule a background processing task to keep BLE alive
        BackgroundBLEHandler.ScheduleKeepalive();
    }
}
