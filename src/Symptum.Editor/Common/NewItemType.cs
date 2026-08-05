using Symptum.Common.ProjectSystem;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects;

namespace Symptum.Editor.Common;

public class NewItemType(string displayName, Type type, string? groupName = null, Func<object>? instantiator = null)
{
    public string DisplayName { get; set; } = displayName;

    public string? GroupName { get; set; } = groupName;

    public Type Type { get; set; } = type;

    public Func<object>? Instantiator { get; set; } = instantiator;

    public static List<NewItemType> KnownTypes { get; } =
    [
        new("Folder", typeof(ProjectFolder), "Common", () => new ProjectFolder()),
        new("Subject", typeof(Subject), "Subjects", () => new Subject()),
        new("Category", typeof(CategoryResource), "Common", () => new CategoryResource()),
        new("Image Category", typeof(ImageCategoryResource), "Common", () => new ImageCategoryResource()),
        new("Markdown Category", typeof(MarkdownCategoryResource), "Common", () => new MarkdownCategoryResource()),
        new("Image File", typeof(ImageFileResource), "Common", () => new ImageFileResource()),
        new("Markdown File", typeof(MarkdownFileResource), "Common", () => new MarkdownFileResource()),
        new("Reference Values Package", typeof(ReferenceValuesPackage), "Reference Values", () => new ReferenceValuesPackage()),
        new("Reference Value Family", typeof(ReferenceValueFamily), "Reference Values", () => new ReferenceValueFamily()),
        new("Reference Value Group", typeof(ReferenceValueGroup), "Reference Values", () => new ReferenceValueGroup()),
    ];
}
