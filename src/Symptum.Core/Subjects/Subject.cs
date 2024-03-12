using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects.QuestionBanks;

namespace Symptum.Core.Subjects;

public class Subject : PackageResource
{
    public Subject()
    { }

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
            ChildrenResources?.Add(questionBank);
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

    protected override void OnAddChildResource(IResource childResource)
    {
        if (childResource is QuestionBank questionBank)
        {
            QuestionBank = questionBank;
        }
    }

    protected override void OnRemoveChildResource(IResource childResource)
    {
        throw new NotImplementedException();
    }

    private void UpdateChildrenResources(QuestionBank? value)
    {
        if (ChildrenResources != null)
        {
            if (questionBank != null && ChildrenResources.Contains(questionBank))
                ChildrenResources.Remove(questionBank);
            if (value != null)
                ChildrenResources.Add(value);
        }
    }
}
