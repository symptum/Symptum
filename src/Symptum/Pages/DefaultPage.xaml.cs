using Symptum.Core.Management.Navigation;
using Symptum.Navigation;

namespace Symptum.Pages;

public sealed partial class DefaultPage : NavigablePage
{
    public DefaultPage()
    {
        InitializeComponent();
    }

    #region Properties

    public static readonly DependencyProperty NavigableResourceProperty = DependencyProperty.Register(
        nameof(NavigableResource),
        typeof(NavigableResource),
        typeof(DefaultPage),
        new(null));

    public NavigableResource NavigableResource
    {
        get => (NavigableResource)GetValue(NavigableResourceProperty);
        set => SetValue(NavigableResourceProperty, value);
    }

    #endregion

    protected override void OnNavigableChanged(INavigable? navigable)
    {
        if (navigable is NavigableResource resource)
        {
            NavigableResource = resource;
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        NavigationManager.Navigate((sender as Button).Tag as INavigable);
    }
}
