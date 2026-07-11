using Microsoft.UI.Xaml.Media.Animation;
using Symptum.Common.Helpers;
using Symptum.Core.Management.Navigation;
using Symptum.Navigation;
using Symptum.Pages;
using Symptum.ViewModels;
using Windows.UI.Core;

namespace Symptum;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();

#if WINDOWS && !HAS_UNO

        if (WindowHelper.MainWindow is Window mainWindow)
            mainWindow.SetTitleBar(titleBar);

        titleBar.Title = App.AppName;
        titleBar.BackRequested += (s, e) => BackRequested();
        titleBar.PaneToggleRequested += (s, e) => navView.IsPaneOpen = !navView.IsPaneOpen;
        navView.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
        navView.IsPaneToggleButtonVisible = false;
        Background = null;
#else
        navView.PaneTitle = App.AppName;
#endif

        contentFrame.Navigated += ContentFrame_Navigated;
        NavigationManager.NavigationRequested += (s, e) => NavView_Navigate(e, new EntranceNavigationTransitionInfo());
        navView.SelectionChanged += NavView_SelectionChanged;
        navView.BackRequested += (s, e) => BackRequested();

#if HAS_UNO
        SystemNavigationManager.GetForCurrentView().BackRequested += (s, e) => e.Handled = BackRequested();
#endif
    }

    public MainViewModel ViewModel { get; } = MainViewModel.Instance;

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem != null)
        {
            var navItem = args.SelectedItem as NavigationInfo;
            NavView_Navigate(navItem, args.RecommendedNavigationTransitionInfo, true);
        }
        _suppressNavigation = false;
    }

    private void NavView_Navigate(INavigable? navigable, NavigationTransitionInfo info, bool selectionChanged = false)
    {
        if (_suppressNavigation && selectionChanged) return;

        _suppressNavigation = true;

        if (contentFrame.Content is NavigablePage page)
            page.Navigable = null;

        navigable ??= NavigationManager.HomeNavInfo;
        Type? pageType = NavigationManager.GetPageTypeForNavigable(navigable);

        if (pageType != null && NavigationManager.CurrentUri != navigable.Uri)
        {
            contentFrame.Navigate(pageType, navigable, info);
        }
    }

    private bool BackRequested()
    {
        if (navView.IsPaneOpen &&
            (navView.DisplayMode == NavigationViewDisplayMode.Minimal
             || navView.DisplayMode == NavigationViewDisplayMode.Compact))
        {
            navView.IsPaneOpen = false;
            return true;
        }

        if (!contentFrame.CanGoBack) return false;

        contentFrame.GoBack();
        return true;
    }

    private bool _suppressNavigation = false;

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        navView.IsBackEnabled = contentFrame.CanGoBack;
        if (e.SourcePageType != null)
        {
            INavigable? navigable = e.Parameter as INavigable;
            if (navigable is NavigationInfo navInfo && navInfo.PageType == e.SourcePageType)
            {
                navView.SelectedItem = navInfo;
            }
            else if (NavigationManager.GetNavigationInfoForUri(navigable.Uri) is NavigationInfo navInfo2 &&
                navInfo2.PageType == e.SourcePageType)
            {
                // If it's a NavigableResource but has a registered NavigationInfo, select that instead.
                navView.SelectedItem = navInfo2;
            }
            else
            {
                // If the selected item was already null, SelectionChanged won't be triggered.
                // But _suppressNavigation is will still be true, so we need to reset it here.
                if (navView.SelectedItem != null)
                    navView.SelectedItem = null;
                else _suppressNavigation = false;
            }

            NavigationManager.CurrentUri = navigable.Uri;

            if (e.Content is NavigablePage page)
                page.Navigable = NavigationManager.GetRealNavigable(navigable);

            // navView.Header = navigable?.Title;
        }
    }
}
