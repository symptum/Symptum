namespace Symptum.Core.Management.Resources;

public struct AuthorInfo
{
    string Name { get; set; } // This will be similar to git author information i.e. "Name <Email>"

    string Email { get; set; }
}
