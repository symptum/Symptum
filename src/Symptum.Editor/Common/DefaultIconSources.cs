using static Symptum.Editor.Common.CommonGlyphs;

namespace Symptum.Editor.Common;

public static class DefaultIconSources
{
    public static IconSource TableViewIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.List };

    public static IconSource PropertiesIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Repair };

    public static IconSource DocumentIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Document };

    public static IconSource GroupListIconSource { get; } = new FontIconSource() { Glyph = List };

    public static IconSource DictionaryIconSource { get; } = new FontIconSource() { Glyph = Dictionary };

    public static IconSource PhotoIconSource { get; } = new FontIconSource() { Glyph = Photo };

    public static IconSource PicturesIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Pictures };

    public static IconSource AudioIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Audio };

    public static IconSource FolderIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Folder };

    public static IconSource PackageIconSource { get; } = new FontIconSource() { Glyph = CommonGlyphs.Package };

    public static IconSource OpenFileIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.OpenFile };

    public static IconSource OpenFolderIconSource { get; } = new FontIconSource() { Glyph = OpenFolder };
}
