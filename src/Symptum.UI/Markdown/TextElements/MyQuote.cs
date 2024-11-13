// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Helpers;
using Markdig.Syntax;

namespace Symptum.UI.Markdown.TextElements;

internal class MyQuote : IAddChild
{
    private SContainer _container;
    private MyFlowDocument _flowDocument;
    private QuoteBlock _quoteBlock;

    public STextElement TextElement
    {
        get => _container;
    }

    public MyQuote(QuoteBlock quoteBlock, MarkdownConfig config, StringSlice? kind = null)
    {
        TextBlock? _alertKindTB = null;
        _quoteBlock = quoteBlock;
        _container = new();

        _flowDocument = new MyFlowDocument(config, false);
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

            _alertKindTB = new()
            {
                Text = alertKind.ToString()
            };
            _flowDocument.StackPanel.Children.Insert(0, _alertKindTB);
        }

        Border border = new()
        {
            Child = _flowDocument.StackPanel,
            Style = alertKind switch
            {
                AlertKind.Tip => config.Themes.TipQuoteBlockStyle,
                AlertKind.Warning => config.Themes.WarningQuoteBlockStyle,
                AlertKind.Caution => config.Themes.CautionQuoteBlockStyle,
                _ => config.Themes.DefaultQuoteBlockStyle
            }
        };

        if (_alertKindTB != null) _alertKindTB.Foreground = border.BorderBrush;

        _container.UIElement = border;
    }

    public void AddChild(IAddChild child)
    {
        _flowDocument.AddChild(child);
    }
}

public enum AlertKind
{
    None,
    Note,
    Tip,
    Important,
    Warning,
    Caution,
}
