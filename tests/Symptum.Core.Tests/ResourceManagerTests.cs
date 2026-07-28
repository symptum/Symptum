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
