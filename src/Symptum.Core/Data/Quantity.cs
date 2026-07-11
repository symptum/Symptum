using System.Diagnostics.CodeAnalysis;
using System.Text;
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

    private NumericalValue _value;

    public NumericalValue Value
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

    public override string ToString() => _value + (!string.IsNullOrWhiteSpace(_unit) ? " " + _unit : string.Empty);

    public static implicit operator Quantity?(string? value)
    {
        if (TryParse(value, out Quantity? quantity))
            return quantity;

        return null;
    }

    public static implicit operator string(Quantity? quantity) => quantity?.ToString() ?? string.Empty;

    private void AppendUnit(StringBuilder sb)
    {
        if (!string.IsNullOrWhiteSpace(Unit))
            sb.Append(" ").Append(Unit);
    }
    
    public string ToReadableString()
    {
        StringBuilder sb = new();

        if (Value.IsInterval)
        {
            // Finite Interval including closed, open and half-open intervals.
            // Assuming intervals will be finite and closed in most cases,
            // we are ignoring the inclusion of the extremities.
            if (double.IsFinite(Value.Minimum) && double.IsFinite(Value.Maximum))
            {
                // Format: Min - Max (unit)
                sb.Append(Value.Minimum);
                sb.Append(" - ");
                sb.Append(Value.Maximum);
                AppendUnit(sb);
            }
            else if (double.IsFinite(Value.Minimum))
            {
                // Format: ≥ or > Min (unit)
                sb.Append(Value.IncludesMinimum ? "≥ " : "> ");
                sb.Append(Value.Minimum);
                AppendUnit(sb);
            }
            else if (double.IsFinite(Value.Maximum))
            {
                // Format: ≤ or < Max (unit)
                sb.Append(Value.IncludesMaximum ? "≤ " : "< ");
                sb.Append(Value.Maximum);
                AppendUnit(sb);
            }
        }
        else if (Value.IsErrorInterval)
        {
            // Format: Value ± Error (unit)
            sb.Append(Value.Value);
            sb.Append(" ± ");
            sb.Append(Value.Error);
            AppendUnit(sb);
        }
        else if (!double.IsNaN(Value.Value))
        {
            // Format: Value (unit)
            sb.Append(Value.Value);
            AppendUnit(sb);
        }

        return sb.ToString();
    }
}
