using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using OWCE.Contracts;
using OWCE.Services;
using OWCE.ViewModels;
using OWCE.Views.Pages;

namespace OWCE;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("SairaExtraCondensed-Regular.ttf", "SairaRegular");
                fonts.AddFont("SairaExtraCondensed-Bold.ttf", "SairaBold");
                fonts.AddFont("SairaExtraCondensed-SemiBold.ttf", "SairaSemiBold");
            });

        // ── Services ──────────────────────────────────────────────────────────
        // BLE
        builder.Services.AddSingleton<IBLEService, BLEService>();

        // Board state (processes raw BLE bytes into typed properties)
        builder.Services.AddSingleton<IBoardStateService, BoardStateService>();

        // Handshake (strategy pattern: V1/Plus/XR = none, Pint/GT = Gemini, GT-S = Polaris)
        builder.Services.AddSingleton<IHandshakeService, HandshakeService>();

        // Board connection orchestrator
        builder.Services.AddSingleton<BoardConnectionService>();

        // Ride recording and history
        builder.Services.AddSingleton<IRideService, RideService>();

        // Watch sync (platform-specific implementations registered in Platforms/)
        // builder.Services.AddSingleton<IWatchSyncService, WatchSyncService>();

        // Settings
        builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>();

        // HTTP
        builder.Services.AddHttpClient();

        // ── ViewModels ────────────────────────────────────────────────────────
        builder.Services.AddTransient<BoardListViewModel>();
        builder.Services.AddTransient<BoardViewModel>();
        builder.Services.AddTransient<RideHistoryViewModel>();
        builder.Services.AddTransient<AppSettingsViewModel>();
        builder.Services.AddTransient<BoardDetailsViewModel>();

        // ── Pages ─────────────────────────────────────────────────────────────
        builder.Services.AddTransient<BoardListPage>();
        builder.Services.AddTransient<BoardPage>();
        builder.Services.AddTransient<RideHistoryPage>();
        builder.Services.AddTransient<AppSettingsPage>();
        builder.Services.AddTransient<BoardDetailsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
