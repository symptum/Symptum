using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Core.Management.Resources;

namespace Symptum.Editor.Controls;

public partial class ResourceViewNode : ObservableObject
{
    private ResourceViewNode? _parent;
    private int _depth;

    internal int _flatIndex = -1;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial IResource Resource { get; private set; }

    [ObservableProperty]
    public partial bool HasChildren { get; private set; }
    
    public int Depth => _depth;

    public ResourceViewNode? Parent => _parent;

    public ResourceViewNode(IResource resource, ResourceViewNode? parent = null)
    {
        Resource = resource;
        _parent = parent;
        _depth = parent != null ? parent.Depth + 1 : 0;
        resource.InitializeResource(parent?.Resource);
        UpdateHasChildren();
    }

    public void UpdateHasChildren() =>
        HasChildren = Resource?.ChildrenResources?.Count > 0;
}
