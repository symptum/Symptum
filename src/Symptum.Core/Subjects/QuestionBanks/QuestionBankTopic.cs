using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json.Serialization;
using CsvHelper;
using Symptum.Core.Management.Resources;

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
        ChildrenResources = null;
    }

    public override bool CanHandleChildResourceType(Type childResourceType)
    {
        return false;
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
}
