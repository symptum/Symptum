using Symptum.Core.Management.Resources;

namespace Symptum.Core.Management.Deployment;

public interface IPackage
{
    string Title { get; set; }

    string Description { get; set; }

    Version Version { get; set; }

    IList<AuthorInfo>? Authors { get; set; }

    IList<IResource>? Contents { get; set; }

    IList<IResource>? Dependencies { get; set; }

    IList<string>? DependencyIds { get; set; }

    IList<string>? Tags { get; set; }
}
