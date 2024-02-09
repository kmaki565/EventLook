using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.Model;

public static class ProcessHelper
{
    // https://stackoverflow.com/a/31856353/5461938
    public static bool IsElevated
    {
        get
        {
            return WindowsIdentity.GetCurrent().Owner
              .IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
        }
    }

    public static void LaunchEventViewer(LogSource logSource)
    {
        string arg = "";
        if (logSource?.PathType == PathType.FilePath && !string.IsNullOrEmpty(logSource?.Path))
            arg = $"/l:\"{logSource.Path}\"";
        else if (logSource?.PathType == PathType.LogName && !string.IsNullOrEmpty(logSource?.Path))
            arg = $"/c:\"{logSource.Path}\"";

        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = "eventvwr.msc",
            Arguments = arg,
        });
    }
}
