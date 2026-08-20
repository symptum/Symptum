using System.Diagnostics;
using Markdig;
using Markdig.Syntax;
using Symptum.Markdown;
using Symptum.UI.Markdown.Renderers;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown;

[TemplatePart(Name = MarkdownContainerName, Type = typeof(Grid))]
public partial class MarkdownTextBlock : Control
{
    private const string MarkdownContainerName = "MarkdownContainer";
    private Grid? _container;
    internal MarkdownPipeline _pipeline;
    private FlowDocumentElement _document;
    private WinUIRenderer? _renderer;
    private CancellationTokenSource? _parseCts;

    public MarkdownTextBlock()
    {
        DefaultStyleKey = typeof(MarkdownTextBlock);
        _document = new FlowDocumentElement(this);
        _pipeline = MarkdownManager.Pipeline;
        DocumentOutline = new();
        ImportsHandler = new();
        LinkHandler = new DefaultLinkHandler(DocumentOutline);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _container = (Grid)GetTemplateChild(MarkdownContainerName);
        _container.Children.Clear();
        _container.Children.Add(_document.StackPanel);
        Build();
    }

    private async void ApplyText(bool rerender)
    {
        if (_renderer != null)
        {
            if (rerender)
            {
                _renderer.ReloadDocument();
            }

            _parseCts?.Cancel();
            _parseCts?.Dispose();
            _parseCts = new CancellationTokenSource();
            var ct = _parseCts.Token;
            string text = Text;

            MarkdownDocument? markdown = null;
            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    var pipeline = _pipeline;
                    markdown = await Task.Run(() => Markdig.Markdown.Parse(text, pipeline), ct);
                }
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                Debug.WriteLine($"Markdown parse failed: {ex.Message}");
            }

            if (markdown != null && !ct.IsCancellationRequested)
            {
                MarkdownDocument = markdown;
                MarkdownParsed?.Invoke(this, new(markdown));
                _renderer.Render(markdown);
                MarkdownRendered?.Invoke(this, null);
            }
        }
        else _document.StackPanel.Children.Clear();
    }

    private void Build()
    {
        _renderer ??= new WinUIRenderer(this, _document);
        _pipeline.Setup(_renderer);
        ApplyText(false);
    }

    public event EventHandler<MarkdownParsedEventArgs>? MarkdownParsed;

    public event EventHandler? MarkdownRendered;

    public void Unload()
    {
        _parseCts?.Cancel();
        _parseCts?.Dispose();
        _parseCts = null;

        if (_renderer != null)
        {
            _renderer.Dispose();
            _renderer = null;
        }

        _document.StackPanel.Children.Clear();
        DocumentOutline.Clear();

        if (_container != null)
        {
            _container.Children.Clear();
            _container = null;
        }

        MarkdownParsed = null;
        MarkdownRendered = null;
    }
}
