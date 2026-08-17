using Symptum.Core.Management.Navigation;
using Symptum.Navigation;

namespace Symptum.ViewModels;

public class MainViewModel
{
    public static MainViewModel Instance { get; } = new();


    #region Properties

    public ICommand NavigateCommand { get; } = new RelayCommand<INavigable>(NavigationManager.Navigate);

    public ICommand NavigateToUriCommand { get; } = new AsyncRelayCommand<Uri>(NavigationManager.NavigateAsync);

    #endregion

    private MainViewModel()
    { }
}