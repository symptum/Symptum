using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper.Configuration.Attributes;
using Symptum.Core.Data.Bibliography;
using Symptum.Core.Extensions;
using Symptum.Core.TypeConversion;

namespace Symptum.Core.Subjects.QuestionBanks;

public class QuestionEntry : ObservableObject, IComparable, IComparable<QuestionEntry>
{
    public QuestionEntry()
    { }

    #region Properties

    private QuestionId? id;

    [TypeConverter(typeof(QuestionIdConverter))]
    public QuestionId? Id
    {
        get => id;
        set => SetProperty(ref id, value);
    }

    private string? title;

    public string? Title
    {
        get => title;
        set => SetProperty(ref title, value);
    }

    private List<string>? descriptions;

    [TypeConverter(typeof(StringListConverter))]
    public List<string>? Descriptions
    {
        get => descriptions;
        set => SetProperty(ref descriptions, value);
    }

    private bool hasPreviouslyBeenAsked;

    public bool HasPreviouslyBeenAsked
    {
        get => hasPreviouslyBeenAsked;
        set => SetProperty(ref hasPreviouslyBeenAsked, value);
    }

    private int importance = 0;

    public int Importance
    {
        get => importance;
        set => SetProperty(ref importance, value);
    }

    private List<DateOnly>? yearsAsked;

    [TypeConverter(typeof(DateOnlyListConverter))]
    public List<DateOnly>? YearsAsked
    {
        get => yearsAsked;
        set => SetProperty(ref yearsAsked, value);
    }

    private List<string>? probableCases;

    [TypeConverter(typeof(StringListConverter))]
    public List<string>? ProbableCases
    {
        get => probableCases;
        set => SetProperty(ref probableCases, value);
    }

    private List<ReferenceBase>? references;

    [TypeConverter(typeof(ReferenceListConverter))]
    public List<ReferenceBase>? References
    {
        get => references;
        set => SetProperty(ref references, value);
    }

    #endregion

    public QuestionEntry Clone()
    {
        return new QuestionEntry()
        {
            Id = id != null ? new() { QuestionType = id.QuestionType, SubjectCode = id.SubjectCode, CompetencyNumbers = id.CompetencyNumbers } : new(),
            Title = title,
            Descriptions = descriptions.CloneList(),
            HasPreviouslyBeenAsked = hasPreviouslyBeenAsked,
            Importance = importance,
            YearsAsked = yearsAsked.CloneList(),
            ProbableCases = probableCases.CloneList(),
            References = references.CloneList(x => x with { })
        };
    }

    public int CompareTo(QuestionEntry? other)
    {
        if (ReferenceEquals(this, other))
            return 0;
        else if (other == null)
            return 1;

        // Compare the QuestionType first
        int? cmp = Id?.QuestionType.CompareTo(other?.Id?.QuestionType);
        if (cmp != null && cmp != 0)
            return cmp.Value;

        // Then Importance is compared
        cmp = Importance.CompareTo(other?.Importance);
        if (cmp != null && cmp != 0)
            return -cmp.Value;

        // Then we compare questions with same Importance and QuestionType with Title
        cmp = Title?.CompareTo(other?.Title);
        if (cmp != null && cmp != 0)
            return cmp.Value;

        return 0;
    }

    public int CompareTo(object? obj)
    {
        if (obj != null && obj.GetType() != GetType())
        {
            throw new ArgumentException(string.Format("Object must be of type {0}", GetType()));
        }
        return CompareTo(obj as QuestionEntry);
    }
}

public class QuestionEntryO
{
    [TypeConverter(typeof(QuestionIdConverter))]
    public QuestionId Id { get; set; }

    public string Title { get; set; }

    [TypeConverter(typeof(StringListConverter))]
    public List<string> Descriptions { get; set; }

    public bool HasPreviouslyBeenAsked { get; set; }

    public int Importance {  get; set; }

    [TypeConverter(typeof(DateOnlyListConverter))]
    public List<DateOnly> YearsAsked { get; set; }

    [TypeConverter(typeof(StringListConverter))]
    public List<string> ProbableCases { get; set; }

    [TypeConverter(typeof(BookReferenceListConverter))]
    public List<BookReference> BookReferences { get; set; }

    [TypeConverter(typeof(UriListConverter))]
    public List<Uri> LinkReferences { get; set; }
}
