using System.Collections.Concurrent;
using Symptum.Core.Management.Resources;

namespace Symptum.Core.Management.Deployment;

public class PackageManager
{
    private static Func<string, Task<IPackageResource?>>? _loadPackageCallback;

    public static void Initialize(Func<string, Task<IPackageResource?>> loadPackageCallback) =>
        Interlocked.Exchange(ref _loadPackageCallback, loadPackageCallback);

    #region Dependency Resolution

    private static readonly ConcurrentDictionary<string, TaskCompletionSource<IPackageResource?>> dependencyLinks = new();

    public static void ResolveDependencies(IPackageResource? package)
    {
        if (package == null || package.DependencyIds == null || package.DependencyIds.Count == 0)
            return;

        package.Dependencies ??= [];

        foreach (var dependencyId in package.DependencyIds)
        {
            _ = ResolveDependencyAsync(package, dependencyId);
        }
    }

    private static async Task ResolveDependencyAsync(IPackageResource? package, string dependencyId)
    {
        IPackageResource? dependency = await GetDependencyAsync(dependencyId);
        if (dependency == null)
            return;

        if (package?.Dependencies is IList<IPackageResource> dependencies)
        {
            lock (dependencies)
            {
                if (!dependencies.Contains(dependency))
                    dependencies.Add(dependency);
            }
        }
    }

    private static Task<IPackageResource?> GetDependencyAsync(string id)
    {
        var taskCompletionSource = dependencyLinks.GetOrAdd(id, _ => new TaskCompletionSource<IPackageResource?>(TaskCreationOptions.RunContinuationsAsynchronously));
        return taskCompletionSource.Task;
    }

    // Only need to call this method after loading all the primary local packages.
    // Then it will be called again automatically after loading the dependencies.
    public static void StartDependencyResolution()
    {
        var pendingIds = dependencyLinks.Keys.ToArray();
        foreach (var id in pendingIds)
        {
            if (ResourceManager.Resources.FirstOrDefault(x => x.Id == id) is IPackageResource dependency)
            {
                if (dependencyLinks.TryRemove(id, out var taskCompletionSource))
                {
                    taskCompletionSource.TrySetResult(dependency);
                }
            }
            else
            {
                _ = LoadDependencyAsync(id);
            }
        }
    }

    private static int loadWaits = 0;

    private static async Task LoadDependencyAsync(string id)
    {
        var loadPackageCallback = Volatile.Read(ref _loadPackageCallback);
        if (loadPackageCallback == null)
            return;

        Interlocked.Increment(ref loadWaits);

        // This will call Symptum.Common.Helpers.PackageHelper.LoadPackageAsync(string packageId)
        // PackageHelper will be responsible for downloading, caching or loading a package from cache
        var package = await loadPackageCallback(id);
        if (package != null)
        {
            if (dependencyLinks.TryRemove(id, out var taskCompletionSource))
            {
                taskCompletionSource.TrySetResult(package);
            }

            ResolveDependencies(package);
        }

        if (Interlocked.Decrement(ref loadWaits) == 0)
        {
            StartDependencyResolution();
        }
    }

    #endregion
}
