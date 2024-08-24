using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace EventLook.Model;

/// <summary>
/// Represents a log source, which can be a .evtx file or a local Event Log Channel.
/// </summary>
public class LogSource
{
    public LogSource(string path, PathType type = PathType.LogName)
    {
        Path = path;
        PathType = type;

        FileWriteTime = DateTime.Now;
        if (type == PathType.FilePath)
        {
            DisplayName = System.IO.Path.GetFileName(Path);
            string computerName = DataService.GetComputerNameFromEvtx(path);
            if (!string.IsNullOrEmpty(computerName))
                DisplayName += $" ({computerName})";

            try
            {
                FileWriteTime = File.GetLastWriteTime(path);
            }
            catch (Exception) { }
        }
        else if (type == PathType.LogName)
        {
            DisplayName = $"{path} (local)";
        }
    }

    public string Path { get; }
    public PathType PathType { get; }
    public string DisplayName { get; }
    /// <summary>
    /// If the source is a .evtx file, represents the file's last write time.
    /// </summary>
    public DateTime FileWriteTime { get; }

}
public class LogSourceMgr
{
    /// <summary>
    /// Initializes the log source list that will be shown in the combobox.
    /// </summary>
    /// <param name="logNames"></param>
    public LogSourceMgr(IEnumerable<string> logNames)
    {
        if (logNames == null || !logNames.Any())
        {
            logNames = defaultLogSources;
            Properties.Settings.Default.StartupLogNames = logNames.ToList();
            Properties.Settings.Default.Save();
        }

        LogSources = new ObservableCollection<LogSource>(logNames.Select(x => new LogSource(x)));
    }
    public ObservableCollection<LogSource> LogSources { get; set; }
    public static readonly IEnumerable<string> defaultLogSources = new[] { "System", "Application" };

    /// <summary>
    /// Adds an evtx file or a local Event Log channel to the log source list. 
    /// By default, it will be added to the beginning of the list.
    /// </summary>
    /// <returns>the added log source</returns>
    public LogSource AddLogSource(string path, PathType type, bool addToBottom = false)
    {
        if (!DataService.IsValidEventLog(path, type))
            return null;

        var logSource = new LogSource(path, type);

        if (addToBottom)
            LogSources.Add(logSource);
        else
            LogSources.Insert(0, logSource);

        return logSource;
    }
}
