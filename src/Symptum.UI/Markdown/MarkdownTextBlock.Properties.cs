using Markdig.Syntax;

namespace Symptum.UI.Markdown;
public partial class MarkdownTextBlock
{
    #region Config Properties

    public static readonly DependencyProperty BaseUrlProperty = DependencyProperty.Register(
        nameof(BaseUrl), typeof(string), typeof(MarkdownTextBlock),
        new PropertyMetadata(null));

    public string? BaseUrl
    {
        get => (string?)GetValue(BaseUrlProperty);
        set => SetValue(BaseUrlProperty, value);
    }

    public static readonly DependencyProperty ImageProviderProperty = DependencyProperty.Register(
        nameof(ImageProvider), typeof(IImageProvider), typeof(MarkdownTextBlock),
        new PropertyMetadata(null));

    public IImageProvider? ImageProvider
    {
        get => (IImageProvider?)GetValue(ImageProviderProperty);
        set => SetValue(ImageProviderProperty, value);
    }

    public static readonly DependencyProperty SVGRendererProperty = DependencyProperty.Register(
        nameof(SVGRenderer), typeof(ISVGRenderer), typeof(MarkdownTextBlock),
        new PropertyMetadata(null));

    public ISVGRenderer? SVGRenderer
    {
        get => (ISVGRenderer?)GetValue(SVGRendererProperty);
        set => SetValue(SVGRendererProperty, value);
    }

    #endregion

