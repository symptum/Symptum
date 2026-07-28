using Symptum.Core.Management.Resources;

namespace Symptum.Markdown.Tests;

[TestClass]
public sealed class MarkdownManagerTests
{
    [TestMethod]
    public void GetOptimizedMarkdown_NullInput_ReturnsNull()
    {
        string? result = MarkdownManager.GetOptimizedMarkdown(null!);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_EmptyInput_ReturnsEmpty()
    {
        string result = MarkdownManager.GetOptimizedMarkdown(string.Empty);
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_NoExportImport_ReturnsSame()
    {
        string input = "# Heading\n\nSome paragraph with **bold** text.\n\n- List item 1\n- List item 2";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ExportBlock_SingleLine_RemovesWrapper()
    {
        string input = "<= greeting\nHello, World!\n<=";
        string expected = "Hello, World!";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ExportBlock_MultiLine_RemovesWrapper()
    {
        string input = "<= note\nFirst line\nSecond line\nThird line\n<=";
        string expected = "First line\nSecond line\nThird line";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ExportBlock_NoContent_RemovesBlock()
    {
        string input = "<= empty\n<=";
        string expected = "";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ExportBlock_NoId_RemovesWrapper()
    {
        string input = "<=\nJust some content\n<=";
        string expected = "Just some content";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_MultipleExportBlocks_AllProcessed()
    {
        string input = "<= first\nContent A\n<=\n\n<= second\nContent B\n<=";
        string expected = "Content A\n\nContent B";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ImportBlock_ResolvesLocalExport()
    {
        string input = "<= greeting\nHello, World!\n<=\n\n=> greeting";
        string expected = "Hello, World!\n\nHello, World!";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ImportBlock_Unresolved_KeepsOriginal()
    {
        string input = "=> unknown";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ImportBeforeExport_StillResolves()
    {
        string input = "=> greeting\n\n<= greeting\nHello, World!\n<=";
        string expected = "Hello, World!\n\nHello, World!";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_MultipleImportBlocks_AllResolved()
    {
        string input = """
<= a
Content A
<=

<= b
Content B
<=

=> a

=> b
""";
        string expected = """
Content A

Content B

Content A

Content B
""";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_MixedContentWithExportAndImport()
    {
        string input = """
# Document Title

<= excerpt
This is the **excerpt** with `code`.
<=

Some regular paragraph here.

=> excerpt

Another paragraph at the end.
""";

        string expected = """
# Document Title

This is the **excerpt** with `code`.

Some regular paragraph here.

This is the **excerpt** with `code`.

Another paragraph at the end.
""";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ExportBlock_PreservesIndentation()
    {
        string input = "<= code\n    indented line\n    another\n<=";
        string expected = "    indented line\n    another";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_WithWindowsLineEndings()
    {
        string input = "<= id\r\nContent line\r\n<=\r\n";
        string expected = "Content line";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ImportBlock_ResolvesExternalResource()
    {
        var resource = new MarkdownFileResource
        {
            Id = "Symptum.TestLibrary",
            Markdown = "<= myBlock\nExported from resource\n<="
        };
        ResourceManager.Resources.Add(resource);

        try
        {
            string input = "=> Symptum.TestLibrary?myBlock";
            string expected = "Exported from resource";
            string result = MarkdownManager.GetOptimizedMarkdown(input);
            Assert.AreEqual(expected, result);
        }
        finally
        {
            ResourceManager.Resources.Remove(resource);
        }
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ImportBlock_ExternalResourceNotFound_KeepsOriginal()
    {
        string input = "=> NonExistent.Resource?someBlock";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ImportBlock_ExternalBlockNotFound_KeepsOriginal()
    {
        var resource = new MarkdownFileResource
        {
            Id = "Symptum.TestLibrary",
            Markdown = "<= myBlock\nExported content\n<="
        };
        ResourceManager.Resources.Add(resource);

        try
        {
            string input = "=> Symptum.TestLibrary?otherBlock";
            string result = MarkdownManager.GetOptimizedMarkdown(input);
            Assert.AreEqual(input, result);
        }
        finally
        {
            ResourceManager.Resources.Remove(resource);
        }
    }

    [TestMethod]
    public void GetOptimizedMarkdown_Import_SameIdInLocalAndExternal_UsesLocal()
    {
        var resource = new MarkdownFileResource
        {
            Id = "Symptum.TestLibrary",
            Markdown = "<= shared\nExternal content\n<="
        };
        ResourceManager.Resources.Add(resource);

        try
        {
            string input = """
<= shared
Local content
<=

=> shared
""";
            string expected = "Local content\n\nLocal content";
            string result = MarkdownManager.GetOptimizedMarkdown(input);
            Assert.AreEqual(expected, result);
        }
        finally
        {
            ResourceManager.Resources.Remove(resource);
        }
    }

    [TestMethod]
    public void GetOptimizedMarkdown_Import_ExplicitExternal_PrefersExternal()
    {
        var resource = new MarkdownFileResource
        {
            Id = "Symptum.TestLibrary",
            Markdown = "<= shared\nExternal content\n<="
        };
        ResourceManager.Resources.Add(resource);

        try
        {
            string input = """
<= shared
Local content
<=

=> Symptum.TestLibrary?shared
""";
            string expected = "Local content\n\nExternal content";
            string result = MarkdownManager.GetOptimizedMarkdown(input);
            Assert.AreEqual(expected, result);
        }
        finally
        {
            ResourceManager.Resources.Remove(resource);
        }
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ConsecutiveExportBlocks()
    {
        string input = "<= a\nA\n<=\n<= b\nB\n<=";
        string expected = "A\nB";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ExportBlockAtBeginning()
    {
        string input = "<= top\nTop content\n<=\n\nRegular text";
        string expected = "Top content\n\nRegular text";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ExportBlockAtEnd()
    {
        string input = "Regular text\n\n<= end\nEnd content\n<=";
        string expected = "Regular text\n\nEnd content";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(expected, result);
    }
}
