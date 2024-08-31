namespace Symptum.Core.Data.Bibliography;

public class LiteratureReference : ReferenceBase
{
    public string Authors { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public int Volume { get; set; }

    public string Pages { get; set; }

    public int Year { get; set; }
    
    public string Editors { get; set; }

    public string Publisher { get; set; }

    public Uri Url { get; set; }
}
