using System.Collections.Specialized;
using Symptum.Core.Management.Resources;

namespace Symptum.Editor.Controls;

public sealed partial class ResourceView
{
    private readonly Dictionary<IResource, ResourceViewNode> _nodeMap = new();
    private readonly List<ResourceViewNode> _rootNodes = new();
    private readonly Dictionary<INotifyCollectionChanged, ResourceViewNode> _sourceObserverMap = new();
    private INotifyCollectionChanged? _sourceObservable;
    private ResourceViewNode? _lastSelected;
    private int _focusedIndex = -1;
    private bool _suppressEvents;

    public void SetRootNodes(IReadOnlyList<IResource>? resources)
    {
        ClearAll();
        if (resources != null)
        {
            foreach (var resource in resources)
            {
                var wrapper = new ResourceViewNode(resource);
                _rootNodes.Add(wrapper);
                _nodeMap[resource] = wrapper;
            }

            if (resources is INotifyCollectionChanged observable)
            {
                _sourceObservable = observable;
                _sourceObservable.CollectionChanged -= OnRootSourceChanged;
                _sourceObservable.CollectionChanged += OnRootSourceChanged;
            }
        }
        FlattenVisible();
    }

    public void ToggleExpansion(ResourceViewNode node)
    {
        if (node == null || node._flatIndex < 0) return;
        node.IsExpanded = !node.IsExpanded;
    }

    public void ExpandAll()
    {
        _suppressEvents = true;
        CreateAllWrappers();
        foreach (var node in _nodeMap.Values)
            node.IsExpanded = true;
        _suppressEvents = false;
        FlattenVisible();
    }

    public void CollapseAll()
    {
        _suppressEvents = true;
        foreach (var node in _nodeMap.Values)
            node.IsExpanded = false;
        _suppressEvents = false;
        FlattenVisible();
    }

    public void HandleItemInvoked(ResourceViewNode node)
    {
        if (node == null) return;
        ResourceOpenRequested?.Invoke(this, node.Resource);
    }

    public void HandleSelectionChanged(ResourceViewNode node)
    {
        if (node == null || SelectionMode == ResourceViewSelectionMode.None) return;

        if (SelectionMode == ResourceViewSelectionMode.Single)
        {
            if (_lastSelected != null && _lastSelected != node)
                _lastSelected.IsSelected = false;
            if (!node.IsSelected)
            {
                node.IsSelected = true;
                _lastSelected = node;
                SelectedResource = node.Resource;
            }
        }
        else
        {
            node.IsSelected = !node.IsSelected;
            UpdateSelectedResources();
        }
    }

    public bool FocusNext()
    {
        if (_focusedIndex < 0) { FocusedIndex = 0; return true; }
        if (_focusedIndex >= FlattenedItems.Count - 1) return false;
        FocusedIndex++;
        return true;
    }

    public bool FocusPrevious()
    {
        if (_focusedIndex <= 0) return false;
        FocusedIndex--;
        return true;
    }

    public int FocusedIndex
    {
        get => _focusedIndex;
        set => _focusedIndex = Math.Clamp(value, -1, FlattenedItems.Count - 1);
    }

    public ResourceViewNode? FocusedItem => _focusedIndex >= 0 && _focusedIndex < FlattenedItems.Count
        ? FlattenedItems[_focusedIndex] : null;

    private void FlattenVisible()
    {
        FlattenedItems.Clear();
        foreach (var kvp in _nodeMap)
            kvp.Value._flatIndex = -1;
        UnobserveAllSources();
        _focusedIndex = -1;

        // There is a possibility that the focused item could be removed from the visual tree.
        // So we shift the keyboard focus back to the ResourceView.
        Focus(FocusState.Keyboard);

        int index = 0;
        foreach (var root in _rootNodes)
            index = FlattenNode(root, index);
    }

    private int FlattenNode(ResourceViewNode node, int index)
    {
        node._flatIndex = index;
        SubscribeEvents(node);
        FlattenedItems.Add(node);
        node.UpdateHasChildren();
        index++;

        if (node.Resource.ChildrenResources != null)
        {
            ObserveSourceChildren(node);
            if (node.IsExpanded)
                foreach (var childResource in node.Resource.ChildrenResources)
                {
                    var childNode = ResolveOrCreateNode(childResource, node);
                    index = FlattenNode(childNode, index);
                }
        }
        return index;
    }

    private void CreateAllWrappers()
    {
        foreach (var root in _rootNodes)
            CreateDescendantWrappers(root);
    }

    private void CreateDescendantWrappers(ResourceViewNode node)
    {
        if (node.Resource.ChildrenResources == null) return;
        foreach (var childResource in node.Resource.ChildrenResources)
        {
            var child = ResolveOrCreateNode(childResource, node);
            CreateDescendantWrappers(child);
        }
    }

