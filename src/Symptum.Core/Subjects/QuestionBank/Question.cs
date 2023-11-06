using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Symptum.Core.QuestionBank
{
    public class Question
    {
        public string Id;

        public bool IsChecked;

        public List<QuestionEntry> Entries;

        public int GetNumberOfTimesAsked()
        {
            int count = 0;

            foreach (QuestionEntry entry in Entries)
            {
                count += entry.YearsAsked.Count;
            }

            return count;
        }
    }
}
