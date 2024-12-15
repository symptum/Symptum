using Symptum.Common.Helpers;
using Symptum.Core.Subjects.Books;

namespace Symptum.Common;

public class Bootstrapper
{
    public static async Task InitializeAsync()
    {
        await PackageHelper.InitializeAsync();
        StorageHelper.Initialize();
        try
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Books/First Year Books.csv"));
            string content = await FileIO.ReadTextAsync(file);
            BookStore.LoadBooks(content);
            file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Books/Second Year Books.csv"));
            content = await FileIO.ReadTextAsync(file);
            BookStore.LoadBooks(content);
            file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Books/Third Year Books.csv"));
            content = await FileIO.ReadTextAsync(file);
            BookStore.LoadBooks(content);
            file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Books/Final Year Books.csv"));
            content = await FileIO.ReadTextAsync(file);
            BookStore.LoadBooks(content);
        }
        catch { }
    }
}
