namespace Symptum.Core;

// Will be used for classes which function as Identifiers. Eg: Question Entries, Reference Values
// Will be helpful for navigation, bookmarking, etc.
public interface IUniqueId
{
    static abstract IUniqueId? Parse(string? idText);
}
