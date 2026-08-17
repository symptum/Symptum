using Symptum.Core.Management.Resources;

namespace Symptum.Common.ProjectSystem;

/// <summary>
/// Represents a logical folder inside the project resource hierarchy. This
/// resource type groups other resources and participates in project entry
/// generation and resource path resolution.
/// </summary>
public class ProjectFolder : CategoryResource<IResource>
{
    public ProjectFolder() { }

    protected override bool ChildRestraint(Type childResourceType) => childResourceType != typeof(Project);

    protected override void OnInitializeResource()
    {
        Id = ResourceManager.GenerateIdFromAncestors(this);
        Uri = new(ResourceManager.GenerateUriFromAncestors(this)!);
    }
}
