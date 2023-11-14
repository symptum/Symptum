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

        public void SaveAsCSV(string path)
        {
            using var writer = new StreamWriter(path);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteHeader<QuestionEntry>();
            csv.NextRecord();
            foreach (var entry in QuestionEntries)
            {
                csv.WriteRecord(entry);
                csv.NextRecord();
            }
        }

        public static QuestionBankTopic ReadTopicFromCSV(string path)
        {
            if (!File.Exists(path) || Path.GetExtension(path).ToLower() != ".csv") return null;

            QuestionBankTopic topic = new(Path.GetFileNameWithoutExtension(path));
            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            topic.QuestionEntries = new(csv.GetRecords<QuestionEntry>().ToList());

            return topic;
        }
    }
}