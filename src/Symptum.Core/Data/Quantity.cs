using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Core.Math;
using Symptum.Core.Serialization;

namespace Symptum.Core.Data;

[JsonConverter(typeof(QuantityJsonConverter))]
public class Quantity : ObservableObject
{
    public Quantity()
    { }

    public Quantity(NumericalValue value, string unit)
    {
        Value = value;
        Unit = unit;
    }

    #region Properties

    private NumericalValue? _value;

    public NumericalValue? Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    private string? _unit;

    public string? Unit
    {
        get => _unit;
        set => SetProperty(ref _unit, value);
    }

    #endregion

    public static bool TryParse(string? value, [NotNullWhen(true)] out Quantity? quantity)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            value = value.Trim();
            int index = value.LastIndexOf(' ');
            string nv = value, unit = string.Empty;
            if (index > 0 && nv.Length > index && !NumericalValue.IsEndCharacter(nv[index + 1]))
            {
                nv = value[..index];
                unit = value[(index + 1)..];
            }
            if (NumericalValue.TryParse(nv, out NumericalValue numericalValue))
            {
                quantity = new()
                {
                    Value = numericalValue,
                    Unit = unit
                };
                return true;
            }
        }

        quantity = null;
        return false;
    }

    public override string ToString()
    {
        return _value + (!string.IsNullOrWhiteSpace(_unit) ? " " + _unit : string.Empty);
    }

    public static implicit operator Quantity?(string? value)
    {
        if (TryParse(value, out Quantity? quantity))
            return quantity;

        return null;
    }

    public static implicit operator string(Quantity? quantity) => quantity?.ToString() ?? string.Empty;
}
