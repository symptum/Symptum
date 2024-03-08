using System.Collections.ObjectModel;
using System.Xml.Serialization;
using Symptum.Core.Management.Resources;

namespace Symptum.Core.Subjects.QuestionBanks;

public class QuestionBank : NavigableResource
{
    public QuestionBank()
    { }

    #region Properties

    private SubjectList _subjectCode;

    public SubjectList SubjectCode
    {
        get => _subjectCode;
        set => SetProperty(ref _subjectCode, value);
    }

    private ObservableCollection<QuestionBankPaper>? questionBankPapers;

    public ObservableCollection<QuestionBankPaper>? QuestionBankPapers
    {
        get => questionBankPapers;
        set
        {
            UnobserveCollection(questionBankPapers);
            SetProperty(ref questionBankPapers, value);
            SetChildrenResources(questionBankPapers);
        }
    }

    #endregion

    protected override void OnInitializeResource(IResource? parent)
    {
        SetChildrenResources(questionBankPapers);
    }

    public override bool CanHandleChildResourceType(Type childResourceType)
    {
        return childResourceType == typeof(QuestionBankPaper);
    }

    public override bool CanAddChildResourceType(Type childResourceType)
    {
        return childResourceType == typeof(QuestionBankPaper);
    }

    protected override void OnAddChildResource(IResource childResource)
    {
        QuestionBankPapers ??= [];
        if (childResource is QuestionBankPaper paper)
            questionBankPapers?.Add(paper);
    }

    protected override void OnRemoveChildResource(IResource childResource)
    {
        throw new NotImplementedException();
    }
}
