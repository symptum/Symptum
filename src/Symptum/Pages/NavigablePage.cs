using Symptum.Core.Management.Navigation;

namespace Symptum.Pages;

public abstract class NavigablePage : Page
{
    public NavigablePage()
    { }

    #region Properties

    public static readonly DependencyProperty NavigableProperty =
        DependencyProperty.Register(
            nameof(Navigable),
            typeof(INavigable),
            typeof(NavigablePage),
            new PropertyMetadata(null, OnNavigableChanged));

    private static void OnNavigableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigablePage navigablePage)
        {
            navigablePage.OnNavigableChanged(e.NewValue as INavigable);
        }
    }

    public INavigable? Navigable
    {
        get => (INavigable?)GetValue(NavigableProperty);
        set => SetValue(NavigableProperty, value);
    }

    #endregion

    protected virtual void OnNavigableChanged(INavigable? navigable)
    {

    }
}
