// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Renderers;
using Markdig.Syntax;

namespace Symptum.UI.Markdown.Renderers;

public abstract class WinUIObjectRenderer<TObject> : MarkdownObjectRenderer<WinUIRenderer, TObject>
    where TObject : MarkdownObject
{
}
