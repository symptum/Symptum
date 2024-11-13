using Symptum.Core.Helpers;
using Symptum.Core.Subjects.Books;
using System.Diagnostics.CodeAnalysis;
using System.Web;

namespace Symptum.Core.Data.Bibliography;

public record BookReference : LiteratureReference
{
    private static readonly string _bookId = "n";
    private static readonly string _bookEdition = "ed";
    private static readonly string _bookVolume = "vol";

    #region Properties

    public string? Section { get; init; }

    public int Edition { get; init; }

    public int ISBN { get; init; }

    #endregion

    // @book?n={id}&ed={edition}&vol={volume}#{pages}
    public static bool TryParse(string? text, [NotNullWhen(true)] out BookReference? bookReference)
    {
        bool parsed = false;
        bookReference = null;
        if (!string.IsNullOrEmpty(text))
        {
            var values = text.Split(ParserHelper.BookReferenceDelimiter);
            if (values.Length == 2)
            {
                string bookString = values[0][0] == '@' && values[0].Length > 6 ? values[0].Remove(0, 6) : values[0];
                (Book? book, int edition, int volume) = ParseBookString(bookString);

                bookReference = new()
                {
                    Id = book?.Id,
                    Title = book?.Title,
                    Authors = book?.Authors,
                    Edition = edition,
                    Volume = volume,
                    Pages = values[1]
                };
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
            string? bookCode = col[_bookId];
            string? bookEdition = col[_bookEdition];
            string? bookVolume = col[_bookVolume];
            if (!string.IsNullOrEmpty(bookCode))
                book = BookStore.Books.FirstOrDefault(x => x.Id == bookCode);
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
        col.Add(_bookId, Id ?? string.Empty);
        col.Add(_bookEdition, Edition.ToString());
        col.Add(_bookVolume, Volume.ToString());
        return "@book?" + col.ToString() + ParserHelper.BookReferenceDelimiter + Pages;
    }

    public override string GetPreviewText()
    {
        return $"{Title} by {Authors}, " +
            $"Edition: {Edition}, Volume: {Volume}, Pages: {Pages}";
    }
}
