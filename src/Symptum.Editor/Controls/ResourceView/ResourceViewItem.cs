using System.ComponentModel;

namespace Symptum.Editor.Controls;

public class ResourceViewItem : ContentControl
{
    private bool _isPressed;
    private bool _isPointerOver;
    private bool _isSelected;
    private bool _isFocused;
    private ResourceViewNode? _node;

    public ResourceViewItem()
    {
        IsTabStop = false;
        DataContextChanged += OnDataContextChanged;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        SubscribeToNode();
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        SubscribeToNode();
    }

    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);
        _isPointerOver = true;
        UpdateVisualStates();
    }

    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);
        _isPointerOver = false;
        UpdateVisualStates();
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        _isPressed = true;
        UpdateVisualStates();
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPressed = false;
        UpdateVisualStates();
    }

    internal void UpdateFocusVisual()
    {
        if (_isFocused)
            VisualStateManager.GoToState(this, "Focused", true);
        else
            VisualStateManager.GoToState(this, "Unfocused", true);
    }

    private void UpdateVisualStates()
    {
        if (_isPressed && _isSelected)
            VisualStateManager.GoToState(this, "PressedSelected", true);
        else if (_isPointerOver && _isSelected)
            VisualStateManager.GoToState(this, "PointerOverSelected", true);
        else if (_isSelected)
            VisualStateManager.GoToState(this, "Selected", true);
        else if (_isPressed)
            VisualStateManager.GoToState(this, "Pressed", true);
        else if (_isPointerOver)
            VisualStateManager.GoToState(this, "PointerOver", true);
        else
            VisualStateManager.GoToState(this, "Normal", true);
    }

    private void SubscribeToNode()
    {
        if (_node != null)
            _node.PropertyChanged -= OnNodePropertyChanged;

        _node = DataContext as ResourceViewNode;

        if (_node != null)
        {
            _isSelected = _node.IsSelected;
            _isFocused = _node.IsFocused;
            _node.PropertyChanged += OnNodePropertyChanged;
            UpdateFocusVisual();
            UpdateVisualStates();
        }
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_node == null) return;

        if (e.PropertyName == nameof(ResourceViewNode.IsSelected))
        {
            _isSelected = _node.IsSelected;
            UpdateVisualStates();
        }
        else if (e.PropertyName == nameof(ResourceViewNode.IsFocused))
        {
            _isFocused = _node.IsFocused;
            UpdateFocusVisual();
        }
    }
}
