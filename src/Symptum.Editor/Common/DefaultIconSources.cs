namespace Symptum.Editor.Common;

public static class DefaultIconSources
{
    public static IconSource DataGridIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.List };

    public static IconSource PropertiesIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Repair };

    public static IconSource DocumentIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Document };

    public static IconSource GroupListIconSource { get; } = new FontIconSource() { Glyph = "\uF168" };

    public static IconSource DictionaryIconSource { get; } = new FontIconSource() { Glyph = "\uE82D" };
    
    public static IconSource PhotoIconSource { get; } = new FontIconSource() { Glyph = "\uE91B" };

    public static IconSource PicturesIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Pictures };

    public static IconSource AudioIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Audio };

    public static IconSource FolderIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Folder };

    public static IconSource PackageIconSource { get; } = new FontIconSource() { Glyph = "\uE7B8" };
}
