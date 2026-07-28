using Microsoft.UI.Xaml.Media.Imaging;
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

        try
        {
            BitmapImage bitmap = new()
            {
                UriSource = new Uri("ms-appx:///Assets/Images/Symptum.png")
            };
            imageViewer.Source = bitmap;
        }
        catch
        {
            // ignore, leave placeholder
        }
    }
}
