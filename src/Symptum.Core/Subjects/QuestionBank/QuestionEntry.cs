using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using Symptum.Core.Subjects.Books;
using Symptum.Core.TypeConversion;

namespace Symptum.Core.Subjects.QuestionBank
{
    public class QuestionEntry : ObservableObject
    {
        #region Properties

        private QuestionId? id;

        [TypeConverter(typeof(QuestionIdConverter))]
        public QuestionId? Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }

        private string title = string.Empty;

        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        private string description = string.Empty;

        public string Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        private List<DateOnly> yearsAsked;

        [TypeConverter(typeof(DateOnlyListConverter))]
        public List<DateOnly> YearsAsked
        {
            get => yearsAsked;
            set => SetProperty(ref yearsAsked, value);
        }

        private List<BookLocation> bookLocations;

        [TypeConverter(typeof(BookLocationListConverter))]
        public List<BookLocation> BookLocations
        {
            get => bookLocations;
            set => SetProperty(ref bookLocations, value);
        }

        private string probableCases;

        public string ProbableCases
        {
            get => probableCases;
            set => SetProperty(ref probableCases, value);
        }

        private List<Uri> referenceLinks;

        [TypeConverter(typeof(UriListConverter))]
        public List<Uri> ReferenceLinks
        {
            get => referenceLinks;
            set => SetProperty(ref referenceLinks, value);
        }

        #endregion

        public QuestionEntry()
        {
        }
    }
}