    private ResourceViewNode ResolveOrCreateNode(IResource resource, ResourceViewNode parent)
    {
        if (!_nodeMap.TryGetValue(resource, out var node))
        {
            node = new ResourceViewNode(resource, parent);
            _nodeMap[resource] = node;
        }
        return node;
    }

    internal void ExpandFromNode(ResourceViewNode node)
    {
        if (node == null || node._flatIndex < 0) return;

        var focusedNode = _focusedIndex >= 0 && _focusedIndex < FlattenedItems.Count
            ? FlattenedItems[_focusedIndex] : null;

        int insertAt = node._flatIndex + 1;

        if (node.Resource.ChildrenResources != null)
        {
            foreach (var childResource in node.Resource.ChildrenResources)
            {
                var childNode = new ResourceViewNode(childResource, node);
                insertAt = AttachNode(childNode, insertAt);
            }
        }
        UpdateIndices(insertAt);

        if (focusedNode != null && focusedNode._flatIndex >= 0)
            _focusedIndex = focusedNode._flatIndex;
    }

    internal void CollapseFromNode(ResourceViewNode node)
    {
        if (node == null || node._flatIndex < 0) return;

        int removed = CascadeRemove(node._flatIndex + 1, node.Depth);
        UpdateIndices(node._flatIndex + 1);

        if (_focusedIndex > node._flatIndex)
        {
            if (_focusedIndex <= node._flatIndex + removed)
                _focusedIndex = node._flatIndex;
            else
                _focusedIndex -= removed;
        }
        Focus(FocusState.Keyboard);
    }

    private int AttachNode(ResourceViewNode node, int insertAt)
    {
        _nodeMap[node.Resource] = node;
        SubscribeEvents(node);
        FlattenedItems.Insert(insertAt, node);
        node._flatIndex = insertAt;
        insertAt++;

        ObserveSourceChildren(node);
        if (node.IsExpanded)
        {
            if (node.Resource.ChildrenResources != null)
            {
                foreach (var childResource in node.Resource.ChildrenResources)
                {
                    var childNode = new ResourceViewNode(childResource, node);
                    insertAt = AttachNode(childNode, insertAt);
                }
            }
        }
        return insertAt;
    }

    private int CascadeRemove(int startIndex, int parentDepth)
    {
        int removed = 0;
        while (startIndex < FlattenedItems.Count && FlattenedItems[startIndex].Depth > parentDepth)
        {
            var node = FlattenedItems[startIndex];
            DetachNode(node);
            FlattenedItems.RemoveAt(startIndex);
            removed++;
        }
        return removed;
    }

    private int RemoveNodeAndDescendants(ResourceViewNode node)
    {
        int startIndex = node._flatIndex;
        int parentDepth = node.Depth;
        int removed = 0;

        DetachNode(node);
        FlattenedItems.RemoveAt(startIndex);
        removed++;

        while (startIndex < FlattenedItems.Count && FlattenedItems[startIndex].Depth > parentDepth)
        {
            var child = FlattenedItems[startIndex];
            DetachNode(child);
            FlattenedItems.RemoveAt(startIndex);
            removed++;
        }

        return removed;
    }

    private void DetachNode(ResourceViewNode node)
    {
        UnsubscribeEvents(node);
        UnobserveSourceChildren(node);
        _nodeMap.Remove(node.Resource);
    }

    private void UnobserveSource()
    {
        _sourceObservable?.CollectionChanged -= OnRootSourceChanged;
        _sourceObservable = null;
    }

