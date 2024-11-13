// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax.Inlines;
using Microsoft.UI.Xaml.Documents;

namespace Symptum.UI.Markdown.TextElements;

internal class MyAutolinkInline : IAddChild
{
    private AutolinkInline _autoLinkInline;
    private SInline inline;
    private Hyperlink _hyperlink;

    public STextElement TextElement { get; private set; }

    public MyAutolinkInline(AutolinkInline autoLinkInline)
    {
        _autoLinkInline = autoLinkInline;
        _hyperlink = new Hyperlink()
        {
            NavigateUri = new Uri(autoLinkInline.Url),
        };
        inline = new SInline()
        {
            Inline = _hyperlink
        };
        TextElement = inline;
    }


    public void AddChild(IAddChild child)
    {
        if (child is MyInlineText text && text.TextElement is SInline inline && inline.Inline is Run run)
            try
            {
                _hyperlink?.Inlines.Add(run);
            }
            catch (Exception ex)
            {
                throw new Exception("Error adding child to MyAutolinkInline", ex);
            }
    }
}
