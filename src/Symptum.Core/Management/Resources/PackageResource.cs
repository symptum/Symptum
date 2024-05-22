using System.Text.Json.Serialization;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Management.Deployment;
using Symptum.Core.Subjects;

namespace Symptum.Core.Management.Resources;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$feature",
    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(Subject), "subject")]
[JsonDerivedType(typeof(ReferenceValuesPackage), "referenceValues")]
public abstract class PackageResource : NavigableResource, IPackageResource
{
    #region Properties

    private string? description = string.Empty;

    public string? Description
    {
        get => description;
        set => SetProperty(ref description, value);
    }

    private Version? version = new(0, 0, 0);

    public Version? Version
    {
        get => version;
        set => SetProperty(ref version, value);
    }

    private IList<AuthorInfo>? authors;

    public IList<AuthorInfo>? Authors
    {
        get => authors;
        set => SetProperty(ref authors, value);
    }

    private IList<string>? contents;

    public IList<string>? Contents
    {
        get => contents;
        set => SetProperty(ref contents, value);
    }

    private IList<string>? tags;

    public IList<string>? Tags
    {
        get => tags;
        set => SetProperty(ref tags, value);
    }

    #endregion
}