    #region Text

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(MarkdownTextBlock),
        new PropertyMetadata(string.Empty, OnTextChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownTextBlock self && e.NewValue is string text)
        {
            self.ApplyText(true);
        }
    }

    #endregion

    #region MarkdownDocument

    public static readonly DependencyProperty MarkdownDocumentProperty = DependencyProperty.Register(
        nameof(MarkdownDocument),
        typeof(MarkdownDocument),
        typeof(MarkdownTextBlock),
        new PropertyMetadata(null));

    public MarkdownDocument? MarkdownDocument
    {
        get => (MarkdownDocument)GetValue(MarkdownDocumentProperty);
        private set => SetValue(MarkdownDocumentProperty, value);
    }

    #endregion

    #region IsTextSelectionEnabled

    public static readonly DependencyProperty IsTextSelectionEnabledProperty = DependencyProperty.Register(
        nameof(IsTextSelectionEnabled),
        typeof(bool),
        typeof(MarkdownTextBlock),
        new PropertyMetadata(true));

    public bool IsTextSelectionEnabled
    {
        get => (bool)GetValue(IsTextSelectionEnabledProperty);
        set => SetValue(IsTextSelectionEnabledProperty, value);
    }

    #endregion

    #region Common

    public static readonly DependencyProperty FlowDocumentStackPanelStyleProperty = DependencyProperty.Register(
        nameof(FlowDocumentStackPanelStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? FlowDocumentStackPanelStyle
    {
        get => (Style?)GetValue(FlowDocumentStackPanelStyleProperty);
        set => SetValue(FlowDocumentStackPanelStyleProperty, value);
    }

    public static readonly DependencyProperty BodyTextBlockStyleProperty = DependencyProperty.Register(
        nameof(BodyTextBlockStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? BodyTextBlockStyle
    {
        get => (Style?)GetValue(BodyTextBlockStyleProperty);
        set => SetValue(BodyTextBlockStyleProperty, value);
    }

    #endregion

    #region Heading

    public static readonly DependencyProperty H1TextBlockStyleProperty = DependencyProperty.Register(
        nameof(H1TextBlockStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? H1TextBlockStyle
    {
        get => (Style?)GetValue(H1TextBlockStyleProperty);
        set => SetValue(H1TextBlockStyleProperty, value);
    }

    public static readonly DependencyProperty H2TextBlockStyleProperty = DependencyProperty.Register(
        nameof(H2TextBlockStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? H2TextBlockStyle
    {
        get => (Style?)GetValue(H2TextBlockStyleProperty);
        set => SetValue(H2TextBlockStyleProperty, value);
    }

    public static readonly DependencyProperty H3TextBlockStyleProperty = DependencyProperty.Register(
        nameof(H3TextBlockStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? H3TextBlockStyle
    {
        get => (Style?)GetValue(H3TextBlockStyleProperty);
        set => SetValue(H3TextBlockStyleProperty, value);
    }

    public static readonly DependencyProperty H4TextBlockStyleProperty = DependencyProperty.Register(
        nameof(H4TextBlockStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? H4TextBlockStyle
    {
        get => (Style?)GetValue(H4TextBlockStyleProperty);
        set => SetValue(H4TextBlockStyleProperty, value);
    }

    public static readonly DependencyProperty H5TextBlockStyleProperty = DependencyProperty.Register(
        nameof(H5TextBlockStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? H5TextBlockStyle
    {
        get => (Style?)GetValue(H5TextBlockStyleProperty);
        set => SetValue(H5TextBlockStyleProperty, value);
    }

    public static readonly DependencyProperty H6TextBlockStyleProperty = DependencyProperty.Register(
        nameof(H6TextBlockStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? H6TextBlockStyle
    {
        get => (Style?)GetValue(H6TextBlockStyleProperty);
        set => SetValue(H6TextBlockStyleProperty, value);
    }

    #endregion

    #region Code

    public static readonly DependencyProperty CodeTextBlockStyleProperty = DependencyProperty.Register(
        nameof(CodeTextBlockStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? CodeTextBlockStyle
    {
        get => (Style?)GetValue(CodeTextBlockStyleProperty);
        set => SetValue(CodeTextBlockStyleProperty, value);
    }

    public static readonly DependencyProperty CodeInlineBorderStyleProperty = DependencyProperty.Register(
        nameof(CodeInlineBorderStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? CodeInlineBorderStyle
    {
        get => (Style?)GetValue(CodeInlineBorderStyleProperty);
        set => SetValue(CodeInlineBorderStyleProperty, value);
    }

    public static readonly DependencyProperty CodeBlockBorderStyleProperty = DependencyProperty.Register(
        nameof(CodeBlockBorderStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? CodeBlockBorderStyle
    {
        get => (Style?)GetValue(CodeBlockBorderStyleProperty);
        set => SetValue(CodeBlockBorderStyleProperty, value);
    }

    #endregion

    #region Quote

    public static readonly DependencyProperty DefaultQuoteControlStyleProperty = DependencyProperty.Register(
        nameof(DefaultQuoteControlStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? DefaultQuoteControlStyle
    {
        get => (Style?)GetValue(DefaultQuoteControlStyleProperty);
        set => SetValue(DefaultQuoteControlStyleProperty, value);
    }

    public static readonly DependencyProperty NoteQuoteControlStyleProperty = DependencyProperty.Register(
        nameof(NoteQuoteControlStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? NoteQuoteControlStyle
    {
        get => (Style?)GetValue(NoteQuoteControlStyleProperty);
        set => SetValue(NoteQuoteControlStyleProperty, value);
    }

    public static readonly DependencyProperty TipQuoteControlStyleProperty = DependencyProperty.Register(
        nameof(TipQuoteControlStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? TipQuoteControlStyle
    {
        get => (Style?)GetValue(TipQuoteControlStyleProperty);
        set => SetValue(TipQuoteControlStyleProperty, value);
    }

    public static readonly DependencyProperty ImportantQuoteControlStyleProperty = DependencyProperty.Register(
        nameof(ImportantQuoteControlStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? ImportantQuoteControlStyle
    {
        get => (Style?)GetValue(ImportantQuoteControlStyleProperty);
        set => SetValue(ImportantQuoteControlStyleProperty, value);
    }

    public static readonly DependencyProperty WarningQuoteControlStyleProperty = DependencyProperty.Register(
        nameof(WarningQuoteControlStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? WarningQuoteControlStyle
    {
        get => (Style?)GetValue(WarningQuoteControlStyleProperty);
        set => SetValue(WarningQuoteControlStyleProperty, value);
    }

    public static readonly DependencyProperty CautionQuoteControlStyleProperty = DependencyProperty.Register(
        nameof(CautionQuoteControlStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? CautionQuoteControlStyle
    {
        get => (Style?)GetValue(CautionQuoteControlStyleProperty);
        set => SetValue(CautionQuoteControlStyleProperty, value);
    }

    #endregion

    #region List

    public static readonly DependencyProperty ListStackPanelStyleProperty = DependencyProperty.Register(
        nameof(ListStackPanelStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? ListStackPanelStyle
    {
        get => (Style?)GetValue(ListStackPanelStyleProperty);
        set => SetValue(ListStackPanelStyleProperty, value);
    }

    public static readonly DependencyProperty ListBulletSpacingProperty = DependencyProperty.Register(
        nameof(ListBulletSpacing), typeof(double), typeof(MarkdownTextBlock),
        new PropertyMetadata(12.0, OnThemePropertyChanged));

    public double ListBulletSpacing
    {
        get => (double)GetValue(ListBulletSpacingProperty);
        set => SetValue(ListBulletSpacingProperty, value);
    }

    #endregion

    #region Table

    public static readonly DependencyProperty TableGridStyleProperty = DependencyProperty.Register(
        nameof(TableGridStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? TableGridStyle
    {
        get => (Style?)GetValue(TableGridStyleProperty);
        set => SetValue(TableGridStyleProperty, value);
    }

    public static readonly DependencyProperty TableCellGridStyleProperty = DependencyProperty.Register(
        nameof(TableCellGridStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? TableCellGridStyle
    {
        get => (Style?)GetValue(TableCellGridStyleProperty);
        set => SetValue(TableCellGridStyleProperty, value);
    }

    public static readonly DependencyProperty AltTableCellGridStyleProperty = DependencyProperty.Register(
        nameof(AltTableCellGridStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? AltTableCellGridStyle
    {
        get => (Style?)GetValue(AltTableCellGridStyleProperty);
        set => SetValue(AltTableCellGridStyleProperty, value);
    }

    public static readonly DependencyProperty TableHeaderCellGridStyleProperty = DependencyProperty.Register(
        nameof(TableHeaderCellGridStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? TableHeaderCellGridStyle
    {
        get => (Style?)GetValue(TableHeaderCellGridStyleProperty);
        set => SetValue(TableHeaderCellGridStyleProperty, value);
    }

    public static readonly DependencyProperty TableHeaderTextBlockStyleProperty = DependencyProperty.Register(
        nameof(TableHeaderTextBlockStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? TableHeaderTextBlockStyle
    {
        get => (Style?)GetValue(TableHeaderTextBlockStyleProperty);
        set => SetValue(TableHeaderTextBlockStyleProperty, value);
    }

    #endregion

    #region Thematic Break

    public static readonly DependencyProperty ThematicBreakBorderStyleProperty = DependencyProperty.Register(
        nameof(ThematicBreakBorderStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? ThematicBreakBorderStyle
    {
        get => (Style?)GetValue(ThematicBreakBorderStyleProperty);
        set => SetValue(ThematicBreakBorderStyleProperty, value);
    }

    #endregion

    #region Address Block

    public static readonly DependencyProperty AddressBlockTextBlockStyleProperty = DependencyProperty.Register(
        nameof(AddressBlockTextBlockStyle), typeof(Style), typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnThemePropertyChanged));

    public Style? AddressBlockTextBlockStyle
    {
        get => (Style?)GetValue(AddressBlockTextBlockStyleProperty);
        set => SetValue(AddressBlockTextBlockStyleProperty, value);
    }

    #endregion

    private static void OnThemePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownTextBlock self)
        {
            self.ApplyText(true);
        }
    }

    public DocumentOutline DocumentOutline { get; }

    public ImportsHandler ImportsHandler { get; }
}
