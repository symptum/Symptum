using Uno.UI.Hosting;

namespace Symptum.iOS;

public class EntryPoint
{
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseAppleUIKit()
            .Build();

        host.Run();
    }
}
