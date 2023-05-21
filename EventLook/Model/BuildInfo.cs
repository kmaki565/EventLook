using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace EventLook.Model;
internal static class BuildInfo
{
    /// <summary>
    /// Simplified package type to be consumed by About menu.
    /// </summary>
    public enum PackageType
    {
        NotPackaged,    // Standalone build such as artifact in GitHub.
        DevPackage,     // Private build of EventLookPackage.
        Store           // Formal release.
    }
    /// <summary>
    /// Gets build date (= last write time) of the main DLL of this .NET Core application.
    /// Assuming the process EXE and the main DLL share the same name (e.g., EventLook.exe vs EventLook.dll)
    /// and are located in the same folder.
    /// </summary>
    /// <returns></returns>
    public static DateTime GetBuildDate()
    {
        string dllPath = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, "dll");

        return File.Exists(dllPath) ? File.GetLastWriteTime(dllPath) : DateTime.MaxValue;
    }

    /// <summary>
    /// Determines the simplified package type of this application.
    /// If the environment is very old (like Win8 or very early version of Win10), treats as not packaged.
    /// </summary>
    /// <returns></returns>
    public static PackageType GetPackageType()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393))
        {
            return PackageType.NotPackaged;
        }
        try
        {
            Package package = Package.Current;
            return package.SignatureKind switch
            {
                PackageSignatureKind.None or PackageSignatureKind.Developer or PackageSignatureKind.Enterprise => PackageType.DevPackage,
                PackageSignatureKind.Store or PackageSignatureKind.System => PackageType.Store,
                _ => PackageType.NotPackaged,
            };
        }
        catch (Exception)
        {
            return PackageType.NotPackaged;
        }
    }
}
