using Microsoft.UI.Xaml.Media.Animation;
using Symptum.Navigation;
using Windows.UI.Core;

namespace Symptum;

public sealed partial class MainPage : Page
{
    private NavigationManager navigationManager;

    public MainPage()
    {
        InitializeComponent();
        navigationManager = NavigationManager.GetNavigationManager();
        navigationManager.NavigationRequested += NavigationManager_NavigationRequested;

        NavView_Navigate("home", new EntranceNavigationTransitionInfo());

        //SystemNavigationManager.GetForCurrentView().BackRequested += SystemNavigationManager_BackRequested;
    }

    private void NavigationManager_NavigationRequested(object? sender, NavigationRequestedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Path))
        {
            NavView_Navigate(e.Path, new EntranceNavigationTransitionInfo());
        }
    }


    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem != null)
        {
            var navItemTag = args.SelectedItemContainer?.Tag?.ToString();
            NavView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
        }
    }

    private void NavView_Navigate(string? navItemTag, NavigationTransitionInfo info)
    {
        NavigationInfo? navInfo = navigationManager.GetNavigationInfoForPath(navItemTag);
        Type? pageType = navInfo?.PageType;

        if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
        {
            navigationManager.SetCurrentNavigationInfo(navInfo);
            ContentFrame.Navigate(pageType, null, info);
        }
    }

    private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        BackRequested();
    }

    private void SystemNavigationManager_BackRequested(object? sender, BackRequestedEventArgs e)
    {
        e.Handled = BackRequested();
    }

    private bool BackRequested()
    {
        if (NavView.IsPaneOpen &&
            (NavView.DisplayMode == NavigationViewDisplayMode.Minimal
             || NavView.DisplayMode == NavigationViewDisplayMode.Compact))
        {
            NavView.IsPaneOpen = false;
            return true;
        }

        if (!ContentFrame.CanGoBack) return false;

        ContentFrame.GoBack();
        return true;
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        NavView.IsBackEnabled = ContentFrame.CanGoBack;
        Type sourcePageType = ContentFrame.SourcePageType;
        if (sourcePageType != null)
        {
            NavigationInfo navInfo;
            if (navigationManager.CurrentNavigationInfo is NavigationInfo _navInfo && _navInfo.PageType == sourcePageType)
            {
                navInfo = _navInfo;
            }
            else
            {
                navInfo = navigationManager.GetNavigationInfoForPageType(sourcePageType);
                navigationManager.SetCurrentNavigationInfo(navInfo);
            }

            NavView.SelectedItem = NavView.FooterMenuItems
                    .OfType<NavigationViewItem>().
                    FirstOrDefault(n => n.Tag.Equals(navInfo.Path)) ??
                    NavView.MenuItems
                    .OfType<NavigationViewItem>()
                    .FirstOrDefault(n => n.Tag.Equals(navInfo.Path));

            NavView.Header = navInfo.Title;
        }
    }
}
