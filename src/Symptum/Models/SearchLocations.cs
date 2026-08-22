namespace Symptum.Models;

[Flags]
public enum SearchLocations
{
    None = 0,
    Content = 1,
    Metadata = 2,
    Both = Content | Metadata
}
