// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.UI.Xaml.Documents;

namespace Symptum.UI.Markdown.TextElements;

internal class MyLineBreak : IAddChild
{
    private SInline inline;

    public STextElement TextElement
    {
        get => inline;
    }

    public MyLineBreak()
    {
        inline = new()
        {
            Inline = new LineBreak()
        };
    }

    public void AddChild(IAddChild child) { }
}
