namespace Symptum.UI.Markdown;

public record MarkdownConfiguration
{
    public string? BaseUrl { get; set; }

    public DocumentOutline DocumentOutline { get; private set; }

    public IImageProvider? ImageProvider { get; set; }

    public ILinkHandler? LinkHandler { get; set; }

    public ISVGRenderer? SVGRenderer { get; set; }

    public MarkdownThemes Themes { get; set; }

    public static MarkdownConfiguration Default = new();

    public MarkdownConfiguration()
    {
        DocumentOutline = new();
        LinkHandler = new DefaultLinkHandler(DocumentOutline);
        SVGRenderer = new DefaultSVGRenderer();
        Themes = MarkdownThemes.Default;
    }
}
