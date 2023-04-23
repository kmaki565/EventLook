using EventLook.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.ViewModel;

public class LogPickerViewModel
{
    private readonly EventLogSession elSession;
    public LogPickerViewModel()
    {
        elSession = new EventLogSession();
        LogChannels= new ObservableCollection<LogChannel>();
    }

    public ObservableCollection<LogChannel> LogChannels { get; set; }
    public LogChannel SelectedChannel { get; set; }

    public void OnLoaded()
    {
        InitializeChannels();
    }

    private void InitializeChannels()
    {
        foreach (var channelName in elSession.GetLogNames())
        {
            EventLogConfiguration config;
            try
            {
                config = new EventLogConfiguration(channelName);
            }
            catch (Exception) { continue; }

            if (config.IsEnabled)
            {
                bool hasEvent = false;
                try
                {
                    var info = elSession.GetLogInformation(config.LogName, PathType.LogName);
                    hasEvent = info.RecordCount.HasValue && info.RecordCount > 0;
                }
                catch (Exception) { continue; }

                if (hasEvent)
                    LogChannels.Add(new LogChannel { Path = channelName, Config = config, HasEvent = hasEvent });
            }
        }
    }
}
