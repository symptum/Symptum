using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper.Configuration.Attributes;

namespace Symptum.Core.Subjects.Books;

public partial class Book : ObservableObject
{
    #region Properties

    [ObservableProperty]
    public partial string? Id { get; set; }

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial string? Authors { get; set; }

    [Ignore]
    [ObservableProperty]
    public partial SubjectList Subject { get; set; }

    #endregion

    public Book()
    {
    }

    public Book(string code, string title, string author)
    {
        Id = code;
        Title = title;
        Authors = author;
    }

    public override string ToString() => Title + " by " + Authors + " (" + Id + ")";
}
