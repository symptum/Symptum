using Symptum.Core.Management.Deployment;
using Symptum.Core.Management.Resources;

namespace Symptum.Core.Tests;

[TestClass]
public class PackageManagerTests
{
    [TestMethod]
    public async Task ResolveDependencies_ConcurrentRequestsShareSingleResolutionResult()
    {
        var dependency = new TestPackageResource { Id = "dependency", Title = "Dependency" };
        var parent = new TestPackageResource
        {
            Id = "parent",
            Title = "Parent",
            DependencyIds = ["dependency"]
        };

        var loadCalls = 0;
        PackageManager.Initialize(id =>
        {
            Interlocked.Increment(ref loadCalls);
            return Task.FromResult<IPackageResource?>(dependency);
        });

        PackageManager.ResolveDependencies(parent);
        PackageManager.ResolveDependencies(parent);
        PackageManager.StartDependencyResolution();

        await Task.Delay(100);

        Assert.AreEqual(1, loadCalls);
        Assert.AreEqual(1, parent.Dependencies?.Count);
        Assert.AreSame(dependency, parent.Dependencies?[0]);
    }

    private sealed class TestPackageResource : PackageResource<MetadataResource> {}
}