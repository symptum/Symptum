using Symptum.Core.Management.Resources;

namespace Symptum.Common.ProjectSystem;

public class ProjectFolder : CategoryResource<IResource>
{
    public ProjectFolder() { }

    protected override bool ChildRestraint(Type childResourceType) => childResourceType != typeof(Project);

    protected override void OnInitializeResource(IResource? parent)
    {
        Id = ResourceManager.GenerateIdFromAncestors(this);
        Uri = new(ResourceManager.GenerateUriFromAncestors(this)!);
    }
}
