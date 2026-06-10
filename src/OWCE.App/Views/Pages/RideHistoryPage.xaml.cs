using OWCE.ViewModels;
namespace OWCE.Views.Pages;
public partial class RideHistoryPage : ContentPage
{
    public RideHistoryPage(RideHistoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is RideHistoryViewModel vm)
            vm.LoadRidesCommand.Execute(null);
    }
}
