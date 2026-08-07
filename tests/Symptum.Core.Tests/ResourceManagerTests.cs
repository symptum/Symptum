using static Symptum.Core.Helpers.FileHelper;
using Symptum.Core.Management.Resources;

namespace Symptum.Core.Tests;

[TestClass]
public class ResourceManagerTests
{
    [TestMethod]
    public void GetAbsoluteFolderPath_IncludesAllAncestorsFromRootToLeaf()
    {
        var package = new TestPackageResource { Title = "Package", Id = "package" };
        var chapter = new TestResource { Title = "Chapter", Id = "package.chapter" };
        var topic = new TestResource { Title = "Topic", Id = "package.chapter.topic" };

        ((IResource)package).InitializeResource(null);
        package.AddChildResource(chapter);
        chapter.AddChildResource(topic);

        string path = ResourceManager.GetAbsoluteFolderPath(topic);

        Assert.AreEqual($"{PathSeparator}Package{PathSeparator}Chapter{PathSeparator}Topic{PathSeparator}", path);
    }

    [TestMethod]
    public void GetAbsoluteResourceFolderPath_StartsAtPackageAncestor()
    {
        var package = new TestPackageResource { Title = "Package", Id = "package" };
        var chapter = new TestResource { Title = "Chapter", Id = "package.chapter" };
        var topic = new TestResource { Title = "Topic", Id = "package.chapter.topic" };

        ((IResource)package).InitializeResource(null);
        package.AddChildResource(chapter);
        chapter.AddChildResource(topic);

        string path = ResourceManager.GetAbsoluteResourceFolderPath(topic);

        Assert.AreEqual($"{PathSeparator}Package{PathSeparator}Chapter{PathSeparator}", path);
    }

    [TestMethod]
    public void GetRelativeResourceFolderPath_UsesPackageAsRootForDirectChildren()
    {
        var package = new TestPackageResource { Title = "Package", Id = "package" };
        var chapter = new TestResource { Title = "Chapter", Id = "package.chapter" };

        ((IResource)package).InitializeResource(null);
        package.AddChildResource(chapter);

        string path = ResourceManager.GetRelativeResourceFolderPath(chapter);

        Assert.AreEqual($"{PathSeparator}Package{PathSeparator}", path);
    }

    [TestMethod]
    public void TryGetParentOfType_ReturnsNearestMatchingAncestor()
    {
        var package = new TestPackageResource { Title = "Package", Id = "package" };
        var chapter = new TestResource { Title = "Chapter", Id = "package.chapter" };
        var topic = new TestResource { Title = "Topic", Id = "package.chapter.topic" };

        ((IResource)package).InitializeResource(null);
        package.AddChildResource(chapter);
        chapter.AddChildResource(topic);

        bool found = ResourceManager.TryGetParentOfType(topic, out TestPackageResource? parent);

        Assert.IsTrue(found);
        Assert.AreSame(package, parent);
    }

    [TestMethod]
    public void TryGetAvailableChildResourceById_ReturnsProbableParentWhenExactMatchIsMissing()
    {
        var root = new TestResource { Title = "Root", Id = "root" };
        var child = new TestResource { Title = "Child", Id = "root.child" };
        var other = new TestResource { Title = "Other", Id = "root.child.other" };

        ((IResource)root).InitializeResource(null);
        root.AddChildResource(child);
        child.AddChildResource(other);

        bool found = ResourceManager.TryGetAvailableChildResourceById("root.child.leaf", [root], out IResource? resource);

        Assert.IsFalse(found);
        Assert.AreSame(child, resource);
    }

    [TestMethod]
    public void TryGetAvailableChildResourceByUri_ReturnsNullWhenNoMatchExists()
    {
        var root = new TestResource { Title = "Root", Id = "root", Uri = new Uri("symptum://root") };
        var child = new TestResource { Title = "Child", Id = "root.child", Uri = new Uri("symptum://root/child") };

        ((IResource)root).InitializeResource(null);
        root.AddChildResource(child);

        bool found = ResourceManager.TryGetAvailableChildResourceByUri(new Uri("symptum://missing"), [root], out IResource? resource);

        Assert.IsFalse(found);
        Assert.IsNull(resource);
    }

    [TestMethod]
    public void TryGetAvailableChildResourceById_DoesNotLoopWhenBranchHasNoChildren()
    {
        var root = new TestResource { Title = "Root", Id = "root" };
        var child = new TestResource { Title = "Child", Id = "root.child" };

        ((IResource)root).InitializeResource(null);
        root.AddChildResource(child);

        bool found = ResourceManager.TryGetAvailableChildResourceById("root.child.leaf", [root], out IResource? resource);

        Assert.IsFalse(found);
        Assert.AreSame(child, resource);
    }

    [TestMethod]
    public void ResolveDependencies_PopulatesDependenciesFromDependencyIds()
    {
        var dependency = new TestResource { Title = "Dependency", Id = "dependency" };
        var resource = new TestResource
        {
            Title = "Resource",
            Id = "resource",
            DependencyIds = ["dependency"]
        };

        ((IResource)dependency).InitializeResource(null);
        ((IResource)resource).InitializeResource(null);
        ResourceManager.Resources.Add(dependency);

        try
        {
            ResourceManager.ResolveDependencies(resource);

            Assert.IsNotNull(resource.Dependencies);
            Assert.HasCount(1, resource.Dependencies);
            Assert.AreSame(dependency, resource.Dependencies[0]);
        }
        finally
        {
            ResourceManager.Resources.Remove(dependency);
        }
    }

    [TestMethod]
    public void ResolveDependencies_WithoutDependencyIds_ClearsDependencies()
    {
        var resource = new TestResource
        {
            Title = "Resource",
            Id = "resource",
            Dependencies = [new TestResource { Title = "Stale", Id = "stale" }]
        };

        ResourceManager.ResolveDependencies(resource);

        Assert.IsTrue(resource.Dependencies == null || resource.Dependencies.Count == 0);
    }

    [TestMethod]
    public void WritePackageMetadata_WithDependencyIds_SerializesSingleDependenciesProperty()
    {
        var package = new TestPackageResource
        {
            Title = "Package",
            Id = "package",
            DependencyIds = ["Symptum.Dependency"]
        };

        string json = ResourceManager.WritePackageMetadata(package);

        Assert.IsFalse(string.IsNullOrEmpty(json));
        Assert.AreEqual(1, CountOccurrences(json, "\"Dependencies\""));
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private sealed class TestResource : MetadataResource
    {
        protected override void OnInitializeResource(IResource? parent)
        {
        }

        public override bool CanHandleChildResourceType(Type childResourceType) => typeof(TestResource).IsAssignableFrom(childResourceType);

        public override bool CanAddChildResourceType(Type childResourceType) => CanHandleChildResourceType(childResourceType);

        protected override void OnAddChildResource(IResource? childResource) => AddChildResourceInternal(childResource);

        protected override void OnRemoveChildResource(IResource? childResource) => RemoveChildResourceInternal(childResource);
    }

    private sealed class TestPackageResource : PackageResource<TestResource>
    {
    }
}
