using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Symptum.Core.Subjects;

namespace Symptum.Core.Management.Deployment;

public class PackageManager
{
    private static Dictionary<string, string> idsCache = [];
    //{
    //    { "Subjects.Z", "//temp//Subjects.Z" },
    //    { "Subjects.A", "" },
    //    { "Subjects.B", "" }
    //};

    public static void Initialize()
    {
        // Read from cache, resolve and load local packages
    }

    public static async Task<IPackageResource?> LoadPackageAsync(string packageId)
    {
        //if (packageId == "Subjects.Z")
        //{
        //    return await Task.Run(() => new Subject() { Title = "Z", Id = "Subjects.Z", DependencyIds = ["Subjects.A"] });
        //}
        //else if (packageId == "Subjects.A")
        //{
        //    return await Task.Run(() => new Subject() { Title = "A", Id = "Subjects.A", DependencyIds = ["Subjects.B"] });
        //}
        //else if (packageId == "Subjects.B")
        //{
        //    return await Task.Run(() => new Subject() { Title = "B", Id = "Subjects.B", DependencyIds = ["Subjects.Test"] });
        //}
        //else if (packageId == "Subjects.P")
        //{
        //    return await Task.Run(() => new Subject() { Title = "P", Id = "Subjects.P", DependencyIds = ["Subjects.B"] });
        //}
        //else
        //{
        //    return await Task.Run(() => new Subject() { Title = "Dummy"});
        //}

        // Available locally. Load from file path;
        if (idsCache.TryGetValue(packageId, out string? path))
        {
            return null;
        }
        else
        {
            return null;
            // Not available locally, request from server
        }
    }
}
