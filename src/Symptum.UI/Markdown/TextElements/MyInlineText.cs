// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.UI.Xaml.Documents;

namespace Symptum.UI.Markdown.TextElements;

internal class MyInlineText : IAddChild
{
    private SInline inline;

    public STextElement TextElement
    {
        get => inline;
    }

    public MyInlineText(string text)
    {
        inline = new()
        {
            Inline = new Run()
            {
                Text = text
            }
        };
    }

    public void AddChild(IAddChild child) { }
}
