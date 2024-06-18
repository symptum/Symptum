using System.Collections.ObjectModel;
using Symptum.Core.Extensions;
using Symptum.Core.Management.Resources;
using Symptum.Core.Serialization;

namespace Symptum.Core.Subjects.QuestionBanks;

public class QuestionBank : MetadataResource
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

    private ObservableCollection<QuestionBankPaper>? papers;

    [ListOfMetadataResource]
    public ObservableCollection<QuestionBankPaper>? Papers
    {
        get => papers;
        set
        {
            UnobserveCollection(papers);
            SetProperty(ref papers, value);
            SetChildrenResources(papers);
        }
    }

    #endregion

    protected override void OnInitializeResource(IResource? parent)
    {
        SetChildrenResources(papers);
    }

    public override bool CanHandleChildResourceType(Type childResourceType)
    {
        return childResourceType == typeof(QuestionBankPaper);
    }

    public override bool CanAddChildResourceType(Type childResourceType)
    {
        return childResourceType == typeof(QuestionBankPaper);
    }

    protected override void OnAddChildResource(IResource? childResource)
    {
        Papers ??= [];
        if (childResource is QuestionBankPaper paper)
            Papers?.Add(paper);
    }

    protected override void OnRemoveChildResource(IResource? childResource)
    {
        Papers.RemoveItemFromListIfExists(childResource);
    }
}
