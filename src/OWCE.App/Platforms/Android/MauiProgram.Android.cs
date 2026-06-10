using OWCE.Contracts;
using OWCE.Platforms.Android.Services;

namespace OWCE;

/// <summary>
/// Android-specific DI registrations. This partial class is compiled only on Android.
/// Registers IWatchSyncService with the Wearable DataClient-based implementation.
/// </summary>
public static partial class MauiProgram
{
    static partial void RegisterPlatformServices(IServiceCollection services)
    {
        services.AddSingleton<IWatchSyncService, WatchSyncService>();
    }
}
