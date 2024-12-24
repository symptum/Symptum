using Symptum.Core.Management.Resources;

namespace Symptum.Common.ProjectSystem;

public class ProjectFolder : CategoryResource<IResource>
{
    protected override bool ChildRestraint(Type childResourceType) => childResourceType != typeof(Project);
}
