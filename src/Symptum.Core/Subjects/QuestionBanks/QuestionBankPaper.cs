using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Symptum.Core.Management.Resources;

namespace Symptum.Core.Subjects.QuestionBanks;

public class QuestionBankPaper : CategoryResource<QuestionBankTopic>
{
    public QuestionBankPaper()
    { }

    public QuestionBankPaper(string title)
    {
        Title = title;
    }

    #region Properties

    [JsonPropertyName("Topics")]
    public override ObservableCollection<QuestionBankTopic>? Items
    {
        get => base.Items;
        set => base.Items = value;
    }

    #endregion
}
