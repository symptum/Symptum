using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using Symptum.Core.Subjects.Books;
using Symptum.Core.TypeConversion;
using System.Runtime.CompilerServices;

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

        private bool hasPreviouslyBeenAsked;

        public bool HasPreviouslyBeenAsked
        {
            get => hasPreviouslyBeenAsked;
            set => SetProperty(ref hasPreviouslyBeenAsked, value);
        }

        private int importance = 0;

        public int Importance
        {
            get => importance;
            set => SetProperty(ref importance, value);
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

        private List<string> probableCases;

        [TypeConverter(typeof(StringListConverter))]
        public List<string> ProbableCases
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

        public QuestionEntry Clone()
        {
            return new QuestionEntry()
            {
                Id = new() { QuestionType = id.QuestionType, SubjectCode = id.SubjectCode, CompetencyNumbers = id.CompetencyNumbers },
                Title = Title,
                Description = Description,
                HasPreviouslyBeenAsked = HasPreviouslyBeenAsked,
                Importance = Importance,
                YearsAsked = CloneList(YearsAsked),
                BookLocations = CloneList(BookLocations, x => new() { Book = x.Book, Edition = x.Edition, Volume = x.Volume, PageNumber = x.PageNumber }),
                ProbableCases = CloneList(probableCases),
                ReferenceLinks = CloneList(referenceLinks)
            };
        }

        private List<T> CloneList<T>(List<T> values, Func<T, T>? function = null)
        {
            List<T> results = [];
            if (values != null)
            {
                foreach (T item in values)
                {
                    T result = function != null ? function(item) : item;
                    results.Add(result);
                }
            }

            return results;
        }
    }
}