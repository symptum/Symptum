using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Core.Management.Resources;
using Symptum.Core.Serialization;

namespace Symptum.Core.Subjects;

public partial class Subject : PackageResource<MetadataResource, ISubjectCategoryResource>
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

    [ObservableProperty]
    public partial SubjectList SubjectCode { get; set; }

    [ListOfMetadataResource]
    public override ObservableCollection<MetadataResource>? Contents
    {
        get => base.Contents;
        set => base.Contents = value;
    }

    #endregion
}
