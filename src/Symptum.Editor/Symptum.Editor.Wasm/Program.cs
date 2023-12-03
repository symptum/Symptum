using Symptum.Editor.Helpers;
using static Uno.Storage.Pickers.FileSystemAccessApiInformation;

namespace Symptum.Editor.Wasm;

public class Program
{
    private static App? _app;

    public static int Main(string[] args)
    {
        Uno.WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration = Uno.WasmPickerConfiguration.FileSystemAccessApiWithFallback;
        StorageHelper.SetPickerSupport(IsOpenPickerSupported, IsSavePickerSupported, IsFolderPickerSupported);
        Microsoft.UI.Xaml.Application.Start(_ => _app = new AppHead());
        return 0;
    }
}
