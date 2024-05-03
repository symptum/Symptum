using Symptum.Core.Management.Resources;

namespace Symptum.Core.Management.Deployment;

public interface IPackageResource : IResource
{
    string Description { get; set; }

    Version Version { get; set; }

    IList<AuthorInfo>? Authors { get; set; }

    IList<string>? Contents { get; set; }

    IList<string>? Tags { get; set; }
}
