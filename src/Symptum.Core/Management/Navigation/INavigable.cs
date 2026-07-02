namespace Symptum.Core.Management.Navigation;

// Classes implementing INavigable need not be implementing IResource

public interface INavigable
{
    public string? Title { get; set; }
    
    Uri? Uri { get; set; }
}
