using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents;
using Windows.UI.Text;

namespace Symptum.UI.Markdown.TextElements;

public class STextElement
{ }

public class SInline : STextElement
{
    public Inline Inline { get; set; }
}

public class SBlock : STextElement
{
    public virtual UIElement? GetUIElement() => null;
}

public class SParagraph : SBlock
{
    public List<Inline> Inlines { get; private set; } = [];

    public double FontSize { get; internal set; }

    public Brush? Foreground { get; internal set; } = null;

    public FontWeight FontWeight { get; internal set; } = FontWeights.Normal;

    public TextAlignment TextAlignment { get; internal set; } = TextAlignment.Left;

    #region Include UI

    // Indices of the inlines where the UIElements should be inserted.
    public List<int> UIIndices { get; private set; } = [];

    public bool ContainsUI { get; internal set; } = false;

    public List<UIElement> UIElements { get; private set; } = [];

    #endregion

    public override UIElement? GetUIElement()
    {
        if (!ContainsUI || UIIndices.Count == 0)
        {
            return CreateTextBlock(Inlines);
        }
        else // Ugly hack to include UIElements in Paragraph
        {
            CommunityToolkit.WinUI.Controls.WrapPanel wrapPanel = new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalSpacing = 4,
                VerticalSpacing = 2
            };

            for (int i = 0; i < UIIndices.Count; i++)
            {
                int j = i > 0 ? UIIndices[i - 1] : 0; // Gets the previous index (0 if no previous index).
                int k = UIIndices[i]; // Gets the index.
                if (j < k)
                {
                    List<Inline> _inlines = Inlines[j..k]; // Get the inlines in the index range.
                    wrapPanel.Children.Add(CreateTextBlock(_inlines));
                }
                wrapPanel.Children.Add(UIElements[i]);

                if (i == UIIndices.Count - 1 && UIIndices[i] < Inlines.Count) // Adding trailing inlines.
                {
                    List<Inline> _inlines = Inlines[k..];
                    wrapPanel.Children.Add(CreateTextBlock(_inlines));
                }
            }

            return wrapPanel;
        }
    }

    private TextBlock CreateTextBlock(List<Inline> inlines)
    {
        TextBlock _textBlock = new()
        {
            FontWeight = FontWeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            TextAlignment = TextAlignment,
            TextWrapping = TextWrapping.Wrap
        };

        if (FontSize > 0)
            _textBlock.FontSize = FontSize;
        if (Foreground != null)
            _textBlock.Foreground = Foreground;

        foreach (Inline _inline in inlines)
            _textBlock.Inlines.Add(_inline);

        return _textBlock;
    }
}

public class SContainer : SBlock
{
    public UIElement? UIElement { get; set; }

    public bool PutUIInfront { get; internal set; } = false;

    public override UIElement? GetUIElement() => UIElement;
}
