using Symptum.Common.ProjectSystem;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects;
using static Symptum.UI.CommonGlyphs;

namespace Symptum.UI;

public static class IconHelper
{
    public static FontFamily? SymptumIconsFontFamily => Application.Current.Resources.TryGetValue("SymptumIconsFontFamily", out var f) ? f as FontFamily : null;

    #region Definitions

    public static IconSource HomeIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Home };

    public static IconSource SearchIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Find };

    public static IconSource SettingsIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Home };

    public static IconSource FocusSessionIconSource { get; } = new FontIconSource() { Glyph = FocusSession };

    public static IconSource SubjectsLibraryIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Library };

    public static IconSource ReferenceValuesIconSource { get; } = new FontIconSource() { Glyph = BookNumber, FontFamily = SymptumIconsFontFamily, FontSize = 16 };

    public static IconSource TableViewIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.List };

    public static IconSource PropertiesIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Repair };

    public static IconSource DocumentIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Document };

    public static IconSource GroupListIconSource { get; } = new FontIconSource() { Glyph = GroupList };

    public static IconSource SubjectIconSource { get; } = new FontIconSource() { Glyph = Dictionary };

    public static IconSource PhotoIconSource { get; } = new FontIconSource() { Glyph = Photo };

    public static IconSource PicturesIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Pictures };

    public static IconSource AudioIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Audio };

    public static IconSource FolderIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.Folder };

    public static IconSource PackageIconSource { get; } = new FontIconSource() { Glyph = CommonGlyphs.Package };

    public static IconSource OpenFileIconSource { get; } = new SymbolIconSource() { Symbol = Symbol.OpenFile };

    public static IconSource OpenFolderIconSource { get; } = new FontIconSource() { Glyph = OpenFolder };

    #endregion

    public static IconSource? GetIconSourceForResourceType(Type resourceType)
    {
        if (resourceType == null) return null;

        return resourceType switch
        {
            Type t when typeof(ProjectFolder).IsAssignableFrom(t) => FolderIconSource,
            Type t when typeof(Subject).IsAssignableFrom(t) => SubjectIconSource,
            Type t when typeof(CsvFileResource).IsAssignableFrom(t) => TableViewIconSource,
            Type t when typeof(ImageFileResource).IsAssignableFrom(t) => PhotoIconSource,
            Type t when typeof(MarkdownFileResource).IsAssignableFrom(t) => DocumentIconSource,
            Type t when typeof(ImageCategoryResource).IsAssignableFrom(t) => PicturesIconSource,
            Type t when typeof(PackageResource).IsAssignableFrom(t) => PackageIconSource,
            Type t when typeof(IResource).IsAssignableFrom(t) => GroupListIconSource,
            _ => null
        };
    }

    public static IconSource? GetIconSourceForUri(Uri? uri)
    {
        if (uri == null) return null;

        return uri.ToString().TrimEnd("/") switch
        {
            "symptum://referencevalues" => ReferenceValuesIconSource,
            _ => null
        };
    }

    #region ResourceType

    public static readonly DependencyProperty ResourceTypeProperty = DependencyProperty.RegisterAttached(
        "ResourceType",
        typeof(Type),
        typeof(IconSourceElement),
        new PropertyMetadata(null, OnResourceTypePropertyChanged));

    public static Type GetResourceType(IconSourceElement obj) => (Type)obj.GetValue(ResourceTypeProperty);

    public static void SetResourceType(IconSourceElement obj, Type value) => obj.SetValue(ResourceTypeProperty, value);

    private static void OnResourceTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        (d as IconSourceElement)?.IconSource = GetIconSourceForResourceType(e.NewValue as Type);

    #endregion

    #region Uri

    public static readonly DependencyProperty UriProperty = DependencyProperty.RegisterAttached(
        "Uri",
        typeof(Uri),
        typeof(IconSourceElement),
        new PropertyMetadata(null, OnUriPropertyChanged));

    public static Uri GetUri(IconSourceElement obj) => (Uri)obj.GetValue(UriProperty);

    public static void SetUri(IconSourceElement obj, Uri value) => obj.SetValue(UriProperty, value);

    private static void OnUriPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        (d as IconSourceElement)?.IconSource = GetIconSourceForUri(e.NewValue as Uri);

    #endregion
}
