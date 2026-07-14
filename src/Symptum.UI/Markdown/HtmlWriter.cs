using HtmlAgilityPack;
using Symptum.UI.Markdown.Renderers;
using Symptum.UI.Markdown.TextElements;
using Symptum.UI.Markdown.TextElements.Html;

namespace Symptum.UI.Markdown;

internal class HtmlWriter
{
    public static void WriteHtml(WinUIRenderer renderer, HtmlNodeCollection nodes)
    {
        if (nodes == null || nodes.Count == 0) return;
        var control = renderer.MarkdownTextBlock;
        foreach (var node in nodes)
        {
            HtmlElementType elementType = node.Name.TagToType();
            if (node.NodeType == HtmlNodeType.Text)
            {
                renderer.WriteText(node.InnerText);
            }
            else if (node.NodeType == HtmlNodeType.Element && elementType == HtmlElementType.Inline)
            {
                var inlineTagName = node.Name;
                if (string.Equals(inlineTagName, "br", StringComparison.OrdinalIgnoreCase))
                {
                    renderer.WriteInline(new LineBreakElement());
                }
                else if (string.Equals(inlineTagName, "a", StringComparison.OrdinalIgnoreCase))
                {
                    IAddChild hyperLink;
                    if (node.ChildNodes.Any(n => n.Name != "#text"))
                    {
                        hyperLink = new HyperlinkButtonElement(node, control.BaseUrl, control, renderer.LinkHandler);
                    }
                    else
                    {
                        hyperLink = new HyperlinkElement(node, control.BaseUrl, renderer.LinkHandler);
                    }
                    renderer.Push(hyperLink);
                    WriteHtml(renderer, node.ChildNodes);
                    renderer.Pop();
                }
                else if (string.Equals(inlineTagName, "img", StringComparison.OrdinalIgnoreCase))
                {
                    var image = new ImageElement(node, control);
                    renderer.WriteInline(image);
                }
                else
                {
                    var inline = new HtmlInlineElement(node);
                    renderer.Push(inline);
                    WriteHtml(renderer, node.ChildNodes);
                    renderer.Pop();
                }
            }
            else if (node.NodeType == HtmlNodeType.Element && elementType == HtmlElementType.Block)
            {
                IAddChild block;
                var tag = node.Name.ToLower();
                if (tag == "details")
                {
                    block = new HtmlDetailsElement(node, control);
                    if (node.ChildNodes.FirstOrDefault(x => x.Name == "summary" || x.Name == "header") is HtmlNode child)
                            node.ChildNodes.Remove(child);
                    renderer.Push(block);
                    WriteHtml(renderer, node.ChildNodes);
                }
                else if (tag.IsHeading())
                {
                    var heading = new HeadingElement(node, control, renderer.DocumentOutline);
                    renderer.Push(heading);
                    WriteHtml(renderer, node.ChildNodes);
                }
                else
                {
                    block = new HtmlBlockElement(node, control);
                    renderer.Push(block);
                    WriteHtml(renderer, node.ChildNodes);
                }
                renderer.Pop();
            }
        }
    }
}
