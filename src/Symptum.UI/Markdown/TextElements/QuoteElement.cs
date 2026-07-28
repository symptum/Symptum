using Markdig.Helpers;
using Markdig.Syntax;

namespace Symptum.UI.Markdown.TextElements;

public class QuoteElement : IAddChild
{
    private SContainer _container;
    private FlowDocumentElement _flowDocument;
    private QuoteBlock _quoteBlock;

    public STextElement TextElement => _container;

    public QuoteElement(QuoteBlock quoteBlock, MarkdownTextBlock control, StringSlice? kind = null)
    {
        _quoteBlock = quoteBlock;
        _container = new();

        _flowDocument = new FlowDocumentElement(control, false);
        AlertKind alertKind = AlertKind.None;

        if (kind != null && kind?.Length < 16)
        {
            Span<char> upperKind = stackalloc char[kind?.Length ?? 0];
            kind?.AsSpan().ToUpperInvariant(upperKind);
            alertKind = upperKind switch
            {
                "NOTE" => AlertKind.Note,
                "TIP" => AlertKind.Tip,
                "IMPORTANT" => AlertKind.Important,
                "WARNING" => AlertKind.Warning,
                "CAUTION" => AlertKind.Caution,
                _ => AlertKind.None
            };
        }

        QuoteControl quote = new()
        {
            Kind = alertKind,
            Content = _flowDocument.StackPanel
        };

        _container.UIElement = quote;

        quote.Style = alertKind switch
        {
            AlertKind.Note => control.NoteQuoteControlStyle,
            AlertKind.Tip => control.TipQuoteControlStyle,
            AlertKind.Important => control.ImportantQuoteControlStyle,
            AlertKind.Warning => control.WarningQuoteControlStyle,
            AlertKind.Caution => control.CautionQuoteControlStyle,
            _ => control.DefaultQuoteControlStyle
        };
    }

    public void AddChild(IAddChild child)
    {
        _flowDocument.AddChild(child);
    }
}
