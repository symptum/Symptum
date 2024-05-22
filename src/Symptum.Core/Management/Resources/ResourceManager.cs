using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Symptum.Core.Management.Deployment;

namespace Symptum.Core.Management.Resources;

public class ResourceManager
{
    private static readonly string defaultUriScheme = "symptum://";

    public static readonly Uri DefaultUri = new(defaultUriScheme);

    private static readonly ObservableCollection<IResource> _resources = [];

    public static ObservableCollection<IResource> Resources { get => _resources; }

    public static Uri GetAbsoluteUri(string path) => new(defaultUriScheme + path);

    #region Dependency Resolution

    private static readonly Dictionary<string, List<TaskCompletionSource<IResource?>>> dependencyLinks = [];

    // TODO: Make thread safe
    public static void ResolveDependencies(IResource? resource)
    {
        if (resource != null && resource.DependencyIds != null)
        {
            ObservableCollection<IResource>? dependencies = [];
            resource.Dependencies = dependencies;
            foreach (var dependencyId in resource.DependencyIds)
            {
                ResolveDependencyAsync(resource, dependencyId);
            }
        }
    }

    private static async void ResolveDependencyAsync(IResource? resource, string dependencyId)
    {
        IResource? dependency = await GetDependencyAsync(dependencyId);
        if (dependency != null)
            resource?.Dependencies?.Add(dependency);
    }

    private static async Task<IResource?> GetDependencyAsync(string id)
    {
        TaskCompletionSource<IResource?> taskCompletionSource = new();
        if (!dependencyLinks.TryGetValue(id, out List<TaskCompletionSource<IResource?>>? tasks))
        {
            tasks = [];
            dependencyLinks.Add(id, tasks);
        }

        tasks.Add(taskCompletionSource);
        var resource = await taskCompletionSource.Task;
        return resource;
    }

    private static int resolutions = 0;

    // Called first after loading all the local resources.
    // Then again after all the primary dependencies have been loaded.
    // Then so on after loading further dependencies.
    public static void StartDependencyResolution()
    {
        resolutions++;
        List<string> finishedIds = [];
        foreach (var pair in dependencyLinks)
        {
            var id = pair.Key;
            var tasks = pair.Value;
            IResource? dependency = Resources.FirstOrDefault(x => x.Id == id);
            if (dependency != null)
            {
                foreach (var task in tasks)
                {
                    task.SetResult(dependency);
                }
                tasks.Clear();
                finishedIds.Add(id);
            }
            else
            {
                // Load Dependency?
                LoadDependencyAsync(id);
            }
        }
        foreach (var id in finishedIds)
        {
            dependencyLinks.Remove(id);
        }
        finishedIds.Clear();
        //System.Diagnostics.Debug.WriteLine("No. of resolutions: " + resolutions);
        //foreach (var resource in resources)
        //{
        //    string output = $"{resource.Title}[{resource.Id}] depends on: ";
        //    foreach (var dependency in resource.Dependencies)
        //    {
        //        output += $"{dependency.Title}[{dependency.Id}],";
        //    }
        //    System.Diagnostics.Debug.WriteLine(output);
        //}
    }

    // idk what or how this works, made sense to me
    private static int loadWaits = 0;

    private static async void LoadDependencyAsync(string id)
    {
        loadWaits++;
        // Ask PackageManager to fetch (from remote or cache) the package.
        var package = await PackageManager.LoadPackageAsync(id);
        if (package != null)
        {
            Resources.Add(package);
            // Link the newly loaded dependencies
            if (dependencyLinks.TryGetValue(id, out var tasks))
            {
                foreach (var task in tasks)
                {
                    task.SetResult(package);
                }
                dependencyLinks.Remove(id);
            }
            ResolveDependencies(package);

            loadWaits--;
        }
        if (loadWaits == 0)
        {
            StartDependencyResolution();
            _ = id;
        }
    }

    #endregion

    #region Resource File Handling

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

    #endregion

    #region Resource Fetching

    // TODO: Make these async

    public static IResource? TryGetResourceFromUri(Uri? uri)
    {
        if (uri == null) return null;
        return TryGetResourceFromUri(uri, _resources);
    }

    internal static IResource? TryGetResourceFromUri(Uri? uri, IList<IResource>? resources)
    {
        if (uri == null || resources == null) return null;
        foreach (IResource resource in resources)
        {
            if (resource.Uri == uri)
            {
                Debug.WriteLine("No. of turns it took to fetch the resource from Uri: " + _c);
                return resource;
            }
            else if (UriContains(uri, resource.Uri))
            {
                // Should the children be manually loaded?
                return TryGetResourceFromUri(uri, resource.ChildrenResources);
            }
        }

        return null;
    }

    private static int _c = 0;

    internal static bool UriContains(Uri? requiredUri, Uri? resourceUri)
    {
        _c++;
        if (requiredUri != null && resourceUri != null)
        {
            if (requiredUri.Scheme == requiredUri.Scheme &&
                requiredUri.Host == resourceUri.Host)
            {
                // Resource's uri would most probably be equal or shorter in segments than the required uri
                // So we take the resource's uri and compare all the segments with the other
                // If it matches, we return true
                // Else, the current resource tree doesn't contain the required uri
                string[] segmentsA = requiredUri.Segments; // Segments are created every call. TODO: Optimization?
                string[] segmentsB = resourceUri.Segments;
                if (segmentsA.Length > 1 && segmentsB.Length > 1)
                {
                    // We skip 0th index because it is just a "/"
                    for (int i = 1; i < segmentsB.Length; i++)
                    {
                        string a = segmentsA[i].Trim('/');
                        string b = segmentsB[i].Trim('/');
                        bool match = string.Equals(a, b, StringComparison.InvariantCultureIgnoreCase);
                        if (!match) break;
                        if (i == segmentsB.Length - 1)
                            return true;
                    }
                }
            }
        }

        return false;
    }

    public static IResource? TryGetResourceFromId(string? id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return TryGetResourceFromId(id, _resources);
    }

    internal static IResource? TryGetResourceFromId(string? id, IList<IResource>? resources)
    {
        if (string.IsNullOrEmpty(id) || resources == null) return null;
        foreach (IResource resource in resources)
        {
            if (string.Equals(resource.Id, id, StringComparison.InvariantCulture)) // Case sensitive?
            {
                Debug.WriteLine("No. of turns it took to fetch the resource from Id: " + _c2);
                return resource;
            }
            else if (IdContains(id, resource.Id))
            {
                return TryGetResourceFromId(id, resource.ChildrenResources);
            }
        }

        return null;
    }

    private static int _c2 = 0;

    internal static bool IdContains(string? requiredId, string? resourceId)
    {
        _c2++;
        if (!string.IsNullOrEmpty(requiredId) && !string.IsNullOrWhiteSpace(requiredId) &&
            !string.IsNullOrEmpty(resourceId) && !string.IsNullOrWhiteSpace(resourceId))
        {
            // Similar to UriContains
            string[] segmentsA = requiredId.Split('.'); // TODO: Optimization?
            string[] segmentsB = resourceId.Split('.');
            if (segmentsA.Length > 0 && segmentsB.Length > 0)
            {
                for (int i = 0; i < segmentsB.Length; i++)
                {
                    string a = segmentsA[i].Trim();
                    string b = segmentsB[i].Trim();
                    bool match = string.Equals(a, b, StringComparison.InvariantCulture); // Case sensitive?
                    if (!match) break;
                    if (i == segmentsB.Length - 1)
                        return true;
                }
            }
        }

        return false;
    }

    #endregion
}
