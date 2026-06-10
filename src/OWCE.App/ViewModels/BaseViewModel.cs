using CommunityToolkit.Mvvm.ComponentModel;

namespace OWCE.ViewModels;

/// <summary>
/// Base class for all ViewModels. Provides IsBusy, Title, and ErrorMessage.
/// All ViewModels must inherit from this class.
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public bool IsNotBusy => !IsBusy;
}
