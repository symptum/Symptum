using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json.Serialization;
using CsvHelper;
using Symptum.Core.Management.Resources;

namespace Symptum.Core.Subjects.QuestionBanks;

public class QuestionBankTopic : CsvFileResource
{
    public QuestionBankTopic()
    { }

    public QuestionBankTopic(string title)
    {
        Title = title;
    }

    #region Properties

    private ObservableCollection<QuestionEntry>? entries;

    [JsonIgnore]
    public ObservableCollection<QuestionEntry>? Entries
    {
        get => entries;
        set => SetProperty(ref entries, value);
    }

    #endregion

    protected override string OnWriteCSV()
    {
        using StringWriter writer = new();
        using CsvWriter csvW = new(writer, CultureInfo.InvariantCulture);
        csvW.WriteHeader<QuestionEntry>();
        csvW.NextRecord();
        if (Entries != null)
        {
            foreach (var entry in Entries)
            {
                csvW.WriteRecord(entry);
                csvW.NextRecord();
            }
        }

        return writer.ToString();
    }

    protected override void OnReadCSV(string csv)
    {
        if (string.IsNullOrEmpty(csv)) return;

        using StringReader reader = new(csv);
        using CsvReader csvReader = new(reader, CultureInfo.InvariantCulture);
        Entries = new(csvReader.GetRecords<QuestionEntry>().ToList());
    }

    public Dictionary<int, int> GenerateWeightage()
    {
        Dictionary<int, int> years = [];

        if (entries != null)
        {
            foreach (var entry in entries)
            {
                if (entry.YearsAsked != null)
                {
                    foreach (var year in entry.YearsAsked)
                    {
                        if (years.ContainsKey(year.Year))
                            years[year.Year] += 1;
                        else years.Add(year.Year, 1);
                    }
                }
            }
        }

        return years;
    }
}
