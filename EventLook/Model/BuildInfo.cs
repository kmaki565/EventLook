using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.Model;
internal static class BuildInfo
{
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
}
