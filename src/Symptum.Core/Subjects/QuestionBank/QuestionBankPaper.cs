using System.Collections.ObjectModel;

namespace Symptum.Core.Subjects.QuestionBank
{
    public class QuestionBankPaper
    {
        public QuestionBankPaper()
        { }

        public QuestionBankPaper(string paperName)
        {
            PaperName = paperName;
        }

        public string PaperName { get; set; }

        public ObservableCollection<QuestionBankTopic> Topics { get; set; }
    }
}