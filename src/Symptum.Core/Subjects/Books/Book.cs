using CommunityToolkit.Mvvm.ComponentModel;

namespace Symptum.Core.Subjects.Books
{
    public class Book : ObservableObject
    {
        #region Properties

        private string _code = string.Empty;

        public string Code
        {
            get => _code;
            set => SetProperty(ref _code, value);
        }

        private string _title = string.Empty;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _authors = string.Empty;

        public string Authors
        {
            get => _authors;
            set => SetProperty(ref _authors, value);
        }

        #endregion

        public Book()
        {
        }

        public Book(string code, string title, string author)
        {
            _code = code;
            _title = title;
            _authors = author;
        }

        public override string ToString()
        {
            return Title + " by " + Authors + " (" + Code + ")";
        }
    }
}