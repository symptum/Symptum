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

    private QuestionBank questionBank;

    public QuestionBank QuestionBank
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
        ChildrenResources = [questionBank];
        foreach (IResource resource in ChildrenResources)
        {
            resource.InitializeResource(this);
        }
    }

    public override bool CanHandleChildResourceType(Type childResourceType)
    {
        return childResourceType == typeof(QuestionBank);
    }

    private void UpdateChildrenResources(QuestionBank value)
    {
        if (ChildrenResources != null)
        {
            if (value != null)
            {
                if (ChildrenResources.Contains(questionBank))
                    ChildrenResources.Remove(questionBank);

                ChildrenResources.Add(value);
            }
            else
                ChildrenResources.Remove(questionBank);
        }
    }
}