using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper.Configuration.Attributes;

namespace Symptum.Core.Subjects.Books;

public class Book : ObservableObject
{
    #region Properties

    private string _id = string.Empty;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    private string _title = string.Empty;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private string _authors = string.Empty;

    public string Authors
    {
        get => _authors;
        set => SetProperty(ref _authors, value);
    }

    private SubjectList _subjectList;

    [Ignore]
    public SubjectList Subject
    {
        get => _subjectList;
        set => SetProperty(ref _subjectList, value);
    }

    #endregion

    public Book()
    {
    }

    public Book(string code, string title, string author)
    {
        _id = code;
        _title = title;
        _authors = author;
    }

    public override string ToString()
    {
        return Title + " by " + Authors + " (" + Id + ")";
    }
}
