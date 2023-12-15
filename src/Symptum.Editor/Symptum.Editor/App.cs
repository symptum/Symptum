using Symptum.Core.Subjects.Books;

namespace Symptum.Editor;

public class App : Application
{
    public Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
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
