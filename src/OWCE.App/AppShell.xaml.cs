using OWCE.Views.Pages;

namespace OWCE;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register all routes that are navigated to programmatically
        Routing.RegisterRoute(Routes.Board, typeof(BoardPage));
        Routing.RegisterRoute(Routes.BoardDetails, typeof(BoardDetailsPage));
        Routing.RegisterRoute(Routes.RideHistory, typeof(RideHistoryPage));
        Routing.RegisterRoute(Routes.AppSettings, typeof(AppSettingsPage));
    }

    /// <summary>
    /// Centralised route name constants. Use these everywhere instead of magic strings.
    /// </summary>
    public static class Routes
    {
        public const string BoardList = "BoardList";
        public const string Board = "Board";
        public const string BoardDetails = "BoardDetails";
        public const string RideHistory = "RideHistory";
        public const string AppSettings = "AppSettings";
    }
}
