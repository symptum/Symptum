using Markdig;
using Symptum.Markdown.Reference;
using Symptum.UI.Markdown.Renderers;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown;

[TemplatePart(Name = MarkdownContainerName, Type = typeof(Grid))]
public partial class MarkdownTextBlock : Control
{
    private const string MarkdownContainerName = "MarkdownContainer";
    private Grid? _container;
    private MarkdownPipeline _pipeline;
    private FlowDocumentElement _document;
    private WinUIRenderer? _renderer;

    #region Properties

    #region Configuration

    private static readonly DependencyProperty ConfigurationProperty = DependencyProperty.Register(
        nameof(Configuration),
        typeof(MarkdownConfiguration),
        typeof(MarkdownTextBlock),
        new PropertyMetadata(MarkdownConfiguration.Default, OnConfigChanged));

    public MarkdownConfiguration Configuration
    {
        get => (MarkdownConfiguration)GetValue(ConfigurationProperty);
        set => SetValue(ConfigurationProperty, value);
    }

    private static void OnConfigChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownTextBlock self && e.NewValue != null)
        {
            self.ApplyConfig(self.Configuration);
        }
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
            self.ApplyText(text, true);
        }
    }

    #endregion

    public DocumentOutline DocumentOutline { get; }

    #endregion

    public MarkdownTextBlock()
    {
        DefaultStyleKey = typeof(MarkdownTextBlock);
        _document = new FlowDocumentElement(Configuration);
        _pipeline = new MarkdownPipelineBuilder()
            .UseAlertBlocks()
            .UseEmphasisExtras()
            .UseAutoLinks()
            .UseListExtras()
            .UseTaskLists()
            .UsePipeTables()
            .UseGridTables()
            .UseAutoIdentifiers(Markdig.Extensions.AutoIdentifiers.AutoIdentifierOptions.GitHub)
            .Use<ReferenceInlineExtension>()
            .Build();
        DocumentOutline = new();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _container = (Grid)GetTemplateChild(MarkdownContainerName);
        _container.Children.Clear();
        _container.Children.Add(_document.StackPanel);
        Build();
    }

    private void ApplyConfig(MarkdownConfiguration config)
    {
        if (_renderer != null)
        {
            _renderer.Configuration = config;
        }
    }

    private void ApplyText(string text, bool rerender)
    {
        Markdig.Syntax.MarkdownDocument markdown = Markdig.Markdown.Parse(text ?? string.Empty, _pipeline);
        if (_renderer != null)
        {
            if (rerender)
            {
                _renderer.ReloadDocument();
            }
            _renderer.Render(markdown);
        }
    }

    private void Build()
    {
        if (Configuration != null)
        {
            if (_renderer == null)
            {
                _renderer = new WinUIRenderer(_document, Configuration, DocumentOutline);
            }
            _pipeline.Setup(_renderer);
            ApplyText(Text, false);
        }
    }
}
