// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax.Inlines;

namespace Symptum.UI.Markdown.TextElements;

internal class MyHyperlinkButton : IAddChild
{
    private HyperlinkButton? _hyperLinkButton;
    private SContainer _container;
    private MyFlowDocument? _flowDoc;
    private string? _baseUrl;
    private LinkInline? _linkInline;

    public STextElement TextElement
    {
        get => _container;
    }

    public MyHyperlinkButton(LinkInline linkInline, string? baseUrl)
    {
        _container = new();
        _baseUrl = baseUrl;
        string? url = linkInline.GetDynamicUrl != null ? linkInline.GetDynamicUrl() ?? linkInline.Url : linkInline.Url;
        _linkInline = linkInline;
        Init(url, baseUrl);
    }

    private void Init(string? url, string? baseUrl)
    {
        _hyperLinkButton = new HyperlinkButton
        {
            NavigateUri = Extensions.GetUri(url, baseUrl),
            Padding = new Thickness(0),
            Margin = new Thickness(0)
        };
        if (_linkInline != null)
        {
            _flowDoc = new MyFlowDocument(MarkdownConfig.Default, false);
        }
        _container.UIElement = _hyperLinkButton;
        _hyperLinkButton.Content = _flowDoc?.StackPanel;
    }

    public void AddChild(IAddChild child)
    {
        _flowDoc?.AddChild(child);
    }
}
