using OWCE.ViewModels;

namespace OWCE.Views.Pages;

public partial class BoardPage : ContentPage
{
    public BoardPage(BoardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
