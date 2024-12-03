using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Core.Management.Resources;
using Symptum.Core.Serialization;

namespace Symptum.Core.Subjects.QuestionBanks;

public partial class QuestionBank : CategoryResource<QuestionBankPaper>, ISubjectCategoryResource
{
    public QuestionBank()
    { }

    #region Properties

    [ObservableProperty]
    public partial SubjectList SubjectCode { get; set; }

    [JsonPropertyName("Papers")]
    [ListOfMetadataResource]
    public override ObservableCollection<QuestionBankPaper>? Items
    {
        get => base.Items;
        set => base.Items = value;
    }

    #endregion
}
