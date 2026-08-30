using Symptum.Common.Helpers;
using Symptum.Common.ProjectSystem;
using Symptum.Core.Management.Resources;
using Symptum.Editor.Controls;
using Symptum.UI;

namespace Symptum.Editor.Pages;

public sealed partial class ImageViewerPage : EditorPageBase
{
    private ImageFileResource? _imageFileResource;
    private ResourcePropertiesEditorDialog? propertyEditorDialog;

    public ImageViewerPage()
    {
        InitializeComponent();
        PageName = "Image Viewer";
        IconSource = IconHelper.PhotoIconSource;
        Loaded += ImageViewerPage_Loaded;
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

    private bool _isBeingSaved = false;

    private async void ImageViewer_ActionButtonClicked(object sender, EventArgs e)
    {
        if (_imageFileResource != null && propertyEditorDialog != null)
        {
            propertyEditorDialog.XamlRoot = XamlRoot;
            var result = await propertyEditorDialog.EditAsync(_imageFileResource);
            if (result == EditorResult.Update)
            {
                if (_isBeingSaved) return;

                _isBeingSaved = true;
                HasUnsavedChanges = !await ProjectSystemManager.SaveResourceAndAncestorAsync(_imageFileResource);
                _isBeingSaved = false;

                WriteToOutput($"Updated properties and saved: {_imageFileResource.Title}");
            }
        }
    }

    protected override void OnCleanupPage()
    {
        imageViewer.Source = null;
        imageViewer.FileSize = 0;
        imageViewer.Unload();
        _imageFileResource = null;
        propertyEditorDialog = null;
    }
}
