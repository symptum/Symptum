namespace Symptum.Core.Data.Bibliography;

internal class BookReference : LiteratureReference
{
    public string Section { get; set; }

    public int Edition { get; set; }

    public int ISBN { get; set; }
}
