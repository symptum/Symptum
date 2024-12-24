namespace Symptum.Core.Data.Bibliography;

public record BookReference : LiteratureReference
{
    public string? Section { get; init; }

    public int Edition { get; init; }

    public int ISBN { get; init; }
}
