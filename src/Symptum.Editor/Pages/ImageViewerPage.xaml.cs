using Symptum.Common.Helpers;
using Symptum.Core.Management.Resources;
using Symptum.Editor.Common;
using Symptum.Editor.Controls;

namespace Symptum.Editor.Pages;

public sealed partial class ImageViewerPage : EditorPageBase
{
    private ImageFileResource? _imageFileResource;
    private ResourcePropertiesEditorDialog? propertyEditorDialog;

    public ImageViewerPage()
    {
        InitializeComponent();
        PageName = "Image Viewer";
        IconSource = DefaultIconSources.PhotoIconSource;
        Loaded += ImageViewerPage_Loaded;
    }

    private async void ImageViewer_ActionButtonClick(object sender, EventArgs e)
    {
        if (_imageFileResource != null && propertyEditorDialog != null)
        {
            propertyEditorDialog.XamlRoot = XamlRoot;
            var result = await propertyEditorDialog.EditAsync(_imageFileResource);
            if (result == EditorResult.Update)
            {
                HasUnsavedChanges = true;
                WriteToOutput($"Updated properties: {_imageFileResource.Title}");
            }
        }
    }

    protected override void OnCleanupPage()
    {
        imageViewer.Source = null;
        imageViewer.FileSize = 0;
        _imageFileResource = null;
        propertyEditorDialog = null;
    }

    private async void ImageViewerPage_Loaded(object sender, RoutedEventArgs e)
    {
        propertyEditorDialog = EditorPagesManager.CreateOrGetDialog<ResourcePropertiesEditorDialog>();
        if (EditableContent is not ImageFileResource imageFileResource) return;

        _imageFileResource = imageFileResource;

        var (imageSource, fileSize) = await ImageResourceHelper.GetImageFromResource(imageFileResource);
        imageViewer.FileSize = fileSize;
        imageViewer.Source = imageSource;

        WriteToOutput($"Loaded image: {imageFileResource.Title}");
    }
}
