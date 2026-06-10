using OWCE.ViewModels;

namespace OWCE.Views.Pages;

public partial class RideHistoryPage : ContentPage
{
    public RideHistoryPage(RideHistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
