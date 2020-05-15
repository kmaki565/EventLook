using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.Model
{
    public class LogSource
    {
        public string Name { get; set; }
    }
    public class LogSourceMgr
    {
        public LogSourceMgr()
        {
            LogSources = new ObservableCollection<LogSource>
            {
                new LogSource() { Name = "System" },
                new LogSource() { Name = "Application" },
                new LogSource() { Name = "Lenovo-Power-BaseModule/Operational" },
                new LogSource() { Name = "Lenovo-Power-SmartStandby/Operational" },
            };
        }
        public ObservableCollection<LogSource> LogSources { get; set; }
    }
}
