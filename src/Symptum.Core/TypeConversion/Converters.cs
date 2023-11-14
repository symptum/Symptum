using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Symptum.Core.Helpers;
using Symptum.Core.Subjects.Books;
using Symptum.Core.Subjects.QuestionBank;
using System.Text;

namespace Symptum.Core.TypeConversion
{
    #region Question Bank

    // These are not named as "...ToStringConverter" because they are used for CSV and are essentially converting to and from string.
    // Another reason is to differentiate them from XAML Type Converters used for Data Binding. Where there is a convention of adding "...ToStringConverter" to their names.

    public class QuestionIdConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            return QuestionId.Parse(text);
        }

        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value is QuestionId id)
                return id.ToString();
            else return string.Empty;
        }
    }

    public class DateOnlyListConverter : ListConverter<DateOnly>
    {
        public override void ValidateData(string text, List<DateOnly> list) => ListToStringConversion.ValidateDataForDate(text, list);

        public override string ElementToString(DateOnly data) => ListToStringConversion.ElementToStringForDateOnly(data);
    }

    public class UriListConverter : ListConverter<Uri>
    {
        public override void ValidateData(string text, List<Uri> list) => ListToStringConversion.ValidateDataForUri(text, list);
    }

    public class StringListConverter : ListConverter<string>
    {
        public override void ValidateData(string text, List<string> list) => ListToStringConversion.ValidateDataForString(text, list);
    }

    public class BookLocationListConverter : ListConverter<BookLocation>
    {
        public override void ValidateData(string text, List<BookLocation> list) => ListToStringConversion.ValidateDataForBookLocation(text, list);
    }

    #endregion

    public class ListToStringConversion
    {
        public static List<T>? ConvertFromString<T>(string? text, Action<string, List<T>> validateData)
        {
            if (string.IsNullOrEmpty(text)) return null;

            List<T> list = new();

            string[] values = text.Split(ParserHelper.ListDelimiter);

            foreach (var value in values)
            {
                validateData(value, list);
            }

            return list;
        }

        public static string? ConvertToString<T>(object? value, Func<T, string> elementToString)
        {
            var stringBuilder = new StringBuilder();

            if (value is List<T> values)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    var data = values[i];
                    stringBuilder.Append(elementToString(data));
                    if (i < values.Count - 1) stringBuilder.Append(ParserHelper.ListDelimiter);
                }
            }

            return stringBuilder.ToString();
        }

        public static void ValidateDataForDate(string text, List<DateOnly> list)
        {
            if (DateOnly.TryParse(text, out DateOnly year))
            {
                list.Add(year);
            }
        }

        public static void ValidateDataForUri(string text, List<Uri> list)
        {
            if (!string.IsNullOrEmpty(text))
            {
                list.Add(new Uri(text));
            }
        }

        public static void ValidateDataForString(string text, List<string> list)
        {
            if (!string.IsNullOrEmpty(text))
            {
                list.Add(text);
            }
        }

        public static void ValidateDataForBookLocation(string text, List<BookLocation> list)
        {
            if (BookLocation.TryParse(text, out BookLocation? location))
            {
                list.Add(location);
            }
        }

        public static string ElementToStringForDateOnly(DateOnly data)
        {
            return data.ToString("yyyy-MM");
        }

        public static string ElementToStringDefault<T>(T? data) => data?.ToString() ?? string.Empty;
    }

    public class ListConverter<T> : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            return ListToStringConversion.ConvertFromString<T>(text, ValidateData);
        }

        public virtual void ValidateData(string text, List<T> list)
        { }

        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            return ListToStringConversion.ConvertToString<T>(value, ElementToString);
        }

        public virtual string ElementToString(T? data) => ListToStringConversion.ElementToStringDefault(data);
    }
}