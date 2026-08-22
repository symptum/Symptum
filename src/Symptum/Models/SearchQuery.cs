namespace Symptum.Models;

public class SearchQuery
{
    public string? QueryText { get; set; }

    public SearchLocations Locations { get; set; } = SearchLocations.Both;

    public string? ScopePackage { get; set; }

    public bool MatchCase { get; set; }

    public bool MatchWholeWord { get; set; }

    public int MaxResults { get; set; } = 50;
}
