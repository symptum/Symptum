using System.Collections.ObjectModel;
using Markdig;
using Markdig.Syntax;
using Symptum.Core.Data;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Management.Resources;
using Symptum.Markdown.Reference;

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

    [TestMethod]
    public void GetOptimizedMarkdown_ReferenceInline_ResolvesToHyperlink()
    {
        var resource = CreateMarkdownResourceWithReferenceGroup();
        string input = "The normal range is @PH#0.1.";
        string expected = "The normal range is [7.35 pH](symptum://referencevalues/test?PH#0.1).";
        string result = MarkdownManager.GetOptimizedMarkdown(input, resource);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ReferenceInline_OptionalIndices_UseFirstEntryAndQuantity()
    {
        var resource = CreateMarkdownResourceWithReferenceGroup();
        string input = "Value is @PH.";
        string expected = "Value is [7.4 pH](symptum://referencevalues/test?PH#0.0).";
        string result = MarkdownManager.GetOptimizedMarkdown(input, resource);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ReferenceInline_OptionalEntryIndex_UsesFirstEntry()
    {
        var resource = CreateMarkdownResourceWithReferenceGroup();
        string input = "Minimum is @PH.2.";
        string expected = "Minimum is [7.45 pH](symptum://referencevalues/test?PH#0.2).";
        string result = MarkdownManager.GetOptimizedMarkdown(input, resource);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ReferenceInline_MultipleReferences_AllResolved()
    {
        var resource = CreateMarkdownResourceWithReferenceGroup();
        string input = "Ranges: @PH#0.0 - @PH#0.1 - @PH#0.2.";
        string expected = "Ranges: [7.4 pH](symptum://referencevalues/test?PH#0.0) - [7.35 pH](symptum://referencevalues/test?PH#0.1) - [7.45 pH](symptum://referencevalues/test?PH#0.2).";
        string result = MarkdownManager.GetOptimizedMarkdown(input, resource);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ReferenceInline_UnknownParameter_KeepsOriginal()
    {
        var resource = CreateMarkdownResourceWithReferenceGroup();
        string input = "Value is @UNKNOWN#0.1.";
        string result = MarkdownManager.GetOptimizedMarkdown(input, resource);
        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ReferenceInline_NoResource_KeepsOriginal()
    {
        string input = "Value is @PH#0.1.";
        string result = MarkdownManager.GetOptimizedMarkdown(input);
        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ReferenceInline_OutOfRangeIndex_FallsBackToParameterTitle()
    {
        var resource = CreateMarkdownResourceWithReferenceGroup();
        string input = "Value is @PH#5.0.";
        string expected = "Value is [pH Level](symptum://referencevalues/test?PH#5.0).";
        string result = MarkdownManager.GetOptimizedMarkdown(input, resource);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ReferenceInline_InvalidSyntax_KeepsOriginal()
    {
        var resource = CreateMarkdownResourceWithReferenceGroup();
        string input = "Empty @ and @#0.1 and @.1.";
        string result = MarkdownManager.GetOptimizedMarkdown(input, resource);
        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ReferenceInline_SuffixWithoutDigits_ResolvesReferenceAndKeepsSuffixText()
    {
        var resource = CreateMarkdownResourceWithReferenceGroup();
        string input = "A: @PH#.1, B: @PH.#, C: @PH#.";
        string expected = "A: [7.4 pH](symptum://referencevalues/test?PH#0.0)#.1, B: [7.4 pH](symptum://referencevalues/test?PH#0.0).#, C: [7.4 pH](symptum://referencevalues/test?PH#0.0)#.";
        string result = MarkdownManager.GetOptimizedMarkdown(input, resource);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ReferenceInline_NotAtStartOfWord_KeepsOriginal()
    {
        var resource = CreateMarkdownResourceWithReferenceGroup();
        string input = "Email is foo@PH#0.1.";
        string result = MarkdownManager.GetOptimizedMarkdown(input, resource);
        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void ReferenceInlineParser_RecognizesReferenceAndIndices()
    {
        var document = Markdig.Markdown.Parse("Use @PH#0.1 for pH.", MarkdownManager.Pipeline);
        var reference = document.Descendants<ReferenceInline>().FirstOrDefault();
        Assert.IsNotNull(reference);
        Assert.IsTrue(ReferenceInlineHelper.TryParse(reference.Content.ToString(), out string? parameterId, out int entryIndex, out int quantityIndex));
        Assert.AreEqual("PH", parameterId);
        Assert.AreEqual(0, entryIndex);
        Assert.AreEqual(1, quantityIndex);
    }

    [TestMethod]
    public void ReferenceInlineParser_TrailingPeriod_ResolvesReferenceAndKeepsPeriod()
    {
        var document = Markdig.Markdown.Parse("Value is @PH.", MarkdownManager.Pipeline);
        var reference = document.Descendants<ReferenceInline>().FirstOrDefault();
        Assert.IsNotNull(reference);
        Assert.IsTrue(ReferenceInlineHelper.TryParse(reference.Content.ToString(), out string? parameterId, out int entryIndex, out int quantityIndex));
        Assert.AreEqual("PH", parameterId);
        Assert.AreEqual(0, entryIndex);
        Assert.AreEqual(0, quantityIndex);
        Assert.AreEqual("@PH", reference.Content.ToString());
    }

    [TestMethod]
    public void ReferenceInlineParser_NotPrecededByWhitespaceOrPunctuation_NotRecognized()
    {
        var document = Markdig.Markdown.Parse("Email is foo@PH#0.1.", MarkdownManager.Pipeline);
        Assert.IsFalse(document.Descendants<ReferenceInline>().Any());
    }

    [TestMethod]
    public void ReferenceInlineParser_EscapedAt_NotRecognized()
    {
        var document = Markdig.Markdown.Parse("Value is \\@PH.", MarkdownManager.Pipeline);
        Assert.IsFalse(document.Descendants<ReferenceInline>().Any());
    }

    [TestMethod]
    public void ReferenceInlineHelper_BuildSyntax_RoundTrips()
    {
        string syntax = ReferenceInlineHelper.BuildSyntax("PH", 1, 2);
        Assert.AreEqual("@PH#1.2", syntax);
        Assert.IsTrue(ReferenceInlineHelper.TryParse(syntax, out string? parameterId, out int entryIndex, out int quantityIndex));
        Assert.AreEqual("PH", parameterId);
        Assert.AreEqual(1, entryIndex);
        Assert.AreEqual(2, quantityIndex);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ReferenceInline_PunctuationAfterParameterId_ResolvesAndKeepsPunctuation()
    {
        var resource = CreateMarkdownResourceWithReferenceGroup();
        string input = "The value is @PH, which is normal.";
        string expected = "The value is [7.4 pH](symptum://referencevalues/test?PH#0.0), which is normal.";
        string result = MarkdownManager.GetOptimizedMarkdown(input, resource);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetOptimizedMarkdown_ReferenceInline_QuantityOnlySuffix_ResolvesQuantity()
    {
        var resource = CreateMarkdownResourceWithReferenceGroup();
        string input = "The low value is @PH.1.";
        string expected = "The low value is [7.35 pH](symptum://referencevalues/test?PH#0.1).";
        string result = MarkdownManager.GetOptimizedMarkdown(input, resource);
        Assert.AreEqual(expected, result);
    }

    private static MarkdownFileResource CreateMarkdownResourceWithReferenceGroup()
    {
        ReferenceValueGroup group = new("Test Group")
        {
            Id = "Symptum.TestGroup",
            Uri = new Uri("symptum://referencevalues/test"),
            Parameters =
            [
                new()
                {
                    Id = "PH",
                    Title = "pH Level",
                    Entries =
                    [
                        new()
                        {
                            Title = "Normal",
                            Quantities = [new(7.4, "pH"), new(7.35, "pH"), new(7.45, "pH")]
                        },
                        new()
                        {
                            Title = "Low",
                            Quantities = [new(6.5, "pH")]
                        }
                    ]
                }
            ]
        };

        return new MarkdownFileResource
        {
            Id = "Symptum.TestMarkdown",
            Dependencies = [group]
        };
    }
}
