using Symptum.Core.Helpers;
using System.Web;
using Symptum.Core.Subjects.Books;
using Symptum.Core.Data.ReferenceValues;

namespace Symptum.Editor;

public class App : Application
{
    public Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        //ReferenceValueEntry entry = new()
        //{
        //    Title = "Lymphocytes",
        //    Data = [ new() { Values = "[x,y] md/dL"}, new() { Values = "x %"} ],
        //    Inference = "Lymphocytosis",
        //    Remarks = "Hello bro! +=-`~$%& 😂😊(❁´◡`❁)©£←→ \r\n kskmsiqkeqomeo"
        //};

        //System.Diagnostics.Debug.WriteLine(entry);
        //string x = "n=Lymphocytes&data=%5bx%2cy%5d+md%2fdL%7cx+%25&inf=Lymphocytosis&rem=Hello+bro!+%2b%3d-%60%7e%24%25%26+%f0%9f%98%82%f0%9f%98%8a(%e2%9d%81%c2%b4%e2%97%a1%60%e2%9d%81)%c2%a9%c2%a3%e2%86%90%e2%86%92+%0d%0a+kskmsiqkeqomeo";
        //if (ReferenceValueEntry.TryParse(x, out ReferenceValueEntry? entry))
        //{
        //    System.Diagnostics.Debug.WriteLine("E");
        //}

        LoadAllBookListsAsync();

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        MainWindow = new()
        {
            ExtendsContentIntoTitleBar = true,
            SystemBackdrop = new MicaBackdrop()
        };
#else
        MainWindow = Microsoft.UI.Xaml.Window.Current;
#endif
        MainWindow.Title = "Symptum Editor";

#if DEBUG
        MainWindow.EnableHotReload();
#endif

        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (MainWindow.Content is not Frame rootFrame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = new();

            // Place the frame in the current Window
            MainWindow.Content = rootFrame;

            rootFrame.NavigationFailed += OnNavigationFailed;
        }

        if (rootFrame.Content == null)
        {
            // When the navigation stack isn't restored navigate to the first page,
            // configuring the new page by passing required information as a navigation
            // parameter
            rootFrame.Navigate(typeof(MainPage), args.Arguments);
        }

        // Ensure the current window is active
        MainWindow.Activate();

#if HAS_UNO
        //Windows.Graphics.Display.DisplayInformation.AutoRotationPreferences = Windows.Graphics.Display.DisplayOrientations.Landscape;
#endif
    }

    private async Task LoadAllBookListsAsync()
    {
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
        }
        catch { }
    }

    /// <summary>
    /// Invoked when Navigation to a certain page fails
    /// </summary>
    /// <param name="sender">The Frame which failed navigation</param>
    /// <param name="e">Details about the navigation failure</param>
    private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
    }
}
