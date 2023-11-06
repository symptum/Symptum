using Symptum.Core.Subjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symptum.Core.QuestionBank
{
    public class QuestionBank
    {
        public SubjectList Subject {  get; set; }

        public ObservableCollection<QuestionBankPaper> QuestionBankPapers { get; set; }

        public QuestionBank() { }

        public QuestionBank(SubjectList subject)
        {
            Subject = subject;
        }

        public void LoadTopicFromCSV(string csvFile)
        {

        }
    }
}
