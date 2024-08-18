using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CsvHelper;
using Symptum.Core.Data.Nutrition;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Subjects.QuestionBanks;

namespace Symptum.Core.Helpers;

public static class CsvResourceHelper
{
    private static readonly string?[]? hQE;
    private static readonly string?[]? hRVP;
    private static readonly string?[]? hFood;

    static CsvResourceHelper()
    {
        using var writer = new StringWriter();
        using var csvW = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csvW.WriteHeader<QuestionEntry>();
        hQE = csvW.HeaderRecord;
        csvW.WriteHeader<ReferenceValueParameter>();
        hRVP = csvW.HeaderRecord;
        csvW.WriteHeader<Food>();
        hFood = csvW.HeaderRecord;
    }

    public static bool TryGetCsvResourceType(string csv, [NotNullWhen(true)] out Type? csvType)
    {
        csvType = null;

        try
        {
            using StringReader reader = new(csv);
            using CsvReader csvReader = new(reader, CultureInfo.InvariantCulture);
            csvReader.Read();
            csvReader.ReadHeader();
            string?[]? header = csvReader.HeaderRecord;

            if (header != null)
            {
                if (header.SequenceEqual(hQE))
                {
                    csvType = typeof(QuestionBankTopic);
                    return true;
                }
                else if (header.SequenceEqual(hRVP))
                {
                    csvType = typeof(ReferenceValueGroup);
                    return true;
                }
                else if (header.SequenceEqual(hFood))
                {
                    csvType = typeof(FoodGroup);
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
