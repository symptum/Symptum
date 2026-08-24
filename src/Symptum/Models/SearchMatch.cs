namespace Symptum.Models;

public class SearchMatch(string field, IReadOnlyList<SearchSegment> segments)
{
    public string Field { get; } = field;

    public bool HasField => !string.IsNullOrEmpty(Field);

    public IReadOnlyList<SearchSegment> Segments { get; } = segments;
}
