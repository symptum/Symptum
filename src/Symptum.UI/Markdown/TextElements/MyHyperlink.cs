// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax.Inlines;
using Microsoft.UI.Xaml.Documents;

namespace Symptum.UI.Markdown.TextElements;

internal class MyHyperlink : IAddChild
{
    private Hyperlink _hyperlink;
    private SInline inline;
    private LinkInline? _linkInline;
    private string? _baseUrl;

    public STextElement TextElement
    {
        get => inline;
    }

    public MyHyperlink(LinkInline linkInline, string? baseUrl)
    {
        _baseUrl = baseUrl;
        string? url = linkInline.GetDynamicUrl != null ? linkInline.GetDynamicUrl() ?? linkInline.Url : linkInline.Url;
        _linkInline = linkInline;
        _hyperlink = new Hyperlink()
        {
            NavigateUri = Extensions.GetUri(url, baseUrl),
        };
        ToolTipService.SetToolTip(_hyperlink, linkInline.Title);
        inline = new()
        {
            Inline = _hyperlink
        };
    }

    public void AddChild(IAddChild child)
    {
        if (child.TextElement is SInline inlineChild)
        {
            try
            {
                _hyperlink.Inlines.Add(inlineChild.Inline);
                // TODO: Add support for click handler
            }
            catch { }
        }
    }
}
