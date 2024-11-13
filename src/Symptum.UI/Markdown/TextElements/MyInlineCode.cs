// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax.Inlines;

namespace Symptum.UI.Markdown.TextElements;

internal class MyInlineCode : IAddChild
{
    private CodeInline _codeInline;
    private SContainer _container;
    private MarkdownConfig _config;

    public STextElement TextElement
    {
        get => _container;
    }

    public MyInlineCode(CodeInline codeInline, MarkdownConfig config)
    {
        _codeInline = codeInline;
        _config = config;
        _container = new();
        Border border = new()
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = _config.Themes.InlineCodeBackground,
            BorderBrush = _config.Themes.InlineCodeBorderBrush,
            BorderThickness = _config.Themes.InlineCodeBorderThickness,
            CornerRadius = _config.Themes.InlineCodeCornerRadius,
            Padding = _config.Themes.InlineCodePadding
        };
        TextBlock textBlock = new()
        {
            FontSize = _config.Themes.InlineCodeFontSize,
            FontWeight = _config.Themes.InlineCodeFontWeight,
            Text = codeInline.Content.ToString(),
            TextWrapping = TextWrapping.Wrap
        };
        border.Child = textBlock;
        _container.UIElement = border;
    }


    public void AddChild(IAddChild child) { }
}
