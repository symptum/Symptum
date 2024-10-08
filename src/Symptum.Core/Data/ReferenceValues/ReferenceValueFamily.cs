using System.Collections.ObjectModel;
using Symptum.Core.Extensions;
using Symptum.Core.Management.Resources;

namespace Symptum.Core.Data.ReferenceValues;

public class ReferenceValueFamily : MetadataResource
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

    protected override void OnAddChildResource(IResource? childResource)
    {
        Groups ??= [];
        Groups.AddItemToListIfNotExists(childResource);
    }

    protected override void OnRemoveChildResource(IResource? childResource)
    {
        Groups.RemoveItemFromListIfExists(childResource);
    }
}
