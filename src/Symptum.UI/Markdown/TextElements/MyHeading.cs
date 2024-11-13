// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax;

namespace Symptum.UI.Markdown.TextElements;

internal class MyHeading : IAddChild
{
    private SParagraph _paragraph;
    private HeadingBlock? _headingBlock;
    private MarkdownConfig _config;

    public STextElement TextElement
    {
        get => _paragraph;
    }

    public MyHeading(HeadingBlock headingBlock, MarkdownConfig config)
    {
        _headingBlock = headingBlock;
        _paragraph = new();
        _config = config;

        int level = headingBlock.Level;
        _paragraph.FontSize = level switch
        {
            1 => _config.Themes.H1FontSize,
            2 => _config.Themes.H2FontSize,
            3 => _config.Themes.H3FontSize,
            4 => _config.Themes.H4FontSize,
            5 => _config.Themes.H5FontSize,
            _ => _config.Themes.H6FontSize,
        };
        _paragraph.Foreground = level switch
        {
            1 => _config.Themes.H1Foreground,
            2 => _config.Themes.H2Foreground,
            3 => _config.Themes.H3Foreground,
            4 => _config.Themes.H4Foreground,
            5 => _config.Themes.H5Foreground,
            _ => _config.Themes.H6Foreground,
        };
        _paragraph.FontWeight = level switch
        {
            1 => _config.Themes.H1FontWeight,
            2 => _config.Themes.H2FontWeight,
            3 => _config.Themes.H3FontWeight,
            4 => _config.Themes.H4FontWeight,
            5 => _config.Themes.H5FontWeight,
            _ => _config.Themes.H6FontWeight,
        };
    }

    public void AddChild(IAddChild child)
    {
        if (child.TextElement is SInline inlineChild)
        {
            _paragraph.Inlines.Add(inlineChild.Inline);
        }
    }
}
