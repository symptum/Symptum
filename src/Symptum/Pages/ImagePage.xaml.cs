using Symptum.Common.Helpers;
using Symptum.Core.Management.Navigation;
using Symptum.Core.Management.Resources;

namespace Symptum.Pages;

public sealed partial class ImagePage : NavigablePage
{
    private ImageFileResource? _imageResource;

    public ImagePage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    protected override void OnNavigableChanged(INavigable? navigable)
    {
        if (navigable is ImageFileResource imageResource)
        {
            _imageResource = imageResource;
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_imageResource == null) return;

        var (imageSource, fileSize) = await ImageResourceHelper.GetImageFromResource(_imageResource);
        imageViewer.FileSize = fileSize;
        imageViewer.Source = imageSource;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _imageResource = null;
        imageViewer.Source = null;
        imageViewer.FileSize = 0;
        imageViewer.Unload();
    }
}
