using Symptum.Core.Management.Navigation;
using Symptum.Core.Management.Resources;
using Symptum.Helpers;

namespace Symptum.Pages;

public sealed partial class MarkdownPage : NavigablePage
{
    private MarkdownFileResource? resource;

    public MarkdownPage()
    {
        InitializeComponent();
        button.Click += (s, e) =>
        {
            ReaderThemeHelper.ApplyTheme("Forest");
        };
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
