namespace Symptum.Core.Data.Bibliography;

public record LiteratureReference : ReferenceBase
{
    public string? Authors { get; init; }

    public string? Title { get; init; }

    public string? Description { get; init; }

    public int Volume { get; init; }

    public string? Pages { get; init; }

    public int Year { get; init; }
    
    public string? Editors { get; init; }

    public string? Publisher { get; init; }

    public Uri? Url { get; init; }
}
