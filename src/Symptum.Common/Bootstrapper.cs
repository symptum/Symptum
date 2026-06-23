using Symptum.Common.Helpers;

namespace Symptum.Common;

public class Bootstrapper
{
    public static async Task InitializeAsync()
    {
        await PackageHelper.InitializeAsync();
        StorageHelper.Initialize();
    }
}
