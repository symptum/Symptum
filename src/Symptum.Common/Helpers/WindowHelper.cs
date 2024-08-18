namespace Symptum.Common.Helpers;

public class WindowHelper
{
    private static nint windowHandle = nint.Zero;

    public static nint WindowHandle { get => windowHandle; }

    private static Window? mainWindow;

    public static Window? MainWindow { get => mainWindow; }

    public static void Initialize(Window window)
    {
        mainWindow = window;

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
#endif
    }
}
