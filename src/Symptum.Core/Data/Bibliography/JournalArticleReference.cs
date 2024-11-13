namespace Symptum.Core.Data.Bibliography;

public record JournalArticleReference : LiteratureReference
{
    public string? JournalName { get; init; }

    public int Issue { get; init; }
}
