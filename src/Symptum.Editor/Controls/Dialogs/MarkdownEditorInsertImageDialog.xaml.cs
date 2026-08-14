using System.Collections.ObjectModel;
using System.Text;
using Symptum.Common.Helpers;
using Symptum.Core.Management.Resources;

namespace Symptum.Editor.Controls;

public sealed partial class MarkdownEditorInsertImageDialog : ContentDialog, IEditorDialog
{
    public EditorResult EditResult { get; private set; } = EditorResult.None;

    public string Markdown { get; private set; } = string.Empty;

    public ObservableCollection<ImageResourceItem> ResourceItems { get; } = [];

    private bool _resourcesLoaded;

    public MarkdownEditorInsertImageDialog()
    {
        InitializeComponent();
        Opened += MarkdownEditorInsertImageDialog_Opened;
        PrimaryButtonClick += MarkdownEditorInsertImageDialog_PrimaryButtonClick;
        CloseButtonClick += MarkdownEditorInsertImageDialog_CloseButtonClick;
    }

    private void MarkdownEditorInsertImageDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        urlTB.Text = null;
        altTB.Text = null;
        titleTB.Text = null;
        _resourcesLoaded = false;
        ResourceItems.Clear();
        onlinePanel.Visibility = Visibility.Visible;
        resourcesView.Visibility = Visibility.Collapsed;
        modeSelector.SelectedItem = onlineItem;
    }

    private void ModeSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem == resourcesItem)
        {
            onlinePanel.Visibility = Visibility.Collapsed;
            resourcesView.Visibility = Visibility.Visible;
            if (!_resourcesLoaded)
            {
                _resourcesLoaded = true;
                LoadResourcesAsync();
            }
        }
        else
        {
            onlinePanel.Visibility = Visibility.Visible;
            resourcesView.Visibility = Visibility.Collapsed;
        }
    }

    private async void LoadResourcesAsync()
    {
        List<ImageFileResource> images = [];
        CollectImageResources(ResourceManager.Resources, images);

        foreach (var img in images)
        {
            var item = new ImageResourceItem(img);
            var (source, _) = await ImageResourceHelper.GetImageFromResource(item.Resource);
            item.Thumbnail = source;
            ResourceItems.Add(item);
        }
    }

    private static void CollectImageResources(IReadOnlyList<IResource> resources, List<ImageFileResource> results)
    {
        foreach (var resource in resources)
        {
            if (resource is ImageFileResource imageResource)
            {
                results.Add(imageResource);
            }

            if (resource.ChildrenResources is { Count: > 0 })
            {
                CollectImageResources(resource.ChildrenResources, results);
            }
        }
    }

    private void MarkdownEditorInsertImageDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (modeSelector.SelectedItem == resourcesItem)
        {
            GenerateMarkdownFromResources();
        }
        else
        {
            GenerateMarkdown();
        }
        EditResult = EditorResult.Create;
    }

    private void MarkdownEditorInsertImageDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        EditResult = EditorResult.Cancel;
    }

    public async Task<EditorResult> InsertAsync()
    {
        await ShowAsync();
        return EditResult;
    }

    private void GenerateMarkdown()
    {
        Markdown = BuildImageMarkdown(urlTB.Text, altTB.Text, titleTB.Text);
    }

    private void GenerateMarkdownFromResources()
    {
        var selected = resourcesView.SelectedItems;
        if (selected.Count == 0) return;

        var result = new StringBuilder();
        foreach (ImageResourceItem item in selected)
        {
            string url = item.Resource.Uri?.ToString() ?? item.Resource.FilePath ?? string.Empty;
            result.AppendLine(BuildImageMarkdown(url, item.Resource.Description, item.Resource.Title));
        }

        Markdown = result.ToString().TrimEnd();
    }

    private static string BuildImageMarkdown(string url, string? alt, string? title)
    {
        var result = new StringBuilder();
        result.Append('!').Append('[');

        if (alt != null)
            result.Append(alt);

        result.Append(']').Append('(').Append(url);

        if (!string.IsNullOrWhiteSpace(title))
        {
            result.Append(' ').Append('\"').Append(title).Append('\"');
        }

        result.Append(')');
        return result.ToString();
    }
}

public partial class ImageResourceItem : ObservableObject
{
    public ImageFileResource Resource { get; }

    public string Title => Resource.Title ?? string.Empty;

    [ObservableProperty]
    public partial ImageSource? Thumbnail { get; set; }

    public ImageResourceItem(ImageFileResource resource)
    {
        Resource = resource;
    }
}