    private void OnRootSourceChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_suppressEvents) return;

        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (IResource resource in e.NewItems)
            {
                var wrapper = new ResourceViewNode(resource);
                _rootNodes.Add(wrapper);
                _nodeMap[resource] = wrapper;
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            foreach (IResource resource in e.OldItems)
            {
                if (_nodeMap.TryGetValue(resource, out var wrapper))
                {
                    _rootNodes.Remove(wrapper);
                    _nodeMap.Remove(resource);
                }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            SetRootNodes(sender as IReadOnlyList<IResource>);
            return;
        }

        FlattenVisible();
    }

    private void ObserveSourceChildren(ResourceViewNode node)
    {
        if (node.Resource.ChildrenResources is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged -= OnSourceChildrenChanged;
            observable.CollectionChanged += OnSourceChildrenChanged;
            _sourceObserverMap[observable] = node;
        }
    }

    private void UnobserveSourceChildren(ResourceViewNode node)
    {
        if (node.Resource.ChildrenResources is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged -= OnSourceChildrenChanged;
            _sourceObserverMap.Remove(observable);
        }
    }

    private void UnobserveAllSources()
    {
        foreach (var kvp in _sourceObserverMap)
            kvp.Key.CollectionChanged -= OnSourceChildrenChanged;
        _sourceObserverMap.Clear();
    }

    private void OnSourceChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_suppressEvents) return;
        if (sender is not INotifyCollectionChanged observable) return;
        if (!_sourceObserverMap.TryGetValue(observable, out var parentNode)) return;

        parentNode.UpdateHasChildren();

        if (!parentNode.IsExpanded) return;

        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            int insertAt = FindInsertIndex(parentNode, e.NewStartingIndex);
            foreach (IResource childResource in e.NewItems)
            {
                var childNode = new ResourceViewNode(childResource, parentNode);
                insertAt = AttachNode(childNode, insertAt);
            }
            UpdateIndices(insertAt);
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            var nodesToRemove = new List<ResourceViewNode>();
            foreach (IResource childResource in e.OldItems)
            {
                if (_nodeMap.TryGetValue(childResource, out var childNode) && childNode._flatIndex >= 0)
                    nodesToRemove.Add(childNode);
            }

            if (nodesToRemove.Count > 0)
            {
                int firstRemove = nodesToRemove.Min(n => n._flatIndex);
                nodesToRemove.Sort((a, b) => b._flatIndex.CompareTo(a._flatIndex));
                foreach (var node in nodesToRemove)
                    RemoveNodeAndDescendants(node);
                UpdateIndices(firstRemove);
                Focus(FocusState.Keyboard);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            CollapseFromNode(parentNode);
            ExpandFromNode(parentNode);
        }
    }

    private int FindInsertIndex(ResourceViewNode parentNode, int newStartingIndex)
    {
        int baseIndex = parentNode._flatIndex + 1;
        if (newStartingIndex > 0)
        {
            var prevResource = parentNode.Resource.ChildrenResources![newStartingIndex - 1];
            if (_nodeMap.TryGetValue(prevResource, out var prevNode) && prevNode._flatIndex >= 0)
            {
                baseIndex = prevNode._flatIndex + 1;
                while (baseIndex < FlattenedItems.Count && FlattenedItems[baseIndex].Depth > prevNode.Depth)
                    baseIndex++;
            }
        }
        return baseIndex;
    }

    private void SubscribeEvents(ResourceViewNode node)
    {
        node.PropertyChanged += OnNodePropertyChanged;
    }

    private void UnsubscribeEvents(ResourceViewNode node)
    {
        node.PropertyChanged -= OnNodePropertyChanged;
    }

    private void OnNodePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_suppressEvents) return;
        if (sender is not ResourceViewNode node) return;

        if (e.PropertyName == nameof(ResourceViewNode.IsExpanded))
        {
            if (node.IsExpanded)
                ExpandFromNode(node);
            else
                CollapseFromNode(node);
        }
        else if (e.PropertyName == nameof(ResourceViewNode.IsSelected))
        {
            if (SelectionMode == ResourceViewSelectionMode.Multiple)
                UpdateSelectedResources();
        }
    }

    private void UpdateIndices(int from)
    {
        for (int i = from; i < FlattenedItems.Count; i++)
            FlattenedItems[i]._flatIndex = i;
    }

    private void ClearSelection()
    {
        foreach (var item in FlattenedItems)
            item.IsSelected = false;
        _lastSelected = null;
        SelectedResource = null;
    }

    private void UpdateSelectedResources()
    {
        var selected = new List<IResource>();
        foreach (var item in FlattenedItems)
            if (item.IsSelected)
                selected.Add(item.Resource);
        SelectedResources = selected;
        ResourcesSelected?.Invoke(this, selected);
    }

    private void ClearAll()
    {
        UnobserveSource();
        _nodeMap.Clear();
        _rootNodes.Clear();
        FlattenedItems.Clear();
        UnobserveAllSources();
        _focusedIndex = -1;
        Focus(FocusState.Keyboard);
        _lastSelected = null;
    }

    private void BringFocusedIntoView()
    {
        if (_itemsRepeater == null) return;
        var element = _itemsRepeater.TryGetElement(_focusedIndex);
        if (element != null)
            element.StartBringIntoView();
        else if (_scrollViewer != null)
        {
            double y = _focusedIndex * 38.0;
            _scrollViewer.ChangeView(null, y, null);
        }
    }

    internal void FocusItem(int index)
    {
        if (_itemsRepeater?.TryGetElement(index) is ResourceViewItem item && item.DataContext is ResourceViewNode node)
        {
            FocusedIndex = node._flatIndex;
            item.Focus(FocusState.Keyboard);
        }
    }

    internal void UpdateCheckBoxVisibility(ResourceViewItem item)
    {
        item.UpdateCheckBoxVisibility(SelectionMode);
    }

    private void UpdateAllItemVisuals()
    {
        if (_itemsRepeater == null) return;
        for (int i = 0; i < FlattenedItems.Count; i++)
        {
            if (_itemsRepeater.TryGetElement(i) is ResourceViewItem item)
                UpdateCheckBoxVisibility(item);
        }
    }
}
