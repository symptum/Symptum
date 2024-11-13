// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.UI.Text;
using Windows.UI.Text;

namespace Symptum.UI.Markdown.TextElements;

public class MyFlowDocument : IAddChild
{
    private StackPanel _stackPanel = new();

    // useless property
    public STextElement TextElement { get; set; }
    //

    public StackPanel StackPanel
    {
        get => _stackPanel;
        set => _stackPanel = value;
    }

    public FontWeight FontWeight { get; set; } = FontWeights.Normal;

    public MyFlowDocument(MarkdownConfig config, bool isTopLevel = true)
    {
        if (config != null)
        {
            _stackPanel.Spacing = config.Themes.Spacing;
            if (isTopLevel) _stackPanel.Padding = config.Themes.Padding;
        }

        TextElement = new SContainer() { UIElement = _stackPanel };
    }

    public void AddChild(IAddChild child)
    {
        STextElement element = child.TextElement;
        if (element != null)
        {
            if (element is SInline inline)
            {
                TextBlock _textBlock = new()
                {
                    FontWeight = FontWeight,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };
                _textBlock.Inlines.Add(inline.Inline);
                _stackPanel.Children.Add(_textBlock);
            }
            else if (element is SBlock block && block.GetUIElement() is UIElement ui)
            {
                _stackPanel.Children.Add(ui);
            }
        }
    }
}
