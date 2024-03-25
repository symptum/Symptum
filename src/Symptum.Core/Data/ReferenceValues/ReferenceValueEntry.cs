using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Core.Helpers;

namespace Symptum.Core.Data.ReferenceValues;

public class ReferenceValueEntry : ObservableObject
{
    private static readonly string _entryTitleId = "n";
    private static readonly string _entryInfId = "inf";
    private static readonly string _entryRemId = "rem";
    private static readonly string _entryDataId = "data";

    #region Properties

    private string _title = string.Empty;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private List<ReferenceValueData>? _data;

    public List<ReferenceValueData>? Data
    {
        get => _data;
        set => SetProperty(ref _data, value);
    }

    private string _inference = string.Empty;

    public string Inference
    {
        get => _inference;
        set => SetProperty(ref _inference, value);
    }

    private string _remarks = string.Empty;

    public string Remarks
    {
        get => _remarks;
        set => SetProperty(ref _remarks, value);
    }

    #endregion

    public ReferenceValueEntry()
    {
    }

    public static bool TryParse(string? text, out ReferenceValueEntry? entry)
    {
        bool parsed = false;
        entry = null;
        if (!string.IsNullOrEmpty(text))
        {
            entry = new ReferenceValueEntry();

            (string title, List<ReferenceValueData> data, string inference, string remarks) = ParseEntryString(text);
            entry.Title = title;
            entry.Data = data;
            entry.Inference = inference;
            entry.Remarks = remarks;
            parsed = true;
        }

        return parsed;
    }

    private static (string title, List<ReferenceValueData> data, string inference, string remarks) ParseEntryString(string entryString)
    {
        string title = string.Empty;
        string inference = string.Empty;
        string remarks = string.Empty;
        List<ReferenceValueData> data = [];
        var col = HttpUtility.ParseQueryString(entryString);
        if (col != null && col.Count > 0)
        {
            title = col[_entryTitleId];
            inference = col[_entryInfId];
            remarks = col[_entryRemId];

            var dataString = col[_entryDataId];
            if (!string.IsNullOrEmpty(dataString))
            {
                var values = dataString.Split(ParserHelper.ReferenceValueDataDelimiter);
                if (values.Length > 0)
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        data.Add(new() { Values = values[i] });
                    }
                }
            }
        }

        return (title, data, inference, remarks);
    }

    public override string ToString()
    {
        var col = HttpUtility.ParseQueryString(string.Empty);
        col?.Add(_entryTitleId, _title);
        string dataString = string.Empty;
        if (_data != null)
        {
            for (int i = 0; i < _data.Count; i++)
            {
                dataString += _data[i].Values;
                if (i < _data.Count - 1) dataString += ParserHelper.ReferenceValueDataDelimiter;
            }
        }
        col?.Add(_entryDataId, dataString);
        col?.Add(_entryInfId, _inference);
        col?.Add(_entryRemId, _remarks);
        return col?.ToString() ?? string.Empty;
    }
}
