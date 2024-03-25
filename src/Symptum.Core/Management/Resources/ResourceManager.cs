using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Symptum.Core.Management.Resources;

public class ResourceManager
{
    private static readonly string defaultUriScheme = "symptum://";

    public static readonly Uri DefaultUri = new(defaultUriScheme);

    private static readonly ObservableCollection<IResource> resources = [];

    public static ObservableCollection<IResource> Resources { get => resources; }

    public static Uri GetAbsoluteUri(string path) => new(defaultUriScheme + path);

    public static IList<IResource>? ResolveDependencies(IResource resource, IList<string> dependencyIds)
    {
        throw new NotImplementedException();
    }

    public static async Task<IList<IResource>?> ResolveDependenciesAsync(IResource resource, IList<string> dependencyIds)
    {
        return await Task.Run(() => ResolveDependencies(resource, dependencyIds));
    }

    public static void LoadResourceFile(FileResource fileResource, string content)
    {
        fileResource.ReadFileContent(content);
    }

    public static string WriteResourceFile(FileResource fileResource)
    {
        return fileResource.WriteFileContent();
    }

    public static PackageResource? LoadPackageMetadata(string content)
    {
        var package = JsonSerializer.Deserialize<PackageResource>(content);
        return package;
    }

    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull
    };

    public static string WritePackageMetadata(PackageResource package)
    {
        return JsonSerializer.Serialize(package, jsonSerializerOptions);
    }
}
