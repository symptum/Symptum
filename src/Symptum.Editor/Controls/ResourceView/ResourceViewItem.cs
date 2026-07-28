using System.ComponentModel;
using Symptum.Editor.Common;

namespace Symptum.Editor.Controls;

public class ResourceViewItem : ContentControl
{
    private bool _isPressed;
    private bool _isPointerOver;
    private bool _isSelected;
    private ResourceViewNode? _node;
    private Border? _expandCollapseZone;
    private Grid? _contentZone;
    private IconSourceElement? _iconElement;
    private FrameworkElement? _selectionIndicator;
    private CheckBox? _selectionCheckBox;

    internal event Action<ResourceViewItem>? ExpandCollapseRequested;
    internal event Action<ResourceViewItem>? ContentTapped;

    public ResourceViewItem()
    {
        UseSystemFocusVisuals = true;
        DataContextChanged += OnDataContextChanged;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _expandCollapseZone?.Tapped -= OnExpandCollapseTapped;
        _expandCollapseZone = GetTemplateChild("ExpandCollapseZone") as Border;
        _expandCollapseZone?.Tapped += OnExpandCollapseTapped;

        _contentZone?.Tapped -= OnContentTapped;
        _contentZone = GetTemplateChild("ContentZone") as Grid;
        _contentZone?.Tapped += OnContentTapped;

        _iconElement = GetTemplateChild("ResourceIconElement") as IconSourceElement;

        _selectionIndicator = GetTemplateChild("SelectionIndicator") as FrameworkElement;
        _selectionCheckBox = GetTemplateChild("SelectionCheckBox") as CheckBox;

        SubscribeToNode();
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        SubscribeToNode();
    }

    private void OnExpandCollapseTapped(object sender, TappedRoutedEventArgs e)
    {
        ExpandCollapseRequested?.Invoke(this);
        e.Handled = true;
    }

    private void OnContentTapped(object sender, TappedRoutedEventArgs e)
    {
        ContentTapped?.Invoke(this);
        e.Handled = true;
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

    internal void UpdateSelectionIndicator()
    {
        _selectionIndicator?.Visibility = _isSelected ? Visibility.Visible : Visibility.Collapsed;
    }

    internal void UpdateCheckBoxVisibility(ResourceViewSelectionMode mode)
    {
        _selectionCheckBox?.Visibility = mode == ResourceViewSelectionMode.Multiple
                ? Visibility.Visible : Visibility.Collapsed;
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
        _node?.PropertyChanged -= OnNodePropertyChanged;

        _node = DataContext as ResourceViewNode;

        if (_node != null)
        {
            _iconElement?.IconSource = DefaultIconSources.GetIconSourceForResourceType(_node.Resource?.GetType());
            _isSelected = _node.IsSelected;
            _node.PropertyChanged += OnNodePropertyChanged;
            UpdateSelectionIndicator();
            UpdateVisualStates();
        }
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_node == null) return;

        if (e.PropertyName == nameof(ResourceViewNode.IsSelected))
        {
            _isSelected = _node.IsSelected;
            UpdateSelectionIndicator();
            UpdateVisualStates();
        }
    }
}
