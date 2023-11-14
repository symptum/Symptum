using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Core.Helpers;
using System.Web;

namespace Symptum.Core.Subjects.Books
{
    public class BookLocation : ObservableObject
    {
        private static readonly string _bookNameId = "n";
        private static readonly string _bookEditionId = "ed";
        private static readonly string _bookVolumeId = "vol";

        #region Properties

        private Book _book;

        public Book Book
        {
            get => _book;
            set => SetProperty(ref _book, value);
        }

        private int _edition;

        public int Edition
        {
            get => _edition;
            set => SetProperty(ref _edition, value);
        }

        private int _volume;

        public int Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, value);
        }

        private int _pageNumber;

        public int PageNumber
        {
            get => _pageNumber;
            set => SetProperty(ref _pageNumber, value);
        }

        #endregion

        public BookLocation()
        {
        }

        public static bool TryParse(string? text, out BookLocation? location)
        {
            bool parsed = false;
            location = null;
            if (!string.IsNullOrEmpty(text))
            {
                var values = text.Split(ParserHelper.BookLocationDelimiter);
                if (values.Length == 2)
                {
                    location = new BookLocation();

                    string bookString = values[0];
                    (Book? book, int edition, int volume) = ParseBookString(bookString);
                    location.Book = book;
                    location.Edition = edition;
                    location.Volume = volume;
                    if (int.TryParse(values[1], out int pgNo))
                        location.PageNumber = pgNo;
                    parsed = true;
                }
            }

            return parsed;
        }

        private static (Book? book, int edition, int volume) ParseBookString(string bookString)
        {
            Book? book = null;
            int edition = 0;
            int volume = 0;
            var col = HttpUtility.ParseQueryString(bookString);
            if (col != null && col.Count > 0)
            {
                string? bookName = col[_bookNameId];
                string? bookEdition = col[_bookEditionId];
                string? bookVolume = col[_bookVolumeId];
                if (!string.IsNullOrEmpty(bookName) && Book.BookStore.TryGetValue(bookName, out Book? _book))
                    book = _book;
                if (int.TryParse(bookEdition, out int edNo))
                    edition = edNo;
                if (int.TryParse(bookVolume, out int volNo))
                    volume = volNo;
            }

            return (book, edition, volume);
        }

        public override string ToString()
        {
            var col = HttpUtility.ParseQueryString(string.Empty);
            col.Add(_bookNameId, Book.BookStore.FirstOrDefault(x => x.Value == _book).Key ?? string.Empty);
            col.Add(_bookEditionId, _edition.ToString());
            col.Add(_bookVolumeId, _volume.ToString());
            return col.ToString() + ParserHelper.BookLocationDelimiter + _pageNumber.ToString();
        }
    }
}