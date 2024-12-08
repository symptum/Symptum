using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Core.Data;
using Symptum.Core.Data.Nutrition;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Extensions;
using Symptum.Core.Management.Deployment;
using Symptum.Core.Subjects;

namespace Symptum.Core.Management.Resources;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$feature",
    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(Subject), "subject")]
[JsonDerivedType(typeof(ReferenceValuesPackage), "referenceValues")]
[JsonDerivedType(typeof(NutritionPackage), "nutrition")]
public abstract partial class PackageResource : MetadataResource, IPackageResource
{
    #region Properties

    [ObservableProperty]
    public partial string? Description { get; set; }

    [ObservableProperty]
    public partial Version? Version { get; set; }

    [ObservableProperty]
    public partial IList<AuthorInfo>? Authors { get; set; }

    [JsonIgnore]
    [ObservableProperty]
    public partial IList<IPackageResource>? Dependencies { get; set; }

    [JsonPropertyName(nameof(Dependencies))]
    [ObservableProperty]
    public partial IList<string>? DependencyIds { get; set; }

    [ObservableProperty]
    public partial IList<string>? Tags { get; set; }

    #endregion
}

public abstract class PackageResource<T> : PackageResource where T : IResource
{
    #region Properties

    private ObservableCollection<T>? contents;

    [JsonIgnore]
    public virtual ObservableCollection<T>? Contents
    {
        get => contents;
        set
        {
            UnobserveCollection(contents);
            SetProperty(ref contents, value);
            SetChildrenResources(contents);
        }
    }

    #endregion

    protected override void OnInitializeResource(IResource? parent) => SetChildrenResources(Contents);

    protected virtual bool ChildRestraint(Type childResourceType) => true;

    public override bool CanHandleChildResourceType(Type childResourceType) =>
        typeof(T).IsAssignableFrom(childResourceType) && ChildRestraint(childResourceType);

    public override bool CanAddChildResourceType(Type childResourceType) => CanHandleChildResourceType(childResourceType);

    protected override void OnAddChildResource(IResource? childResource)
    {
        Contents ??= [];
        Contents.AddItemToListIfNotExists(childResource);
    }

    protected override void OnRemoveChildResource(IResource? childResource) => Contents.RemoveItemFromListIfExists(childResource);
}

public abstract class PackageResource<T, TCondition> : PackageResource<T> where T : IResource
{
    protected override bool ChildRestraint(Type childResourceType) => typeof(TCondition).IsAssignableFrom(childResourceType);
}
