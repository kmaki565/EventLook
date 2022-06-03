using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace EventLook.Model
{
    public class LogSource
    {
        public LogSource(string path, PathType type = PathType.LogName)
        {
            Path = path;
            PathType = type;

            FileWriteTime = DateTime.Now;
            if (type == PathType.FilePath)
            {
                try
                {
                    FileWriteTime = File.GetLastWriteTime(path);
                }
                catch (Exception) { }
            }
        }

        public string Path { get; }
        public PathType PathType { get; }
        /// <summary>
        /// If the source is a .evtx file, represents the file's last write time.
        /// </summary>
        public DateTime FileWriteTime { get; }

    }
    public class LogSourceMgr
    {
        public LogSourceMgr()
        {
            LogSources = new ObservableCollection<LogSource>
            {
                new LogSource("System"),
                new LogSource("Application"),
                new LogSource("Lenovo-Power-BaseModule/Operational"),
                new LogSource("Lenovo-Power-SmartStandby/Operational"),
                new LogSource("Lenovo-Sif-Core/Operational"), // This channel requires admin privilege to read.
                new LogSource("Microsoft-Windows-WindowsUpdateClient/Operational"),
                new LogSource("Microsoft-Windows-Windows Defender/Operational")
            };
        }
        public ObservableCollection<LogSource> LogSources { get; set; }

        /// <summary>
        /// Adds an evtx file to the log source list. 
        /// By default, it will be added to the beginning of the list.
        /// </summary>
        /// <returns>the added log source</returns>
        public LogSource AddEvtx(string filePath, bool addToBottom = false)
        {
            LogSource logSource = null;
            if (Path.GetExtension(filePath).Equals(".evtx", StringComparison.OrdinalIgnoreCase))
            {
                logSource = new LogSource(filePath, PathType.FilePath);
                if (addToBottom)
                    LogSources.Add(logSource);
                else
                    LogSources.Insert(0, logSource);
            }
            return logSource;
        }
    }
}
