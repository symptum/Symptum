namespace Symptum.Models;

public class SearchSegment(string text, bool isMatch)
{
    public string Text { get; } = text;

    public bool IsMatch { get; } = isMatch;
}
