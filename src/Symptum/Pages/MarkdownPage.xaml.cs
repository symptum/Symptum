using Symptum.Core.Management.Navigation;
using Symptum.Core.Management.Resources;

namespace Symptum.Pages;

public sealed partial class MarkdownPage : NavigablePage
{
    private MarkdownFileResource? resource;

    public MarkdownPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigableChanged(INavigable? navigable)
    {
        if (navigable is MarkdownFileResource md)
        {
            resource = md;
            markdownView.Text = md.Markdown;
        }
    }
}
