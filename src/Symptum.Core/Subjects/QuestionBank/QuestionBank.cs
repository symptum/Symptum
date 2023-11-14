using System.Collections.ObjectModel;

namespace Symptum.Core.Subjects.QuestionBank
{
    public class QuestionBank
    {
        public SubjectList Subject { get; set; }

        public ObservableCollection<QuestionBankPaper> QuestionBankPapers { get; set; }

        public QuestionBank()
        { }

        public QuestionBank(SubjectList subject)
        {
            Subject = subject;
        }

        public void LoadTopicFromCSV(string csvFile)
        {
        }
    }
}