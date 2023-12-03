using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Symptum.Core.Subjects.QuestionBank
{
    public class QuestionBankTopic : ObservableObject
    {
        #region Properties

        private string topicName = string.Empty;

        public string TopicName
        {
            get => topicName;
            set => SetProperty(ref topicName, value);
        }

        private ObservableCollection<QuestionEntry> questionEntries;

        [JsonIgnore]
        public ObservableCollection<QuestionEntry> QuestionEntries
        {
            get => questionEntries;
            set => SetProperty(ref questionEntries, value);
        }

        #endregion

        public QuestionBankTopic()
        { }

        public QuestionBankTopic(string topicName)
        {
            TopicName = topicName;
        }

        public string ToCSV()
        {
            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteHeader<QuestionEntry>();
            csv.NextRecord();
            foreach (var entry in QuestionEntries)
            {
                csv.WriteRecord(entry);
                csv.NextRecord();
            }
            return writer.ToString();
        }

        public static QuestionBankTopic CreateTopicFromCSV(string topicName, string csv)
        {
            if (string.IsNullOrEmpty(csv)) return null;

            QuestionBankTopic topic = new(topicName);
            using var reader = new StringReader(csv);
            using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
            topic.QuestionEntries = new(csvReader.GetRecords<QuestionEntry>().ToList());

            return topic;
        }
    }
}