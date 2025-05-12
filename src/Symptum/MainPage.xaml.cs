using Microsoft.UI.Xaml.Media.Animation;
using Symptum.Common.Helpers;
using Symptum.Core.Management.Navigation;
using Symptum.Navigation;
using Symptum.Pages;
using Windows.UI.Core;

namespace Symptum;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();

#if WINDOWS && !HAS_UNO

        if (WindowHelper.MainWindow is Window mainWindow)
        {
            mainWindow.SetTitleBar(AppTitleBar);
            TitleTextBlock.Text = mainWindow.Title;
        }

        Background = null;
#endif

        NavigationManager.NavigationRequested += (s, e) => NavView_Navigate(e, new EntranceNavigationTransitionInfo());

        NavView_Navigate(null, new EntranceNavigationTransitionInfo());
        NavView.BackRequested += (s, e) => BackRequested();
#if HAS_UNO
        SystemNavigationManager.GetForCurrentView().BackRequested += (s, e) => e.Handled = BackRequested();
#endif
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem != null)
        {
            var navItem = args.SelectedItem as NavigationInfo;
            NavView_Navigate(navItem, args.RecommendedNavigationTransitionInfo);
        }
    }

    private void NavView_Navigate(INavigable? navigable, NavigationTransitionInfo info)
    {
        navigable ??= NavigationManager.HomeNavInfo;
        Type? pageType = NavigationManager.GetPageTypeForNavigable(navigable);

        if (pageType != null && NavigationManager.CurrentNavigable != navigable)
        {
            ContentFrame.Navigate(pageType, navigable, info);
        }
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
        if (e.SourcePageType != null)
        {
            INavigable? navigable = e.Parameter as INavigable;
            if (navigable is NavigationInfo navInfo && navInfo.PageType == e.SourcePageType)
            {
                NavView.SelectedItem = navInfo;
            }
            else NavView.SelectedItem = null;

            NavigationManager.CurrentNavigable = navigable;
            if (e.Content is NavigablePage page) page.Navigable = navigable;
            NavView.Header = navigable?.Title;
        }
    }
}
