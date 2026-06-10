using OWCE.Contracts;
using OWCE.Platforms.iOS.Services;

namespace OWCE;

/// <summary>
/// iOS-specific DI registrations. This partial class is compiled only on iOS.
/// Registers IWatchSyncService with the WCSession-based implementation.
/// </summary>
public static partial class MauiProgram
{
    static partial void RegisterPlatformServices(IServiceCollection services)
    {
        services.AddSingleton<IWatchSyncService, WatchSyncService>();
    }
}
