using CommunityToolkit.Mvvm.ComponentModel;

namespace Symptum.Core.Data.ReferenceValues;

public class ReferenceValueData : ObservableObject
{
    #region Properties

    private string values = string.Empty;

    // TODO: Add math support to check if a test value satisfies the value, range or interval
    // [x,y] : x to y, x and y included;
    // (x,y) : x to y, x and y excluded;
    // (y,_) : greater than y;
    // (_,x) : lesser than x;
    // [y,_] : greater than or equal to y;
    // [_,x] : lesser than or equal to x;
    // x{y} : x + y or x - y;
    // x : single value
    public string Values
    {
        get => values;
        set => SetProperty(ref values, value);
    }

    private string unit = string.Empty;

    // TODO: Add unit conversion support
    public string Unit
    {
        get => unit;
        set => SetProperty(ref unit, value);
    }

    #endregion

    public ReferenceValueData()
    {
    }
}