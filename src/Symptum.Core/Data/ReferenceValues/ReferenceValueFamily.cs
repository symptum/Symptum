using System.Collections.ObjectModel;
using Symptum.Core.Management.Resources;

namespace Symptum.Core.Data.ReferenceValues;

public class ReferenceValueFamily : NavigableResource
{
    public ReferenceValueFamily()
    { }

    public ReferenceValueFamily(string title)
    {
        Title = title;
    }

    #region Properties

    private ObservableCollection<ReferenceValueGroup>? groups;

    public ObservableCollection<ReferenceValueGroup>? Groups
    {
        get => groups;
        set
        {
            UnobserveCollection(groups);
            SetProperty(ref groups, value);
            SetChildrenResources(groups);
        }
    }

    #endregion

    protected override void OnInitializeResource(IResource? parent)
    {
        SetChildrenResources(groups);
    }

    public override bool CanHandleChildResourceType(Type childResourceType)
    {
        return childResourceType == typeof(ReferenceValueGroup);
    }

    public override bool CanAddChildResourceType(Type childResourceType)
    {
        return childResourceType == typeof(ReferenceValueGroup);
    }

    protected override void OnAddChildResource(IResource childResource)
    {
        Groups ??= [];
        if (childResource is ReferenceValueGroup group)
            Groups.Add(group);
    }

    protected override void OnRemoveChildResource(IResource childResource)
    {
        throw new NotImplementedException();
    }
}
