#if WINDOWS
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
#endif

namespace Symptum.Helpers;

public static class NotificationHelper
{
    public static void Register()
    {
#if WINDOWS
        try
        {
            AppNotificationManager.Default.Register();
        }
        catch
        {
        }
#endif
    }

    public static void Show(string title, string message)
    {
#if WINDOWS
        try
        {
            var notification = new AppNotificationBuilder()
                .AddText(title)
                .AddText(message)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
        catch
        {
        }
#else
        System.Diagnostics.Debug.WriteLine($"{title}: {message}");
#endif
    }
}
