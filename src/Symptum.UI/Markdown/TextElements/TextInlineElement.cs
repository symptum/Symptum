using Microsoft.UI.Xaml.Documents;

namespace Symptum.UI.Markdown.TextElements;

public class TextInlineElement : IAddChild
{
    private SInline inline;

    public STextElement TextElement
    {
        get => inline;
    }

    public TextInlineElement(string text)
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
