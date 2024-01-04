using System.Text;
using Symptum.Core.Subjects.Books;
using Symptum.Core.Subjects.QuestionBanks;
using Symptum.Core.TypeConversion;

namespace Symptum.Editor.Helpers;

public class MarkdownHelper
{
    private static string GetImportanceText(int importance)
    {
        if (importance > 1)
            return new string('*', importance).Replace("*", "\\*");
        else
            return string.Empty;
    }

    private static string GetDescription(string description)
    {
        string x = description.Replace("- ", "");
        string lineBreak = ", ";
        x = x.Replace("\r\n", lineBreak).Replace("\n", lineBreak).Replace("\r", lineBreak).Replace("/", lineBreak);
        return x;
    }

    private static string GetYear(DateOnly date)
    {
        string month = date.ToString("MMMM").Substring(0, 3);
        return $"{month} {date:yy}";
    }

    private static string GetOrdinal(int num)
    {
        const string TH = "th";
        string s = num.ToString();

        if (num < 1)
        {
            return s;
        }

        num %= 100;
        if ((num >= 11) && (num <= 13))
        {
            return s + TH;
        }

        return (num % 10) switch
        {
            1 => s + "st",
            2 => s + "nd",
            3 => s + "rd",
            _ => s + TH,
        };
    }

    private static string GetBookLocation(BookLocation? bookLocation)
    {
        if (bookLocation == null) return string.Empty;
        string volString = bookLocation.Volume > 0 ? $" Volume {bookLocation.Volume}," : string.Empty;

        return $"{bookLocation.Book.Code}, {GetOrdinal(bookLocation.Edition)} Edition,{volString} Pg.No: {bookLocation.PageNumber}";
    }

    public static void GenerateMarkdownForQuestionBankTopic(QuestionBankTopic topic, ref StringBuilder mdBuilder)
    {
        if (topic != null && topic.QuestionEntries != null && topic.QuestionEntries.Count > 0)
        {
            mdBuilder.AppendLine("### " + topic.Title);
            mdBuilder.AppendLine();

            GenerateMarkdownForQuestionEntries(topic.QuestionEntries.Order(), ref mdBuilder);
        }
    }

    public static void GenerateMarkdownForQuestionEntries(IEnumerable<QuestionEntry>? questionEntries, ref StringBuilder mdBuilder)
    {
        ArgumentNullException.ThrowIfNull(mdBuilder);
        if (questionEntries == null || !questionEntries.Any()) return;

        int quesno = 1;

        var essays = questionEntries.Where(x => x.Id?.QuestionType == QuestionType.Essay);
        var shortNotes = questionEntries.Where(x => x.Id?.QuestionType == QuestionType.ShortNote);

        if (essays.Any())
        {
            mdBuilder.AppendLine("#### Essays");
            mdBuilder.AppendLine();

            foreach (var entry in essays)
            {
                GenerateMarkdownForQuestionEntry(entry, quesno, ref mdBuilder);
                quesno++;
            }
        }
        quesno = 1;
        if (shortNotes.Any())
        {
            mdBuilder.AppendLine("#### Short Notes");
            mdBuilder.AppendLine();

            foreach (var entry in shortNotes)
            {
                GenerateMarkdownForQuestionEntry(entry, quesno, ref mdBuilder);
                quesno++;
            }
        }
        mdBuilder.AppendLine();
    }

    private static bool _includeYearsAsked = true;
    private static bool _includeBookLocations = true;

    public static void GenerateMarkdownForQuestionEntry(QuestionEntry? entry, int quesno, ref StringBuilder mdBuilder)
    {
        ArgumentNullException.ThrowIfNull(mdBuilder);
        if (entry == null) return;

        string qtitle = entry.Title.Trim();
        mdBuilder.AppendFormat("{0}. **{1}** {2}",
            quesno, qtitle, GetImportanceText(entry.Importance));
        if (_includeYearsAsked && entry.YearsAsked != null && entry.YearsAsked.Count > 0)
            mdBuilder.AppendFormat(" ({0})",
                ListToStringConversion.ConvertToString<DateOnly>(entry.YearsAsked, GetYear));
        if (_includeBookLocations && entry.BookLocations != null && entry.BookLocations.Count > 0)
            mdBuilder.AppendFormat("\t({0})",
                ListToStringConversion.ConvertToString<BookLocation>(entry.BookLocations, GetBookLocation));
        mdBuilder.AppendLine();
        if (entry.Descriptions != null && entry.Descriptions.Count > 0)
        {
            foreach (var desc in entry.Descriptions)
            {
                mdBuilder.AppendFormat("\t- {0}", GetDescription(desc));
                mdBuilder.AppendLine();
            }
            mdBuilder.AppendLine();
        }
        if (entry.ProbableCases != null && entry.ProbableCases.Count > 0)
        {
            mdBuilder.AppendLine("\t*Probable Cases:*");
            foreach (var @case in entry.ProbableCases)
            {
                mdBuilder.AppendFormat("\t- {0}", @case);
                mdBuilder.AppendLine();
            }
            mdBuilder.AppendLine();
        }
        mdBuilder.AppendLine();
    }
}
