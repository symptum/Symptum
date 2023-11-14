namespace Symptum.Core.Subjects.QuestionBank
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