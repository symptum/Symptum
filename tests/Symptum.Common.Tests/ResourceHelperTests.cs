using Symptum.Common.Helpers;

namespace Symptum.Common.Tests;

[TestClass]
public sealed class ResourceHelperTests
{
    [TestMethod]
    public async Task SaveTextFileAsync_WithTargetFolder_ReturnsTrue()
    {
        MarkdownFileResource md = new() { Title = "TestMd", Markdown = "Hello, World!" };
        string folderPath = "TestFolder";

        StorageFolder tempFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(folderPath, CreationCollisionOption.OpenIfExists);

        bool saved = await ResourceHelper.SaveResourceAsync(md, tempFolder);

        Assert.IsTrue(saved);
    }
}
