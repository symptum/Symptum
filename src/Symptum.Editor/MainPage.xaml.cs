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
            EditorPagesManager.ShowWelcomePage();
        };
    }

    #region Properties

    public static DependencyProperty ShowResourcesPaneProperty = DependencyProperty.Register(
        nameof(ShowResourcesPane),
        typeof(bool),
        typeof(MainPage),
        new(true, OnShowResourcesPaneProperty));

    private static void OnShowResourcesPaneProperty(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MainPage page)
            page.ShowOrHideResourcesPane((bool)e.NewValue);
    }

    public bool ShowResourcesPane
    {
        get => (bool)GetValue(ShowResourcesPaneProperty);
        set => SetValue(ShowResourcesPaneProperty, value);
    }

    public MainViewModel ViewModel { get => MainViewModel.Instance; }

    #endregion

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
            VisualStateManager.GoToState(this, collapsed || !ShowResourcesPane ? "MinimalState" : "DefaultState", true);
        }
    }

    private void ShowOrHideResourcesPane(bool showResourcesPane)
    {
        ToolTipService.SetToolTip(showResourcesPaneButton, showResourcesPane ? "Unpin" : "Pin");
        resourcesPaneButtonSymbolIcon.Symbol = showResourcesPane ? Symbol.UnPin : Symbol.Pin;
        showResourcesPaneMenuItem.IsChecked = showResourcesPane;
        VisualStateManager.GoToState(this, showResourcesPane && !_collapsed ? "DefaultState" : "MinimalState", true);
    }

    private void ShowResourcesPaneButton_Click(object sender, RoutedEventArgs e)
    {
        ShowResourcesPane = !ShowResourcesPane;
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
