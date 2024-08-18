using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Symptum.Core.Data;
using Symptum.Core.Data.Nutrition;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Helpers;
using Symptum.Core.Subjects.Books;
using Symptum.Core.Subjects.QuestionBanks;

namespace Symptum.Core.TypeConversion;

// These are not named as "...ToStringConverter" because they are used for CSV and are essentially converting to and from string.
// Another reason is to differentiate them from XAML Type Converters used for Data Binding. Where there is a convention of adding "...ToStringConverter" to their names.

#region Question Bank

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

public class BookReferenceListConverter : ListConverter<BookReference>
{
    public override void ValidateData(string text, List<BookReference> list) => ListToStringConversion.ValidateDataForBookReference(text, list);
}

#endregion

#region Reference Values

public class ReferenceValueEntryListConverter : ListConverter<ReferenceValueEntry>
{
    public override void ValidateData(string text, List<ReferenceValueEntry> list) => ListToStringConversion.ValidateDataForReferenceValueEntry(text, list);
}

#endregion

#region Common

public class QuantityCsvConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (Quantity.TryParse(text, out Quantity? quantity))
            return quantity;
        return null;
    }

    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        if (value is Quantity quantity)
            return quantity.ToString();
        else return string.Empty;
    }
}

#endregion

#region Nutrition

public class FoodMeasureListConverter : ListConverter<FoodMeasure>
{
    public override void ValidateData(string text, List<FoodMeasure> list) => ListToStringConversion.ValidateDataForFoodMeasure(text, list);
}

#endregion

public class ListToStringConversion
{
    public static List<T>? ConvertFromString<T>(string? text, Action<string, List<T>> validateData)
    {
        if (string.IsNullOrEmpty(text)) return null;

        List<T> list = [];

        string[] values = text.Split(ParserHelper.ListDelimiter);

        foreach (var value in values)
        {
            validateData(value, list);
        }

        return list;
    }

    public static string? ConvertToString<T>(object? value, Func<T, string> elementToString, string? separator = null)
    {
        var stringBuilder = new StringBuilder();

        if (value is List<T> values)
        {
            for (int i = 0; i < values.Count; i++)
            {
                var data = values[i];
                stringBuilder.Append(elementToString(data));
                if (i < values.Count - 1) stringBuilder.Append(separator);
            }
        }

        return stringBuilder.ToString();
    }

    public static string? ConvertToString<T>(object? value, Func<T, string> elementToString)
    {
        return ConvertToString(value, elementToString, ParserHelper.ListDelimiter.ToString());
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

    public static void ValidateDataForBookReference(string text, List<BookReference> list)
    {
        if (BookReference.TryParse(text, out BookReference? reference))
        {
            list.Add(reference);
        }
    }

    public static void ValidateDataForReferenceValueEntry(string text, List<ReferenceValueEntry> list)
    {
        if (ReferenceValueEntry.TryParse(text, out ReferenceValueEntry? entry))
        {
            list.Add(entry);
        }
    }

    public static void ValidateDataForFoodMeasure(string text, List<FoodMeasure> list)
    {
        if (FoodMeasure.TryParse(text, out FoodMeasure? measure))
        {
            list.Add(measure);
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
