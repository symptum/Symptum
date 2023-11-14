using CommunityToolkit.Mvvm.ComponentModel;

namespace Symptum.Core.Subjects.Books
{
    public class Book : ObservableObject
    {
        public static readonly Dictionary<string, Book> BookStore = new()
        {
        };

        public static readonly List<Book> Books = [.. BookStore.Values];

        #region Properties

        private string _title;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _author;

        public string Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }

        #endregion

        public Book()
        {
        }

        public Book(string title, string author)
        {
            _title = title;
            _author = author;
        }

        public override string ToString()
        {
            return Title + " by " + Author;
        }
    }
}