using Symptum.Core.Management.Resources;
using Symptum.Core.Serialization;

namespace Symptum.Core.Data.Nutrition;

public class NutritionPackage : PackageResource
{
    public NutritionPackage()
    { }

    public NutritionPackage(string title)
    {
        Title = title;
    }

    #region Properties

    private NutritionDataSet? dataSet;

    [MetadataResource]
    public NutritionDataSet? DataSet
    {
        get => dataSet;
        set
        {
            UpdateChildrenResources(value);
            SetProperty(ref dataSet, value);
        }
    }

    #endregion

    protected override void OnInitializeResource(IResource? parent)
    {
        if (DataSet != null)
            AddChildResourceInternal(DataSet);
    }

    public override bool CanHandleChildResourceType(Type childResourceType) => childResourceType == typeof(NutritionDataSet);

    public override bool CanAddChildResourceType(Type childResourceType) => DataSet == null;

    protected override void OnAddChildResource(IResource? childResource)
    {
        if (childResource is NutritionDataSet _ds)
            DataSet = _ds;
    }

    protected override void OnRemoveChildResource(IResource? childResource)
    {
        if (childResource is NutritionDataSet)
            DataSet = null;
    }

    private void UpdateChildrenResources(NutritionDataSet? value)
    {
        if (ChildrenResources != null)
        {
            if (dataSet != null && ChildrenResources.Contains(dataSet))
                RemoveChildResourceInternal(dataSet);
            if (value != null)
                AddChildResourceInternal(value);
        }
    }
}
