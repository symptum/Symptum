using Symptum.Core.Management.Resources;
using Symptum.Core.Serialization;
using Symptum.Core.Subjects.QuestionBanks;

namespace Symptum.Core.Subjects;

public class Subject : PackageResource
{
    public Subject()
    { }

    public Subject(string title)
    {
        Title = title;
    }

    public Subject(SubjectList subjectCode)
    {
        SubjectCode = subjectCode;
    }

    #region Properties

    private SubjectList _subjectCode;

    public SubjectList SubjectCode
    {
        get => _subjectCode;
        set => SetProperty(ref _subjectCode, value);
    }

    private QuestionBank? questionBank;

    [MetadataResource]
    public QuestionBank? QuestionBank
    {
        get => questionBank;
        set
        {
            UpdateChildrenResources(value);
            SetProperty(ref questionBank, value);
        }
    }

    #endregion

    protected override void OnInitializeResource(IResource? parent)
    {
        if (questionBank != null)
            AddChildResourceInternal(questionBank);
    }

    public override bool CanHandleChildResourceType(Type childResourceType)
    {
        return childResourceType == typeof(QuestionBank);
    }

    public override bool CanAddChildResourceType(Type childResourceType)
    {
        if (childResourceType == typeof(QuestionBank))
        {
            return QuestionBank == null;
        }

        return false;
    }

    protected override void OnAddChildResource(IResource? childResource)
    {
        if (childResource is QuestionBank _qb)
            QuestionBank = _qb;
    }

    protected override void OnRemoveChildResource(IResource? childResource)
    {
        if (childResource is QuestionBank)
            QuestionBank = null;
    }

    private void UpdateChildrenResources(QuestionBank? value)
    {
        if (ChildrenResources != null)
        {
            if (questionBank != null && ChildrenResources.Contains(questionBank))
                RemoveChildResourceInternal(questionBank);
            if (value != null)
                AddChildResourceInternal(value);
        }
    }
}
