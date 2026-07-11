using Microsoft.UI.Xaml.Media.Imaging;
using Symptum.Core.Management.Resources;
using Windows.Storage.Streams;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Common.Helpers;

public class ImageResourceHelper
{
    public static async Task<ImageSource?> GetImageFromResource(ImageFileResource imageFileResource)
    {
        using IRandomAccessStream? stream = await ResourceHelper.OpenFileForReadAsync(imageFileResource);
        if (stream == null) return null;

        if (SvgFileExtension.Equals(imageFileResource.FileExtension, StringComparison.InvariantCultureIgnoreCase))
        {
            SvgImageSource svg = new();
            await svg.SetSourceAsync(stream);
            return svg;
        }
        else
        {
            // NOTE: IRandomAccessStream doesn't seem to render in WASM?
            BitmapImage bitmap = new();
            await bitmap.SetSourceAsync(stream);
            return bitmap;
        }
    }
}
