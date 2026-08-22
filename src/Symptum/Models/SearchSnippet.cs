namespace Symptum.Models;

public class SearchSnippet(string field, string pre, string match, string post)
{
    public string Field { get; } = field;

    public string Pre { get; } = pre;

    public string Match { get; } = match;

    public string Post { get; } = post;
}
