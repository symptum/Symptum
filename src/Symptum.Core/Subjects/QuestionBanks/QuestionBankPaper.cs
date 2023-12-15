using System.Collections.ObjectModel;
using Symptum.Core.Management.Resources;

namespace Symptum.Core.Subjects.QuestionBanks;

public class QuestionBankPaper : NavigableResource
{
    public QuestionBankPaper()
    { }

    public QuestionBankPaper(string title)
    {
        Title = title;
    }

    #region Properties

    private ObservableCollection<QuestionBankTopic> topics;

    public ObservableCollection<QuestionBankTopic> Topics
    {
        get => topics;
        set
        {
            UnobserveCollection(topics);
            SetProperty(ref topics, value);
            ObserveCollection(topics);
        }
    }

    #endregion

    protected override void OnInitializeResource(IResource? parent)
    {
        SetChildrenResources(topics);
    }

    public override bool CanHandleChildResourceType(Type childResourceType)
    {
        return childResourceType == typeof(QuestionBankTopic);
    }
}
