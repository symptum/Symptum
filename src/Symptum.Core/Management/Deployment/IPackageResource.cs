using Symptum.Core.Data;
using Symptum.Core.Management.Resources;

namespace Symptum.Core.Management.Deployment;

public interface IPackageResource : IResource
{
    public string? Description { get; set; }

    public Version? Version { get; set; }

    public IList<AuthorInfo>? Authors { get; set; }

    public IList<IPackageResource>? Dependencies { get; set; }

    public IList<string>? DependencyIds { get; set; }

    public IList<string>? Tags { get; set; }
}
