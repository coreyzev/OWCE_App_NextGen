using Android.App;
using Android.Runtime;

namespace OWCE.Platforms.Android;

/// <summary>
/// Android Application entry point.
/// Handles Android 13+ (API 33) BLE permission changes (issue #97).
///
/// Android 12+ split BLE permissions into granular permissions:
/// - BLUETOOTH_SCAN (replaces BLUETOOTH_ADMIN for scanning)
/// - BLUETOOTH_CONNECT (required for connecting and reading characteristics)
///
/// These are declared in AndroidManifest.xml. The runtime permission request
/// is handled in BLEService.cs before scanning begins.
/// </summary>
[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
