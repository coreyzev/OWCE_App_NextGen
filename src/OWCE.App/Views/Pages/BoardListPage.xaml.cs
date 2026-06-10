using OWCE.ViewModels;

namespace OWCE.Views.Pages;

public partial class BoardListPage : ContentPage
{
    public BoardListPage(BoardListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
