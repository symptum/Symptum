using Symptum.Core.Management.Navigation;
using Symptum.Core.Management.Resources;
using Symptum.Services;

namespace Symptum.Pages;

public sealed partial class MarkdownPage : NavigablePage
{
    private MarkdownFileResource? resource;

    public MarkdownPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ThemeService.ThemeChanged += OnThemeChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ThemeService.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ReRenderMarkdown();
    }

    protected override void OnNavigableChanged(INavigable? navigable)
    {
        if (navigable is MarkdownFileResource md)
        {
            resource = md;
            markdownView.Text = md.Markdown;
        }
    }

    private void ReRenderMarkdown()
    {
        if (resource != null)
        {
            var text = resource.Markdown;
            markdownView.Text = string.Empty;
            markdownView.Text = text;
        }
    }
}
