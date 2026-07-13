using Markdig.Syntax.Inlines;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents;
using Windows.UI.Text;

namespace Symptum.UI.Markdown.TextElements;

public class EmphasisInlineElement : IAddChild, ICascadeChild
{
    private Span? _span;
    private SInline inline;
    private SContainer _container;
    private EmphasisInline _markdownObject;
    private MarkdownConfiguration _config;
    private TextBlock? _textBlock;
    private CommunityToolkit.WinUI.Controls.WrapPanel? _wrapPanel;

    private bool _isBold;
    private bool _isItalic;
    private bool _isStrikeThrough;
    private bool _isSubscript;
    private bool _isSuperscript;

    private bool _containsUI = false;

    // We use a Container when there are UIElements within the Inlines such as Sub/Superscript, etc.
    // Else we use an Inline which will get added to the parent Paragraph's Inlines.
    public STextElement TextElement => _containsUI ? _container : inline;

    public EmphasisInlineElement(EmphasisInline emphasisInline, MarkdownConfiguration config)
    {
        _span = new Span();
        _markdownObject = emphasisInline;
        _config = config;
        inline = new() { Inline = _span };
        _container = new();
    }

    public void AddChild(IAddChild child)
    {
        if (child == null) return;
        if (child is ICascadeChild cascadeChild)
            cascadeChild.InheritProperties(this);

        if (child.TextElement is SInline _inline)
        {
            if (_containsUI)
            {
                EnsureTextBlock();
                _textBlock!.Inlines.Add(_inline.Inline);
            }
            else
            {
                _span?.Inlines.Add(_inline.Inline);
            }
        }
        else if (child.TextElement is SContainer container)
        {
            _containsUI = true;
            // If the first child is a container no need to create a TextBlock before it.
            // Only create a TextBlock and copy the inlines if there are pre-existing inlines.
            if (_span?.Inlines.Count > 0) EnsureTextBlock();
            EnsureWrapPanel().Children.Add(container.UIElement);
            // When a container is added after a TextBlock the subsequent inlines should not be added to the same TextBlock.
            // So to force create a new one, we are removing the current one.
            _textBlock = null;
            _span = null; // To prevent adding the span to the new TextBlock.
        }
    }

    public void SetBold()
    {
        _isBold = true;
        if (!_containsUI) _span?.FontWeight = FontWeights.Bold;
        else _textBlock?.FontWeight = FontWeights.Bold;
    }

    public void SetItalic()
    {
        _isItalic = true;
        if (!_containsUI) _span?.FontStyle = FontStyle.Italic;
        else _textBlock?.FontStyle = FontStyle.Italic;
    }

    public void SetStrikeThrough()
    {
        _isStrikeThrough = true;
        if (!_containsUI) _span?.TextDecorations = TextDecorations.Strikethrough;
        else _textBlock?.TextDecorations = TextDecorations.Strikethrough;
    }

    public void SetSubscript()
    {
        _isSubscript = true;
        ConstructSubSuperContainer();
    }

    public void SetSuperscript()
    {
        _isSuperscript = true;
        ConstructSubSuperContainer();
    }

    private CommunityToolkit.WinUI.Controls.WrapPanel EnsureWrapPanel()
    {
        if (_wrapPanel == null)
        {
            _wrapPanel = new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalSpacing = 4,
                VerticalSpacing = 2,
            };

            if (_textBlock != null && _container.UIElement == _textBlock)
                _wrapPanel.Children.Add(_textBlock);

            _container.UIElement = _wrapPanel;
        }

        return _wrapPanel;
    }

    private void EnsureTextBlock()
    {
        if (_textBlock == null)
        {
            CreateBaseTextBlock();
            if (_span != null) _textBlock!.Inlines.Add(_span);
        }
    }

    private void CreateBaseTextBlock()
    {
        _textBlock = new TextBlock()
        {
            Style = _config.Themes.BodyTextBlockStyle,
            IsTextSelectionEnabled = _config.IsTextSelectionEnabled
        };
        if (_isBold) _textBlock.FontWeight = FontWeights.Bold;
        if (_isItalic) _textBlock.FontStyle = FontStyle.Italic;
        if (_isStrikeThrough) _textBlock.TextDecorations = TextDecorations.Strikethrough;
        EnsureWrapPanel().Children.Add(_textBlock);
    }

    private void ConstructSubSuperContainer()
    {
        _containsUI = true;
        _textBlock = new TextBlock
        {
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = GetBaseFontSize() * 0.7,
            IsTextSelectionEnabled = _config.IsTextSelectionEnabled
        };

        double offset = _isSuperscript ? -0.45 : 0.15;
        // -4 is for WrapPanel's HorizontalSpacing
        _textBlock.Margin = new Thickness(-4, GetBaseFontSize() * offset, 0, 0);

        _container.UIElement = _textBlock;
    }

    public void InheritProperties(IAddChild parent)
    {
        if (!_isSubscript && !_isSuperscript || _textBlock == null)
            return;

        double fontSize = GetBaseFontSize();
        FontWeight? fontWeight = null;
        FontStyle? fontStyle = null;
        Brush? foreground = null;

        if (parent is IAddChild parentElement)
        {
            if (parentElement.TextElement is SInline parentInline && parentInline.Inline is TextElement parentTextElement)
            {
                if (parentTextElement.FontSize > 0)
                    fontSize = parentTextElement.FontSize;
                fontWeight = parentTextElement.FontWeight;
                fontStyle = parentTextElement.FontStyle;
                foreground = parentTextElement.Foreground;
            }
            else if (parentElement is EmphasisInlineElement parentEmphasis)
            {
                if (parentEmphasis._span?.FontSize > 0)
                    fontSize = parentEmphasis._span.FontSize;
                fontWeight = parentEmphasis._span?.FontWeight;
                fontStyle = parentEmphasis._span?.FontStyle;
                foreground = parentEmphasis._span?.Foreground;
            }
        }

        _textBlock.FontSize = fontSize * 0.7;
        if (fontWeight != null) _textBlock.FontWeight = fontWeight.Value;
        if (fontStyle != null) _textBlock.FontStyle = fontStyle.Value;
        if (foreground != null) _textBlock.Foreground = foreground;

        double offset = _isSuperscript ? -0.45 : 0.15;
        _textBlock.Margin = new Thickness(-4, fontSize * offset, 0, 0);
    }

    private double GetBaseFontSize()
    {
        if (_config?.Themes.BodyTextBlockStyle?.Setters.FirstOrDefault(
            s => s is Setter sizeSetter && sizeSetter.Property == TextBlock.FontSizeProperty) is Setter { Value: double size })
        {
            return size;
        }
        return 14;
    }
}
