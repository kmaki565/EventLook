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
    /// Gets build date of the main DLL of this .NET Core application.
    /// </summary>
    /// <returns></returns>
    public static DateTime GetBuildDate()
    {
        var buildDate = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(e => e.Key == "BuildDate")?.Value;
        return DateTime.TryParse(buildDate, out var buildDateTime) ? buildDateTime : DateTime.MaxValue;
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
