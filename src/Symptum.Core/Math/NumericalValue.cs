using System.Text;

namespace Symptum.Core.Math;

public struct NumericalValue : IEquatable<NumericalValue>
{
    private const char OpeningSquareBracket = '[';
    private const char ClosingSquareBracket = ']';
    private const char OpeningParenthesis = '(';
    private const char ClosingParenthesis = ')';
    private const char EmptyValue = '_';
    private const string PlusOrMinus = "pm";

    public NumericalValue()
    { }

    #region Properties

    private double _value = double.NaN;

    public double Value
    {
        get => _value;
        set
        {
            if (!IsInterval)
                _value = value;
            else
                throw new NotSupportedException();
        }
    }

    public bool IsInterval { get; set; } = false;

    public double Minimum { get; set; } = double.NaN;

    public bool IncludesMinimum { get; set; } = false;

    public double Maximum { get; set; } = double.NaN;

    public bool IncludesMaximum { get; set; } = false;

    public bool IsErrorInterval { get; set; } = false;

    public double Error { get; set; } = double.NaN;

    #endregion

    // Finite & Closed:
    //     1. [a, b] : a <= x <= b;
    // Finite & Open:
    //     2. (a, b) : a < x < b;
    // Finite & Half-Open:
    //     3. [a, b) : a <= x < b;
    //     4. (a, b] : a < x <= b;
    // Infinite & Closed:
    //     5. [a, _) : a <= x;
    //     6. (_, b] : x <= b;
    // Infinite & Open:
    //     7. (a, _) : a < x;
    //     8. (_, b) : x < b;
    // Plus-minus:
    //     9. pm(a, b) : x = a ± b;
    // Single Value:
    //     10. a : x = a;
    public static bool TryParse(string? value, out NumericalValue numericalValue)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            value = value.Trim();
            char start = value[0];
            if (start == OpeningSquareBracket || start == OpeningParenthesis) // It's an interval notation
            {
                NumericalValue nv = new()
                {
                    IsInterval = true
                };
                int commaIndex = value.IndexOf(',');
                if (commaIndex > 1)
                {
                    string minString = value[1..commaIndex].Trim();
                    if (minString == EmptyValue.ToString())
                    {
                        nv.Minimum = double.NegativeInfinity;
                    }
                    else if (double.TryParse(minString, out double min))
                    {
                        nv.Minimum = min;
                        nv.IncludesMinimum = start == OpeningSquareBracket;
                    }
                    char end = value[^1];
                    if (end == ClosingSquareBracket || end == ClosingParenthesis)
                    {
                        string maxString = value[(commaIndex + 1)..^1].Trim();
                        if (maxString == EmptyValue.ToString())
                        {
                            nv.Maximum = double.PositiveInfinity;
                        }
                        else if (double.TryParse(maxString, out double max))
                        {
                            nv.Maximum = max;
                            nv.IncludesMaximum = end == ClosingSquareBracket;
                        }
                        numericalValue = nv;
                        return true;
                    }
                }
            }
            else if (value.StartsWith(PlusOrMinus)) // It's an error interval
            {
                // Error Interval can be converted to a normal interval with minimum and maximum values.
                // But it's done this way to conserve the original string data from which it was parsed.
                if (value[2] == OpeningParenthesis && value[^1] == ClosingParenthesis)
                {
                    int commaIndex = value.IndexOf(',');
                    string val = value[3..commaIndex].Trim();
                    string er = value[(commaIndex + 1)..^1].Trim();
                    if (double.TryParse(val, out double number)
                        && double.TryParse(er, out double error))
                    {
                        numericalValue = new()
                        {
                            IsErrorInterval = true,
                            Value = number,
                            Error = error
                        };
                        return true;
                    }
                }
            }
            else if (double.TryParse(value, out double number)) // It's a plain number
            {
                numericalValue = new()
                {
                    Value = number
                };
                return true;
            }
        }
        numericalValue = new();
        return false;
    }

    public override string ToString()
    {
        if (IsInterval)
        {
            StringBuilder sb = new();
            sb.Append(IncludesMinimum ? OpeningSquareBracket : OpeningParenthesis);
            sb.Append(double.IsNormal(Minimum) ? Minimum : EmptyValue.ToString());
            sb.Append(", ");
            sb.Append(double.IsNormal(Maximum) ? Maximum : EmptyValue.ToString());
            sb.Append(IncludesMaximum ? ClosingSquareBracket : ClosingParenthesis);
            return sb.ToString();
        }
        else if (IsErrorInterval)
            return $"{PlusOrMinus}{OpeningParenthesis}{Value}, {Error}{ClosingParenthesis}";
        else if (!double.IsNaN(Value))
            return Value.ToString();

        return string.Empty;
    }

    public override bool Equals(object? obj)
    {
        return obj switch
        {
            NumericalValue value => Equals(value),
            double number => Contains(number),
            _ => false
        };
    }

    public override int GetHashCode() => HashCode.Combine(Value, IsInterval, Minimum, IncludesMinimum, Maximum, IncludesMaximum, IsErrorInterval, Error);

    public bool Equals(NumericalValue other)
    {
        return IsInterval == other.IsInterval && IncludesMinimum == other.IncludesMinimum && IncludesMaximum == other.IncludesMaximum &&
            Value.Equals(other.Value) && Minimum.Equals(other.Minimum) && Maximum.Equals(other.Maximum) &&
            IsErrorInterval == other.IsErrorInterval && Error.Equals(other.Error);
    }

    public bool Contains(double value)
    {
        if (double.IsNaN(value)) return false;

        if (IsInterval)
        {
            bool cmin = false;
            bool cmax = false;
            if (!double.IsNaN(Minimum))
            {
                if (IncludesMinimum)
                    cmin = value >= Minimum;
                else
                    cmin = value > Minimum;
            }
            if (!double.IsNaN(Maximum))
            {
                if (IncludesMaximum)
                    cmax = value <= Maximum;
                else
                    cmax = value < Maximum;
            }
            return cmin && cmax;
        }
        else if (IsErrorInterval)
        {
            if (!double.IsNaN(Value) && !double.IsNaN(Error))
                return value >= Value - Error && value <= Value + Error;
        }
        else
            return value.Equals(Value);

        return false;
    }

    internal static bool IsEndCharacter(char end) => end == ClosingParenthesis || end == ClosingSquareBracket ||
        end == EmptyValue || end == ',' || char.IsNumber(end);

    #region Operators

    public static bool operator ==(NumericalValue value1, NumericalValue value2) => value1.Equals(value2);

    public static bool operator !=(NumericalValue value1, NumericalValue value2) => !value1.Equals(value2);

    public static bool operator ==(NumericalValue value1, double value2) => value1.Contains(value2);

    public static bool operator !=(NumericalValue value1, double value2) => !value1.Contains(value2);

    public static implicit operator NumericalValue(double value) => new() { Value = value };

    public static implicit operator NumericalValue(int value) => new() { Value = value };

    #endregion
}
