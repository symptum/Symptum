using Symptum.Common.ProjectSystem;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects;

namespace Symptum.Editor.Common;

public class NewItemType(string displayName, Type type, string? groupName = null)
{
    public string DisplayName { get; set; } = displayName;

    public Type Type { get; set; } = type;

    public string? GroupName { get; set; } = groupName;

    public static List<NewItemType> KnownTypes { get; } =
    [
        new("Folder", typeof(ProjectFolder), "Common"),
        new("Subject", typeof(Subject), "Subjects"),
        new("Category", typeof(CategoryResource), "Common"),
        new("Image Category", typeof(ImageCategoryResource), "Common"),
        new("Markdown Category", typeof(MarkdownCategoryResource), "Common"),
        new("Image File", typeof(ImageFileResource), "Common"),
        new("Markdown File", typeof(MarkdownFileResource), "Common"),
        new("Reference Values Package", typeof(ReferenceValuesPackage), "Reference Values"),
        new("Reference Value Family", typeof(ReferenceValueFamily), "Reference Values"),
        new("Reference Value Group", typeof(ReferenceValueGroup), "Reference Values"),
    ];
}
