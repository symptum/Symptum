using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Core.Helpers;

namespace Symptum.Core.Subjects.Books;

public class BookReference : ObservableObject
{
    private static readonly string _bookCodeId = "n";
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

    private string _pageNumbers;

    public string PageNumbers
    {
        get => _pageNumbers;
        set => SetProperty(ref _pageNumbers, value);
    }

    #endregion

    public BookReference()
    {
    }

    public static bool TryParse(string? text, out BookReference? bookReference)
    {
        bool parsed = false;
        bookReference = null;
        if (!string.IsNullOrEmpty(text))
        {
            var values = text.Split(ParserHelper.BookReferenceDelimiter);
            if (values.Length == 2)
            {
                bookReference = new BookReference();

                string bookString = values[0];
                (Book? book, int edition, int volume) = ParseBookString(bookString);
                bookReference.Book = book;
                bookReference.Edition = edition;
                bookReference.Volume = volume;
                if (!string.IsNullOrEmpty(values[1]))
                    bookReference.PageNumbers = values[1];
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
            string? bookCode = col[_bookCodeId];
            string? bookEdition = col[_bookEditionId];
            string? bookVolume = col[_bookVolumeId];
            if (!string.IsNullOrEmpty(bookCode))
                book = BookStore.Books.FirstOrDefault(x => x.Code == bookCode);
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
        col.Add(_bookCodeId, _book?.Code ?? string.Empty);
        col.Add(_bookEditionId, _edition.ToString());
        col.Add(_bookVolumeId, _volume.ToString());
        return col.ToString() + ParserHelper.BookReferenceDelimiter + _pageNumbers;
    }
}
