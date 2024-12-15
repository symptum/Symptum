using Symptum.Common.ProjectSystem;
using Symptum.Core.Data.Nutrition;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects;
using Symptum.Core.Subjects.QuestionBanks;

namespace Symptum.Editor.Common;

public class NewItemType
{
    public string? DisplayName { get; set; }

    public Type? Type { get; set; }

    public string? GroupName { get; set; }

    public NewItemType()
    { }

    public NewItemType(string displayName, Type type, string? groupName = null)
    {
        DisplayName = displayName;
        Type = type;
        GroupName = groupName;
    }

    public static List<NewItemType> KnownTypes { get; } =
    [
        new("Folder", typeof(ProjectFolder), "Common"),
        new("Subject", typeof(Subject), "Subjects"),
        new("Category", typeof(CategoryResource), "Common"),
        new("Image Category", typeof(ImageCategoryResource), "Common"),
        new("Markdown Category", typeof(MarkdownCategoryResource), "Common"),
        new("Image File", typeof(ImageFileResource), "Common"),
        new("Markdown File", typeof(MarkdownFileResource), "Common"),
        new("Question Bank", typeof(QuestionBank), "Question Banks"),
        new("Question Bank Paper", typeof(QuestionBankPaper), "Question Banks"),
        new("Question Bank Topic", typeof(QuestionBankTopic), "Question Banks"),
        new("Reference Values Package", typeof(ReferenceValuesPackage), "Reference Values"),
        new("Reference Value Family", typeof(ReferenceValueFamily), "Reference Values"),
        new("Reference Value Group", typeof(ReferenceValueGroup), "Reference Values"),
        new("Nutrition Package", typeof(NutritionPackage), "Nutrition"),
        new("Nutrition Data Set", typeof(NutritionDataSet), "Nutrition"),
        new("Food Group", typeof(FoodGroup), "Nutrition"),
    ];
}
