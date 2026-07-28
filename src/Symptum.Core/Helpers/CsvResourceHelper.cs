using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CsvHelper;
using Symptum.Core.Data.ReferenceValues;

namespace Symptum.Core.Helpers;

public static class CsvResourceHelper
{
    private static readonly string?[]? hRVP;

    static CsvResourceHelper()
    {
        using var writer = new StringWriter();
        using var csvW = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csvW.WriteHeader<ReferenceValueParameter>();
        hRVP = csvW.HeaderRecord;
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
                if (header.SequenceEqual(hRVP))
                {
                    csvType = typeof(ReferenceValueGroup);
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
