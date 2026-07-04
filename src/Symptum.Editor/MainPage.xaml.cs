using System.ComponentModel;
using Symptum.Common.Helpers;
using Symptum.Editor.Pages;
using Symptum.Editor.ViewModels;
using Windows.System;

namespace Symptum.Editor;

public sealed partial class MainPage : Page
{
    private bool _collapsed = false;

    public MainPage()
    {
        InitializeComponent();

#if WINDOWS && !HAS_UNO

        if (WindowHelper.MainWindow is Window mainWindow)
        {
            mainWindow.SetTitleBar(AppTitleBar);
        }
        titleTB.Text = App.AppTitle;

        Background = null;

#endif

        expandResourcesPaneButton.Click += (s, e) =>
        {
            splitView.IsPaneOpen = true;
        };

        SizeChanged += MainPage_SizeChanged;
        Loaded += (s, e) =>
        {
            ViewModel.Initialize();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.RecentItemsChanged += PopulateRecentMenus;
            PopulateRecentMenus();
            EditorPagesManager.ShowWelcomePage();
        };
    }

    public MainViewModel ViewModel { get; } = MainViewModel.Instance;

    private void MainPage_SizeChanged(object sender, SizeChangedEventArgs args)
    {
        bool collapsed = args.NewSize.Width switch
        {
            >= 1007 => false,
            _ => true
        };

        if (collapsed != _collapsed)
        {
            _collapsed = collapsed;
            VisualStateManager.GoToState(this, collapsed || !ViewModel.ShowResourcesPane ? "MinimalState" : "DefaultState", true);
        }
    }

    private void ViewModel_PropertyChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.ShowResourcesPane))
        {
            ShowOrHideResourcesPane();
        }
    }

    private void ShowOrHideResourcesPane()
    {
        bool showResourcesPane = ViewModel.ShowResourcesPane;
        ToolTipService.SetToolTip(showResourcesPaneButton, showResourcesPane ? "Unpin" : "Pin");
        resourcesPaneButtonSymbolIcon.Symbol = showResourcesPane ? Symbol.UnPin : Symbol.Pin;
        showResourcesPaneMenuItem.IsChecked = showResourcesPane;
        VisualStateManager.GoToState(this, showResourcesPane && !_collapsed ? "DefaultState" : "MinimalState", true);
    }

    private void ShowResourcesPaneButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowResourcesPane = !ViewModel.ShowResourcesPane;
    }

    private void PopulateRecentMenus()
    {
        openRecentMenuItem.Items.Clear();

        var items = ViewModel.RecentItems;
        if (items.Count == 0)
        {
            openRecentMenuItem.Visibility = Visibility.Collapsed;
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            string path = items[i];
            var menuItem = new MenuFlyoutItem
            {
                Text = path,
                Command = ViewModel.OpenRecentItemCommand,
                CommandParameter = path
            };
            ToolTipService.SetToolTip(menuItem, path);
            openRecentMenuItem.Items.Add(menuItem);
        }

        openRecentMenuItem.Items.Add(new MenuFlyoutSeparator());
        openRecentMenuItem.Items.Add(new MenuFlyoutItem()
                                         .Text("Clear Recent Items")
                                         .Command(ViewModel.ClearRecentItemsCommand));
        openRecentMenuItem.Visibility = Visibility.Visible;
    }

    private void CloseSelectedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        EditorPagesManager.TryCloseEditor(editorsTabView.SelectedItem as IEditorPage);
    }

    private void NavigateToNumberedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        int max = editorsTabView.TabItems.Count - 1;
        int tabToSelect = sender.Key switch
        {
            VirtualKey.Number1 => 0,
            VirtualKey.Number2 => 1,
            VirtualKey.Number3 => 2,
            VirtualKey.Number4 => 3,
            VirtualKey.Number5 => 4,
            VirtualKey.Number6 => 5,
            VirtualKey.Number7 => 6,
            VirtualKey.Number8 => 7,
            _ => max
        };

        tabToSelect = Math.Clamp(tabToSelect, 0, max);
        editorsTabView.SelectedIndex = tabToSelect;
    }

    private void CommandPalette_Click(object sender, RoutedEventArgs e)
    {
        commandPalette.ShowPalette();
    }
}
