using System.Collections.ObjectModel;
using Symptum.Core.Management.Resources;
using Symptum.Core.Extensions;

namespace Symptum.Core.Data.Nutrition;

public class NutritionDataSet : MetadataResource
{
    public NutritionDataSet()
    { }

    public NutritionDataSet(string title)
    {
        Title = title;
    }

    #region Properties

    private ObservableCollection<FoodGroup>? groups;

    public ObservableCollection<FoodGroup>? Groups
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
        return childResourceType == typeof(FoodGroup);
    }

    public override bool CanAddChildResourceType(Type childResourceType)
    {
        return childResourceType == typeof(FoodGroup);
    }

    protected override void OnAddChildResource(IResource? childResource)
    {
        Groups ??= [];
        if (childResource is FoodGroup group)
            Groups.Add(group);
    }

    protected override void OnRemoveChildResource(IResource? childResource)
    {
        Groups.RemoveItemFromListIfExists(childResource);
    }
}
