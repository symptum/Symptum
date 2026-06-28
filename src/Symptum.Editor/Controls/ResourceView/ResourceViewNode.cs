using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Core.Management.Resources;

namespace Symptum.Editor.Controls;

public partial class ResourceViewNode : ObservableObject
{
    private IResource _resource;
    private ResourceViewNode? _parent;
    private int _depth;

    internal int _flatIndex = -1;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial bool IsFocused { get; set; }

    public IResource Resource => _resource;
    public int Depth => _depth;
    public ResourceViewNode? Parent => _parent;
    public bool HasChildren => _resource?.ChildrenResources?.Count > 0;

    public ResourceViewNode(IResource resource, ResourceViewNode? parent = null)
    {
        _resource = resource;
        _parent = parent;
        _depth = parent != null ? parent.Depth + 1 : 0;
        resource.InitializeResource(parent?.Resource);
    }
}
