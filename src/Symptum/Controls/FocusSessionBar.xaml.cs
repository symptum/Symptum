using Symptum.Navigation;
using Symptum.ViewModels;

namespace Symptum.Controls;

public sealed partial class FocusSessionBar : UserControl
{
    private bool _isFocusPageVisible;

    public FocusSessionBar()
    {
        InitializeComponent();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        openButton.Click += (s, e) => NavigationManager.Navigate(NavigationManager.FocusSessionNavInfo); ;

        UpdateMode();
        UpdateProgress();
        UpdateVisibility();
    }

    public FocusSessionViewModel ViewModel => FocusSessionViewModel.Instance;

    public void SetIsFocusPageVisible(bool value)
    {
        if (_isFocusPageVisible == value) return;
        _isFocusPageVisible = value;
        UpdateVisibility();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(FocusSessionViewModel.IsRunning):
            case nameof(FocusSessionViewModel.MiniBarEnabled):
                UpdateVisibility();
                break;

            case nameof(FocusSessionViewModel.Mode):
                if (Visibility == Visibility.Visible) UpdateMode();
                break;

            case nameof(FocusSessionViewModel.Progress):
                if (Visibility == Visibility.Visible)  UpdateProgress();
                break;
        }
    }

    private void UpdateVisibility()
    {
        bool show = ViewModel.MiniBarEnabled && ViewModel.IsRunning && !_isFocusPageVisible;
        if (show == (Visibility == Visibility.Visible)) return;

        if (show)
        {
            root.Opacity = 0;
            UpdateMode();
            UpdateProgress();
            Visibility = Visibility.Visible;
            entranceAnimation.Begin();
        }
        else
        {
            entranceAnimation.Stop();
            Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateMode()
    {
        progress.Fill = ViewModel.Mode switch
        {
            FocusSessionMode.Focus => FocusSessionViewModel.FocusBrush,
            FocusSessionMode.ShortBreak => FocusSessionViewModel.ShortBreakBrush,
            FocusSessionMode.LongBreak => FocusSessionViewModel.LongBreakBrush,
            _ => null,
        };

        UpdateProgress();
    }

    private void UpdateProgress()
    {
        if (root.ActualWidth > 0)
        {
            progress.Width = root.ActualWidth * ViewModel.Progress;
        }
    }
}
