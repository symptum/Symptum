using Symptum.Core.Management.Resources;

namespace Symptum.Core.Management.Deployment;

public interface IPackageResource : IResource
{
    public string? Description { get; set; }

    public Version? Version { get; set; }

    public IList<AuthorInfo>? Authors { get; set; }

    public IList<string>? Contents { get; set; } // Is this necessary?

    public IList<string>? Tags { get; set; }
}
