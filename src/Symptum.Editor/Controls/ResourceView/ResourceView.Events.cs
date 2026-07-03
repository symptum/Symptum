using Windows.System;

namespace Symptum.Editor.Controls;

public sealed partial class ResourceView
{
    private void OnElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        if (args.Element is not ResourceViewItem item) return;

        item.ExpandCollapseRequested -= OnItemExpandCollapseRequested;
        item.ExpandCollapseRequested += OnItemExpandCollapseRequested;

        item.ContentTapped -= OnItemContentTapped;
        item.ContentTapped += OnItemContentTapped;

        item.DoubleTapped -= OnDoubleTapped;
        item.DoubleTapped += OnDoubleTapped;

        item.UpdateCheckBoxVisibility(SelectionMode);
    }

    private void OnElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
    {
        if (args.Element is not ResourceViewItem item) return;

        item.ExpandCollapseRequested -= OnItemExpandCollapseRequested;
        item.ContentTapped -= OnItemContentTapped;
        item.DoubleTapped -= OnDoubleTapped;
    }

    private void OnItemExpandCollapseRequested(ResourceViewItem item)
    {
        if (item.DataContext is ResourceViewNode node)
        {
            FocusedIndex = node._flatIndex;
            ToggleExpansion(node);
            item.Focus(FocusState.Pointer);
        }
    }

    private void OnItemContentTapped(ResourceViewItem item)
    {
        if (item.DataContext is ResourceViewNode node)
        {
            FocusedIndex = node._flatIndex;
            if (SelectionMode == ResourceViewSelectionMode.Single)
                HandleSelectionChanged(node);
            HandleItemInvoked(node);
            item.Focus(FocusState.Pointer);
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
                    FocusItem(_focusedIndex);
                }
                e.Handled = true;
                break;

            case VirtualKey.Down:
                if (FocusNext())
                {
                    BringFocusedIntoView();
                    if (SelectionMode == ResourceViewSelectionMode.Single && FocusedItem != null)
                        HandleSelectionChanged(FocusedItem);
                    FocusItem(_focusedIndex);
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
                        FocusItem(_focusedIndex);
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
                        FocusItem(_focusedIndex);
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
