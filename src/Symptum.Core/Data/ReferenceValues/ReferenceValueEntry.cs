using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Core.TypeConversion;

namespace Symptum.Core.Data.ReferenceValues;

public class ReferenceValueEntry : ObservableObject
{
    public ReferenceValueEntry()
    { }

    #region Properties

    private string? _title;

    public string? Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private List<Quantity>? _quantities;

    public List<Quantity>? Quantities
    {
        get => _quantities;
        set => SetProperty(ref _quantities, value);
    }

    private string? _inference;

    public string? Inference
    {
        get => _inference;
        set => SetProperty(ref _inference, value);
    }

    private string? _remarks;

    public string? Remarks
    {
        get => _remarks;
        set => SetProperty(ref _remarks, value);
    }

    #endregion

    public static bool TryParse(string? text, [NotNullWhen(true)] out ReferenceValueEntry? entry)
    {
        bool parsed = false;
        entry = null;
        if (!string.IsNullOrEmpty(text))
        {
            entry = JsonSerializer.Deserialize<ReferenceValueEntry>(text, options);
            parsed = true;
        }

        return parsed;
    }

    public override string ToString() => JsonSerializer.Serialize(this, options);

    private static JsonSerializerOptions options = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull
    };

    public string GetPreviewText()
    {
        StringBuilder sb = new();
        sb.Append(_title);
        sb.Append(": ");
        sb.Append(ListToStringConversion.ConvertToString<Quantity>(_quantities, x => x.ToString(), ", "));
        sb.Append(" Inference: ");
        sb.Append(_inference);
        sb.Append(" Remarks: ");
        sb.Append(_remarks);
        return sb.ToString();
    }
}
