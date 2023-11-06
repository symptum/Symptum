using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symptum.Core.QuestionBank
{
    public class QuestionBankPaper
    {
        public QuestionBankPaper() { }

        public QuestionBankPaper(string paperName)
        {
            PaperName = paperName;
        }

        public string PaperName { get; set; }

        public ObservableCollection<QuestionBankTopic> Topics { get; set; }
    }
}
