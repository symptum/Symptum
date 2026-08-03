using Windows.UI;

namespace Symptum.UI.Markdown.Mermaid;

internal sealed class MermaidPalette(
    Brush surface,
    Brush border,
    Brush diagram,
    Brush nodeFill,
    Brush nodeStroke,
    Brush edge,
    Brush text,
    Brush metaText,
    Brush[] chartColors)
{
    public Brush Surface { get; } = surface;

    public Brush Border { get; } = border;

    public Brush Diagram { get; } = diagram;

    public Brush NodeFill { get; } = nodeFill;

    public Brush NodeStroke { get; } = nodeStroke;

    public Brush Edge { get; } = edge;

    public Brush Text { get; } = text;

    public Brush MetaText { get; } = metaText;

    public Brush[] ChartColors { get; } = chartColors;
}

internal static class MermaidTheme
{
    private static readonly Color LightSurface = Color.FromArgb(255, 0xF3, 0xF3, 0xF3);
    private static readonly Color LightBorder = Color.FromArgb(255, 0x5C, 0x5C, 0x5C);
    private static readonly Color LightDiagram = Color.FromArgb(255, 0xF3, 0xF3, 0xF3);
    private static readonly Color LightNodeFill = Color.FromArgb(255, 0xFF, 0xFF, 0xFF);
    private static readonly Color LightNodeStroke = Color.FromArgb(255, 0x5C, 0x5C, 0x5C);
    private static readonly Color LightEdge = Color.FromArgb(255, 0x61, 0x61, 0x61);
    private static readonly Color LightText = Color.FromArgb(255, 0x00, 0x00, 0x00);
    private static readonly Color LightMetaText = Color.FromArgb(255, 0x76, 0x76, 0x76);

    private static readonly Color DarkSurface = Color.FromArgb(255, 0x2D, 0x2D, 0x30);
    private static readonly Color DarkBorder = Color.FromArgb(255, 0x81, 0x81, 0x81);
    private static readonly Color DarkDiagram = Color.FromArgb(255, 0x2D, 0x2D, 0x30);
    private static readonly Color DarkNodeFill = Color.FromArgb(255, 0x23, 0x23, 0x23);
    private static readonly Color DarkNodeStroke = Color.FromArgb(255, 0x81, 0x81, 0x81);
    private static readonly Color DarkEdge = Color.FromArgb(255, 0xC7, 0xC7, 0xC7);
    private static readonly Color DarkText = Color.FromArgb(255, 0xFF, 0xFF, 0xFF);
    private static readonly Color DarkMetaText = Color.FromArgb(255, 0x81, 0x81, 0x81);

    private static readonly Brush[] DefaultChartColors =
    [
        ToBrush(Color.FromArgb(255, 0x60, 0xA5, 0xFA)),
        ToBrush(Color.FromArgb(255, 0x34, 0xD3, 0x99)),
        ToBrush(Color.FromArgb(255, 0xFF, 0xA5, 0x00)),
        ToBrush(Color.FromArgb(255, 0xEA, 0x54, 0x5D)),
        ToBrush(Color.FromArgb(255, 0x9B, 0x6D, 0xE2)),
        ToBrush(Color.FromArgb(255, 0x2F, 0xC4, 0xCE)),
        ToBrush(Color.FromArgb(255, 0xF7, 0x5C, 0x9E)),
        ToBrush(Color.FromArgb(255, 0x8B, 0xC8, 0x4A))
    ];

    public static MermaidPalette Create(ElementTheme? requestedTheme = null)
    {
        bool dark = requestedTheme switch
        {
            ElementTheme.Dark => true,
            ElementTheme.Light => false,
            _ => Application.Current?.RequestedTheme == ApplicationTheme.Dark
        };

        return dark ? CreateDark() : CreateLight();
    }

    public static double ResolveFontSize()
    {
        if (Application.Current?.Resources.TryGetValue("MarkdownBodyFontSize", out var resource) == true &&
            resource is double fontSize && fontSize > 0)
        {
            return fontSize;
        }

        return 14;
    }

    private static MermaidPalette CreateLight()
    {
        return new MermaidPalette(
            Resolve("SolidBackgroundFillColorSecondaryBrush", LightSurface),
            Resolve("ControlStrongStrokeColorDefaultBrush", LightBorder),
            Resolve("SolidBackgroundFillColorSecondaryBrush", LightDiagram),
            Resolve("CardBackgroundFillColorDefaultBrush", LightNodeFill),
            Resolve("ControlStrongStrokeColorDefaultBrush", LightNodeStroke),
            Resolve("TextFillColorSecondaryBrush", LightEdge),
            Resolve("TextFillColorPrimaryBrush", LightText),
            Resolve("TextFillColorTertiaryBrush", LightMetaText),
            DefaultChartColors);
    }

    private static MermaidPalette CreateDark()
    {
        return new MermaidPalette(
            Resolve("SolidBackgroundFillColorSecondaryBrush", DarkSurface),
            Resolve("ControlStrongStrokeColorDefaultBrush", DarkBorder),
            Resolve("SolidBackgroundFillColorSecondaryBrush", DarkDiagram),
            Resolve("CardBackgroundFillColorDefaultBrush", DarkNodeFill),
            Resolve("ControlStrongStrokeColorDefaultBrush", DarkNodeStroke),
            Resolve("TextFillColorSecondaryBrush", DarkEdge),
            Resolve("TextFillColorPrimaryBrush", DarkText),
            Resolve("TextFillColorTertiaryBrush", DarkMetaText),
            DefaultChartColors);
    }

    private static Brush Resolve(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out object? resource) == true && resource is Brush brush)
        {
            return brush;
        }

        return ToBrush(fallback);
    }

    private static SolidColorBrush ToBrush(Color color) => new(color);
}
