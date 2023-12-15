namespace Symptum.Core.Management.Resources;

public class ResourceManager
{
    private static string defaultUriScheme = "symptum://";

    public static Uri DefaultUri = new(defaultUriScheme);

    public static Uri GetAbsoluteUri(string path) => new(defaultUriScheme + path);

    public static IList<IResource>? ResolveDependencies(IResource resource, IList<string> dependencyIds)
    {
        throw new NotImplementedException();
    }

    public static async Task<IList<IResource>?> ResolveDependenciesAsync(IResource resource, IList<string> dependencyIds)
    {
        return await Task.Run(() => ResolveDependencies(resource, dependencyIds));
    }
}
