using OWCE.ViewModels;
namespace OWCE.Views.Pages;
public partial class BoardDetailsPage : ContentPage
{
    public BoardDetailsPage(BoardDetailsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
