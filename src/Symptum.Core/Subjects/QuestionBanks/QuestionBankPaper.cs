using System.Collections.ObjectModel;
using Symptum.Core.Extensions;
using Symptum.Core.Management.Resources;

namespace Symptum.Core.Subjects.QuestionBanks;

public class QuestionBankPaper : MetadataResource
{
    public QuestionBankPaper()
    { }

    public QuestionBankPaper(string title)
    {
        Title = title;
    }

    #region Properties

    private ObservableCollection<QuestionBankTopic>? topics;

    public ObservableCollection<QuestionBankTopic>? Topics
    {
        get => topics;
        set
        {
            UnobserveCollection(topics);
            SetProperty(ref topics, value);
            SetChildrenResources(topics);
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

    public override bool CanAddChildResourceType(Type childResourceType)
    {
        return childResourceType == typeof(QuestionBankTopic);
    }

    protected override void OnAddChildResource(IResource? childResource)
    {
        Topics ??= [];
        if (childResource is QuestionBankTopic topic)
            Topics.Add(topic);
    }

    protected override void OnRemoveChildResource(IResource? childResource)
    {
        Topics.RemoveItemFromListIfExists(childResource);
    }
}
