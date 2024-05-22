namespace Symptum.Core.Management.Navigation;

public class NavigationManager
{
    static NavigationManager()
    {
        
    }

    public static event EventHandler<NavigationRequestedEventArgs> NavigationRequested;
}

public class NavigationRequestedEventArgs : EventArgs
{

}
