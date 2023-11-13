using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Symptum.Core.QuestionBank
{
    public class QuestionBankTopic : ObservableObject
    {
        private string topicName;

        public string TopicName
        {
            get => topicName;
            set => SetProperty(ref topicName, value);
        }

        [JsonIgnore]
        public ObservableCollection<QuestionEntry> QuestionEntries { get; set; }

        public QuestionBankTopic() { }

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
            topic.QuestionEntries = new (csv.GetRecords<QuestionEntry>().ToList());

            return topic;
        }
    }
}
