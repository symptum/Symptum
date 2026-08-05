using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Core.Extensions;
using Symptum.Core.TypeConversion;

namespace Symptum.Core.Data.ReferenceValues;

public partial class ReferenceValueEntry : ObservableObject
{
    public ReferenceValueEntry()
    { }

    #region Properties

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial List<Quantity>? Quantities { get; set; }

    [ObservableProperty]
    public partial string? Inference { get; set; }

    [ObservableProperty]
    public partial string? Remarks { get; set; }

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
        sb.Append(Title)
            .Append(": ")
            .Append(ListToStringConversion.ConvertToString<Quantity>(Quantities, x => x.ToString(), ", "));
        
        if (!string.IsNullOrEmpty(Inference))
        {
            sb.Append(" Inference: ").Append(Inference);
        }
        if (!string.IsNullOrEmpty(Remarks))
        {
            sb.Append(" Remarks: ").Append(Remarks);
        }
        return sb.ToString();
    }

    public ReferenceValueEntry Clone() =>
        new()
        {
            Title = Title,
            Quantities = Quantities.CloneList(q => q.Clone()),
            Inference = Inference,
            Remarks = Remarks
        };
}
