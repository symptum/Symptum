using Microsoft.UI.Xaml.Media.Imaging;
using Symptum.Core.Management.Resources;
using Windows.Storage.Streams;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Common.Helpers;

public class ImageResourceHelper
{
    public static async Task<(ImageSource?, ulong)> GetImageFromResource(ImageFileResource imageFileResource)
    {
        using IRandomAccessStream? stream = await ResourceHelper.OpenFileForReadAsync(imageFileResource);
        if (stream == null) return (null, 0);

        try
        {
            if (SvgFileExtension.Equals(imageFileResource.FileExtension, StringComparison.InvariantCultureIgnoreCase))
            {
                SvgImageSource svg = new();
                await svg.SetSourceAsync(stream);
                return (svg, stream.Size);
            }
            else
            {
                BitmapImage bitmap = new();
                await bitmap.SetSourceAsync(stream);
                return (bitmap, stream.Size);
            }
        } catch {
            return (null, 0);
        } finally {
            stream.Dispose();
        }
    }
}
