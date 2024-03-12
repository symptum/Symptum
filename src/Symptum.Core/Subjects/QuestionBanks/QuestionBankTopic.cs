using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json.Serialization;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects.Books;
using Symptum.Core.TypeConversion;

namespace Symptum.Core.Subjects.QuestionBanks;

public class QuestionBankTopic : NavigableResource
{
    public QuestionBankTopic()
    { }

    public QuestionBankTopic(string title)
    {
        Title = title;
    }

    #region Properties

    private ObservableCollection<QuestionEntry>? questionEntries;

    [JsonIgnore]
    public ObservableCollection<QuestionEntry>? QuestionEntries
    {
        get => questionEntries;
        set => SetProperty(ref questionEntries, value);
    }

    #endregion

    protected override void OnInitializeResource(IResource? parent)
    {
    }

    public override bool CanHandleChildResourceType(Type childResourceType)
    {
        return false;
    }

    public override bool CanAddChildResourceType(Type childResourceType)
    {
        return false;
    }

    protected override void OnAddChildResource(IResource childResource)
    {
        throw new NotImplementedException();
    }

    protected override void OnRemoveChildResource(IResource childResource)
    {
        throw new NotImplementedException();
    }

    public string ToCSV()
    {
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteHeader<QuestionEntry>();
        csv.NextRecord();
        if (QuestionEntries != null)
        {
            foreach (var entry in QuestionEntries)
            {
                csv.WriteRecord(entry);
                csv.NextRecord();
            }
        }

        return writer.ToString();
    }

    public static QuestionBankTopic CreateTopicFromCSV(string topicName, string csv)
    {
        if (string.IsNullOrEmpty(csv)) return null;

        QuestionBankTopic topic = new(topicName)
        {
            QuestionEntries = LoadQuestionEntriesFromCSV(csv)
        };

        return topic;
    }

    public static ObservableCollection<QuestionEntry>? LoadQuestionEntriesFromCSV(string csv)
    {
        if (string.IsNullOrEmpty(csv)) return null;

        using var reader = new StringReader(csv);
        using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
        ObservableCollection<QuestionEntry> questionEntries = new(csvReader.GetRecords<QuestionEntry>().ToList());
        return questionEntries;
    }

    #region Upgrading QuestionEntry

    //public static string UpgradeCSV(string csv)
    //{
    //    if (string.IsNullOrEmpty(csv)) return null;

    //    using var reader = new StringReader(csv);
    //    using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
    //    List<QuestionEntry1> oldQuestionEntries = new(csvReader.GetRecords<QuestionEntry1>().ToList());
    //    List<QuestionEntry> questionEntries = MigrateEntries(oldQuestionEntries);

    //    using var writer = new StringWriter();
    //    using var csvW = new CsvWriter(writer, CultureInfo.InvariantCulture);
    //    csvW.WriteHeader<QuestionEntry>();
    //    csvW.NextRecord();
    //    if (questionEntries != null)
    //    {
    //        foreach (var entry in questionEntries)
    //        {
    //            csvW.WriteRecord(entry);
    //            csvW.NextRecord();
    //        }
    //    }

    //    return writer.ToString();
    //}

    //private static List<QuestionEntry> MigrateEntries(List<QuestionEntry1> entries)
    //{
    //    List<QuestionEntry> newEntries = [];
    //    foreach (var entry in entries)
    //    {
    //        QuestionEntry entry1 = new()
    //        {
    //            Id = entry.Id,
    //            Title = entry.Title,
    //            Descriptions = entry.Descriptions,
    //            HasPreviouslyBeenAsked = entry.HasPreviouslyBeenAsked,
    //            Importance = entry.Importance,
    //            YearsAsked = entry.YearsAsked,
    //            ProbableCases = entry.ProbableCases,
    //            BookReferences = entry.BookLocations,
    //            LinkReferences = entry.ReferenceLinks
    //        };
    //        newEntries.Add(entry1);
    //    }

    //    return newEntries;
    //}

    //private class QuestionEntry1
    //{
    //    [TypeConverter(typeof(QuestionIdConverter))]
    //    public QuestionId? Id { get; set; }

    //    public string Title { get; set; }

    //    [TypeConverter(typeof(StringListConverter))]
    //    public List<string>? Descriptions { get; set; }

    //    public bool HasPreviouslyBeenAsked { get; set; }

    //    public int Importance { get; set; }

    //    [TypeConverter(typeof(DateOnlyListConverter))]
    //    public List<DateOnly>? YearsAsked { get; set; }

    //    [TypeConverter(typeof(BookReferenceListConverter))]
    //    public List<BookReference>? BookLocations { get; set; }

    //    [TypeConverter(typeof(StringListConverter))]
    //    public List<string>? ProbableCases { get; set; }

    //    [TypeConverter(typeof(UriListConverter))]
    //    public List<Uri>? ReferenceLinks { get; set; }

    //}

    #endregion
}
