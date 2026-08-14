using Symptum.Core.Management.Navigation;
using Symptum.Core.Management.Resources;
using Symptum.Helpers;

namespace Symptum.Pages;

public sealed partial class MarkdownPage : NavigablePage
{
    private static readonly string _paddingKey = "MarkdownPadding";
    private static readonly string _blockMarginKey = "MarkdownBlockMargin";
    private static readonly string _listMarginKey = "MarkdownListMargin";
    private static readonly Thickness _defaultPadding; // 36
    private static readonly Thickness _defaultBlockMargin; // 16, 8, 8, 8
    private static readonly Thickness _defaultListMargin; // 16, 8, 0, 8

    private static readonly Thickness _narrowPadding = new(12);
    private static readonly Thickness _narrowBlockMargin = new(8, 4, 4, 4);
    private static readonly Thickness _narrowListMargin = new(8, 4, 0, 4);

    private MarkdownFileResource? resource;

    static MarkdownPage()
    {
        try
        {
            if (App.Current.Resources.TryGetValue(_paddingKey, out var p))
                _defaultPadding = (Thickness)p;
            if (App.Current.Resources.TryGetValue(_blockMarginKey, out var bm))
                _defaultBlockMargin = (Thickness)bm;
            if (App.Current.Resources.TryGetValue(_paddingKey, out var lm))
                _defaultListMargin = (Thickness)lm;
        } catch { }
    }

    public MarkdownPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigableChanged(INavigable? navigable)
    {
        if (navigable is MarkdownFileResource md)
        {
            resource = md;
            markdownView.Text = md.Markdown;

            // Re-apply to fix resolution.
            ThemeHelper.ApplyFontFamily(ThemeHelper.FontName);
            ThemeHelper.ApplyFontSize(ThemeHelper.FontSize);
        }
    }

    // It is not updated dynamically. Only resolved the first time.
    // Subsequent updates requires re-rendering.
    // The only options are:
    // - Re-rendering when the layout is changed.
    // - Making these as individual properties which will also re-render internally.
    // But imma leave it like this.
    protected override void OnLayoutChanged(bool isWide)
    {
        if (isWide)
        {
            App.Current.Resources[_paddingKey] = _defaultPadding;
            App.Current.Resources[_blockMarginKey] = _defaultBlockMargin;
            App.Current.Resources[_listMarginKey] = _defaultListMargin;
        }
        else
        {
            App.Current.Resources[_paddingKey] = _narrowPadding;
            App.Current.Resources[_blockMarginKey] = _narrowBlockMargin;
            App.Current.Resources[_listMarginKey] = _narrowListMargin;
        }
    }
}
