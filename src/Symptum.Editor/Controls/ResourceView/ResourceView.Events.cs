using Windows.System;

namespace Symptum.Editor.Controls;

public sealed partial class ResourceView
{
    private void OnElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        if (args.Element is not ResourceViewItem item) return;

        if (item.GetTemplateChild("ExpandCollapseZone") is Border expandCollapse)
        {
            expandCollapse.Tapped -= OnExpandCollapseTapped;
            expandCollapse.Tapped += OnExpandCollapseTapped;
        }

        if (item.GetTemplateChild("ContentZone") is Grid contentZone)
        {
            contentZone.Tapped -= OnContentTapped;
            contentZone.Tapped += OnContentTapped;
        }

        item.DoubleTapped -= OnDoubleTapped;
        item.DoubleTapped += OnDoubleTapped;

        UpdateCheckBoxVisibility(item);
    }

    private void OnElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
    {
        if (args.Element is not ResourceViewItem item) return;

        if (item.GetTemplateChild("ExpandCollapseZone") is Border expandCollapse)
        {
            expandCollapse.Tapped -= OnExpandCollapseTapped;
        }

        if (item.GetTemplateChild("ContentZone") is Grid contentZone)
        {
            contentZone.Tapped -= OnContentTapped;
        }

        item.DoubleTapped -= OnDoubleTapped;

        UpdateCheckBoxVisibility(item);
    }

    private void OnExpandCollapseTapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ResourceViewNode node)
        {
            FocusedIndex = node._flatIndex;
            ToggleExpansion(node);
            Focus(FocusState.Keyboard);
            e.Handled = true;
        }
    }

    private void OnContentTapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ResourceViewNode node)
        {
            FocusedIndex = node._flatIndex;
            if (SelectionMode == ResourceViewSelectionMode.Single)
                HandleSelectionChanged(node);
            HandleItemInvoked(node);
            Focus(FocusState.Keyboard);
            e.Handled = true;
        }
    }

    private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ResourceViewNode node)
        {
            HandleItemInvoked(node);
            e.Handled = true;
        }
    }

    private void OnResourceViewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Up:
                if (FocusPrevious())
                {
                    BringFocusedIntoView();
                    if (SelectionMode == ResourceViewSelectionMode.Single && FocusedItem != null)
                        HandleSelectionChanged(FocusedItem);
                    Focus(FocusState.Keyboard);
                }
                e.Handled = true;
                break;

            case VirtualKey.Down:
                if (FocusNext())
                {
                    BringFocusedIntoView();
                    if (SelectionMode == ResourceViewSelectionMode.Single && FocusedItem != null)
                        HandleSelectionChanged(FocusedItem);
                    Focus(FocusState.Keyboard);
                }
                e.Handled = true;
                break;

            case VirtualKey.Left:
                {
                    var item = FocusedItem;
                    if (item == null) break;

                    if (item.IsExpanded)
                        ToggleExpansion(item);
                    else if (item.Parent != null)
                    {
                        FocusedIndex = item.Parent._flatIndex;
                        if (SelectionMode == ResourceViewSelectionMode.Single && FocusedItem != null)
                            HandleSelectionChanged(FocusedItem);
                        BringFocusedIntoView();
                        Focus(FocusState.Keyboard);
                    }
                    e.Handled = true;
                    break;
                }

            case VirtualKey.Right:
                {
                    var item = FocusedItem;
                    if (item == null) break;

                    if (item.HasChildren && !item.IsExpanded)
                        ToggleExpansion(item);
                    if (item.IsExpanded && item.HasChildren)
                    {
                        FocusedIndex = item._flatIndex + 1;
                        if (SelectionMode == ResourceViewSelectionMode.Single && FocusedItem != null)
                            HandleSelectionChanged(FocusedItem);
                        BringFocusedIntoView();
                        Focus(FocusState.Keyboard);
                    }
                    e.Handled = true;
                    break;
                }

            case VirtualKey.Enter:
                {
                    var item = FocusedItem;
                    if (item != null)
                    {
                        if (SelectionMode == ResourceViewSelectionMode.Single)
                            HandleSelectionChanged(item);
                        HandleItemInvoked(item);
                        e.Handled = true;
                    }
                    break;
                }

            case VirtualKey.Space:
                {
                    var item = FocusedItem;
                    if (item != null)
                    {
                        HandleSelectionChanged(item);
                        e.Handled = true;
                    }
                    break;
                }
        }
    }
}
