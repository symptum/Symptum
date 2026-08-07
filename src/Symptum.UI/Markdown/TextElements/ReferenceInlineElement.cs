using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Symptum.Markdown.Reference;

namespace Symptum.UI.Markdown.TextElements;

public class ReferenceInlineElement : IAddChild
{
    private ReferenceInline _referenceInline;
    private SInline _inline;
    private IReferenceValueResolver? _referenceValueResolver;
    private ILinkHandler? _linkHandler;
    private string? _baseUrl;

    public STextElement TextElement => _inline;

    public ReferenceInlineElement(ReferenceInline referenceInline, IReferenceValueResolver? referenceValueResolver = null,
        ILinkHandler? linkHandler = null, string? baseUrl = null)
    {
        _referenceInline = referenceInline;
        _referenceValueResolver = referenceValueResolver;
        _linkHandler = linkHandler;
        _baseUrl = baseUrl;

        _inline = new SInline()
        {
            Inline = new Run()
            {
                Text = referenceInline.Content.ToString()
            }
        };

        if (_referenceValueResolver != null)
        {
            _ = ResolveAsync();
        }
    }

    private async Task ResolveAsync()
    {
        if (_referenceValueResolver == null) return;

        try
        {
            var result = await _referenceValueResolver.ResolveAsync(_referenceInline.Content.ToString());
            if (result == null) return;

            var hyperlink = new Hyperlink();
            hyperlink.Click += (s, e) => _linkHandler?.HandleNavigation(result.Value.Url, _baseUrl);
            hyperlink.Inlines.Add(new Run { Text = result.Value.Text });
            ToolTipService.SetToolTip(hyperlink, result.Value.Url);
            _inline.Inline = hyperlink;
        }
        catch { }
    }

    public void AddChild(IAddChild child) { }
}
