using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Symptum.Core.Data.Nutrition;

public class FoodMeasure : ObservableObject
{
    public FoodMeasure()
    { }

    public FoodMeasure(string title, double weight)
    {
        Title = title;
        Weight = weight;
    }

    #region Properties

    private string? _title;

    public string? Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private double _weight = 100;

    // Average Weight of the measure in grams.
    public double Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }

    #endregion

    public double GetMultiplier() => _weight / 100.0;

    public static bool TryParse(string? text, [NotNullWhen(true)] out FoodMeasure? measure)
    {
        bool parsed = false;
        measure = null;
        if (!string.IsNullOrEmpty(text))
        {
            measure = JsonSerializer.Deserialize<FoodMeasure>(text, options);
            parsed = true;
        }

        return parsed;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, options);
    }

    public string GetPreviewText()
    {
        return $"{_title}: {_weight}";
    }

    private static JsonSerializerOptions options = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull
    };
}
