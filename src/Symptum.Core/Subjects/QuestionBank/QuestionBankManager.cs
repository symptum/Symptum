using Symptum.Core.Subjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Symptum.Core.QuestionBank
{
    public class QuestionBankManager
    {
        public QuestionBankManager() { }

        public static QuestionBankTopic GetTestQuestionBankTopic()
        {
            QuestionBankTopic topic = new("Abdomen")
            {
                QuestionEntries =
                [
                    new QuestionEntry()
                    {
                        Id = "S_AN_12.2",
                        Title = "Blood and Nerve Supply of Liver",
                        Description = "Write the blood supply and nerve supply of Liver. Blood Supply (3), Nerve Supply (2)",
                        BookLocations = ["anat?n=vsr&ed=8&vol=2#200"],
                        ProbableCases = "Mmbu",
                        YearsAsked = new List<DateOnly>()
                        {
                            DateOnly.FromDateTime(DateTime.Parse("2020-3")),
                            DateOnly.FromDateTime(DateTime.Parse("2018-2")),
                            DateOnly.FromDateTime(DateTime.Parse("2015-4")),
                        },
                        ReferenceLinks = new List<Uri>()
                        {
                            new("symptum://subjects/anat/notes/liver#blood_supply"),
                            new("symptum://subjects/anat/notes/liver#nerve_supply")
                        }
                    },
                    new QuestionEntry()
                    {
                        Id = "E_AN_9.5",
                        Title = "Define boundaries and contents of posterior mediastinum.\r\nDescribe the formation, course & relations, termination and tributaries of Azygos vein",
                        Description = "Definition, Boundaries and contents of posterior mediastinum\r\nFormation, course, Relations, Termination, Tributaries of Azygos vein",
                        BookLocations = ["anat?n=vsr&ed=8&vol=1#130"],
                        ProbableCases = "Malak",
                        YearsAsked = new List<DateOnly>()
                        {
                            DateOnly.FromDateTime(DateTime.Parse("2022-2"))
                        },
                        ReferenceLinks = new List<Uri>()
                        {
                            new("symptum://subjects/anat/notes/posterior_mediastinum"),
                            new("symptum://subjects/anat/notes/azygousvein")
                        }
                    }
                ]
            };

            return topic;
        }

        public static void Test()
        {
            var topic = GetTestQuestionBankTopic();

            topic.SaveAsCSV("D:\\test.csv");

            QuestionBank questionBank = new(SubjectList.Anatomy)
            {
                QuestionBankPapers =
                [
                    new QuestionBankPaper("Paper 1")
                    {
                        Topics =
                        [
                            topic,
                        ]
                    }
                ]
            };

            string jsonString = JsonSerializer.Serialize(questionBank, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText("D:\\test.json", jsonString);
        }

        public static void Test2()
        {
            string jsonString = File.ReadAllText("D:\\test.json");
            QuestionBank questionBank = JsonSerializer.Deserialize<QuestionBank>(jsonString);
            Debug.WriteLine(questionBank.Subject.ToString());
        }

        public static void Test3()
        {
            var topic = QuestionBankTopic.ReadTopicFromCSV("D:\\test.csv");
            string jsonString = JsonSerializer.Serialize(topic, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText("D:\\test3.json", jsonString);
        }
    }
}
