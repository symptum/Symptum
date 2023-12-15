using System.Collections.ObjectModel;
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

    private ObservableCollection<QuestionBankPaper> questionBankPapers;

    public ObservableCollection<QuestionBankPaper> QuestionBankPapers
    {
        get => questionBankPapers;
        set
        {
            UnobserveCollection(questionBankPapers);
            SetProperty(ref questionBankPapers, value);
            ObserveCollection(questionBankPapers);
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
}
