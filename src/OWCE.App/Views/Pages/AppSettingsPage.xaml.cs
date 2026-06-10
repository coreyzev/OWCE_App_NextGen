using OWCE.ViewModels;

namespace OWCE.Views.Pages;

public partial class AppSettingsPage : ContentPage
{
    public AppSettingsPage(AppSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
