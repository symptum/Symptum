namespace Symptum.Editor.Common;

public static class DefaultIconSources
{
    public static IconSource DataGridIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.List };

    public static IconSource PropertiesIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Repair };

    public static IconSource DocumentIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Document };

    public static IconSource PicturesIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Pictures };
}
