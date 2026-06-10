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

        // ── Core Services ─────────────────────────────────────────────────────

        // BLE abstraction
        builder.Services.AddSingleton<IBLEService, BLEService>();

        // Board state processor (raw BLE bytes → typed BoardState snapshots)
        builder.Services.AddSingleton<IBoardStateService, BoardStateService>();

        // Handshake (strategy: V1/Plus/XR = none, Pint/GT = Gemini, GT-S = Polaris)
        builder.Services.AddSingleton<IHandshakeService, HandshakeService>();

        // Board connection orchestrator — registered against interface (not concrete type)
        // This ensures ViewModels depend on IBoardConnectionService, not the implementation.
        builder.Services.AddSingleton<IBoardConnectionService, BoardConnectionService>();

        // Ride recording, top speed tracking, and SQLite persistence
        builder.Services.AddSingleton<IRideService, RideService>();

        // Settings
        builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>();

        // IDispatcher — MAUI provides this as a built-in. Injecting it into services
        // instead of using Application.Current.Dispatcher enforces Rule 3 (no App.Current
        // outside of App.xaml.cs) and makes those services unit-testable.
        builder.Services.AddSingleton(sp =>
            sp.GetRequiredService<IApplication>().Dispatcher);

        // ── HTTP ──────────────────────────────────────────────────────────────
        // Named "owce" client with base address, timeout, and retry policy.
        // HandshakeService requests this client by name via IHttpClientFactory.
        builder.Services.AddHttpClient("owce", client =>
        {
            client.BaseAddress = new Uri("https://api.owce.app");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // ── Watch Sync (platform-specific) ────────────────────────────────────
        // IWatchSyncService is registered in platform-specific partial files:
        //   Platforms/iOS/MauiProgram.iOS.cs    → WCSession implementation
        //   Platforms/Android/MauiProgram.Android.cs → DataClient implementation
        // This keeps the shared MauiProgram.cs free of #if directives.
        RegisterPlatformServices(builder.Services);

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

    /// <summary>
    /// Registers platform-specific services. Implemented as partial methods in
    /// Platforms/iOS/MauiProgram.iOS.cs and Platforms/Android/MauiProgram.Android.cs.
    /// </summary>
    static partial void RegisterPlatformServices(IServiceCollection services);
}
