using System.Collections.ObjectModel;
using Symptum.Core.Management.Resources;

namespace Symptum.Core.Data.ReferenceValues;

public class ReferenceValuesPackage : PackageResource
{
    public ReferenceValuesPackage()
    { }

    public ReferenceValuesPackage(string title)
    {
        Title = title;
    }

    #region Properties

    private ObservableCollection<ReferenceValueFamily>? families;

    public ObservableCollection<ReferenceValueFamily>? Families
    {
        get => families;
        set
        {
            UnobserveCollection(families);
            SetProperty(ref families, value);
            SetChildrenResources(families);
        }
    }

    #endregion

    protected override void OnInitializeResource(IResource? parent)
    {
        SetChildrenResources(families);
    }

    public override bool CanHandleChildResourceType(Type childResourceType)
    {
        return childResourceType == typeof(ReferenceValueFamily);
    }

    public override bool CanAddChildResourceType(Type childResourceType)
    {
        return childResourceType == typeof(ReferenceValueFamily);
    }

    protected override void OnAddChildResource(IResource childResource)
    {
        Families ??= [];
        if (childResource is ReferenceValueFamily family)
            families?.Add(family);
    }

    protected override void OnRemoveChildResource(IResource childResource)
    {
        throw new NotImplementedException();
    }
}
