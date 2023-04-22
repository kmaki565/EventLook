using EventLook.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.ViewModel
{
    public class OpenLocalLogViewModel
    {
        public OpenLocalLogViewModel()
        {
            var session = new EventLogSession();
            LogChannels = new List<LogChannel>();
            foreach (var channelName in session.GetLogNames())
            {
                LogChannels.Add(new LogChannel { Path = channelName });
            }
        }
        public List<LogChannel> LogChannels { get; set; }
        public LogChannel SelectedChannel { get; set; }
    }
}
