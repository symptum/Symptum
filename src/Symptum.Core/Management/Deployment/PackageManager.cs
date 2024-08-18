using Symptum.Core.Management.Resources;
using System.Collections.ObjectModel;

namespace Symptum.Core.Management.Deployment;

public class PackageManager
{
    private static Func<string, Task<IPackageResource?>>? _loadPackageCallback;

    public static void Initialize(Func<string, Task<IPackageResource?>> loadPackageCallback)
    {
        _loadPackageCallback = loadPackageCallback;
    }

    #region Dependency Resolution

    private static readonly Dictionary<string, List<TaskCompletionSource<IPackageResource?>>> dependencyLinks = [];

    // TODO: Make thread safe
    public static void ResolveDependencies(IPackageResource? package)
    {
        if (package != null && package.DependencyIds != null)
        {
            ObservableCollection<IPackageResource>? dependencies = [];
            package.Dependencies = dependencies;
            foreach (var dependencyId in package.DependencyIds)
            {
                ResolveDependencyAsync(package, dependencyId);
            }
        }
    }

    private static async void ResolveDependencyAsync(IPackageResource? package, string dependencyId)
    {
        IPackageResource? dependency = await GetDependencyAsync(dependencyId);
        if (dependency != null)
            package?.Dependencies?.Add(dependency);
    }

    private static async Task<IPackageResource?> GetDependencyAsync(string id)
    {
        TaskCompletionSource<IPackageResource?> taskCompletionSource = new();
        if (!dependencyLinks.TryGetValue(id, out List<TaskCompletionSource<IPackageResource?>>? tasks))
        {
            tasks = [];
            dependencyLinks.Add(id, tasks);
        }

        tasks.Add(taskCompletionSource);
        var package = await taskCompletionSource.Task;
        return package;
    }

    // Only need to call this method after loading all the primary local packages.
    // Then it will be called again automatically after loading the dependencies.
    public static void StartDependencyResolution()
    {
        List<string> finishedIds = [];
        foreach (var pair in dependencyLinks)
        {
            var id = pair.Key;
            var tasks = pair.Value;
            if (ResourceManager.Resources.FirstOrDefault(x => x.Id == id) is IPackageResource dependency)
            {
                foreach (var task in tasks)
                {
                    task.SetResult(dependency);
                }
                tasks.Clear();
                finishedIds.Add(id);
            }
            else
                LoadDependencyAsync(id);
        }
        foreach (var id in finishedIds)
        {
            dependencyLinks.Remove(id);
        }
        finishedIds.Clear();
    }

    // idk what or how this works, made sense to me
    private static int loadWaits = 0;

    private static async void LoadDependencyAsync(string id)
    {
        if (_loadPackageCallback == null) return;

        loadWaits++;

        // This will call Symptum.Common.Helpers.PackageHelper.LoadPackageAsync(string packageId)
        // PackageHelper will be responsible for downloading, caching or loading a package from cache
        var package = await _loadPackageCallback(id);
        if (package != null)
        {
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
        if (loadWaits == 0) // Resolve dependencies of the newly loaded packages after loading all the packages
        {
            StartDependencyResolution();
            _ = id;
        }
    }

    #endregion
}
