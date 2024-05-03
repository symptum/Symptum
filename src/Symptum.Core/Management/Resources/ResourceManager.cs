using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Symptum.Core.Management.Deployment;

namespace Symptum.Core.Management.Resources;

public class ResourceManager
{
    private static readonly string defaultUriScheme = "symptum://";

    public static readonly Uri DefaultUri = new(defaultUriScheme);

    private static readonly ObservableCollection<IResource> resources = [];

    public static ObservableCollection<IResource> Resources { get => resources; }

    public static Uri GetAbsoluteUri(string path) => new(defaultUriScheme + path);

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

    private static readonly Dictionary<string, List<TaskCompletionSource<IResource?>>> dependencyLinks = [];

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

    static int resolutions = 0;
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
