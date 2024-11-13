// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Symptum.UI.Markdown;

public record MarkdownConfig
{
    public string? BaseUrl { get; set; }

    public IImageProvider? ImageProvider { get; set; }

    public ISVGRenderer? SVGRenderer { get; set; }

    public MarkdownThemes Themes { get; set; } = MarkdownThemes.Default;

    public static MarkdownConfig Default = new();
}
