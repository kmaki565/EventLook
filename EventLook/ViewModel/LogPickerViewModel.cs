using CommunityToolkit.Mvvm.ComponentModel;
using EventLook.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace EventLook.ViewModel;

public class LogPickerViewModel : ObservableObject
{
    private readonly EventLogSession elSession;
    public LogPickerViewModel()
    {
        elSession = new EventLogSession();
        LogChannels= new ObservableCollection<LogChannel>();
        LogsView = CollectionViewSource.GetDefaultView(LogChannels);
        LogsView.Filter = OnFilterTriggered;
        isRunAsAdmin = ProcessHelper.IsElevated;
    }

    public ICollectionView LogsView { get; private set; }
    public ObservableCollection<LogChannel> LogChannels { get; set; }
    public LogChannel SelectedChannel { get; set; }

    private bool hidesEmptyLogs = false;
    public bool HidesEmptyLogs
    {
        get => hidesEmptyLogs;
        set
        {
            SetProperty(ref hidesEmptyLogs, value);
            LogsView.Refresh();
        }
    }

    private string filterText = "";
    public string FilterText
    { 
        get => filterText;
        set 
        {
            SetProperty(ref filterText, value);
            LogsView.Refresh();
        }
    }
    private readonly bool isRunAsAdmin;
    public bool IsRunAsAdmin { get => isRunAsAdmin; }

    public async void OnLoaded()
    {
        await InitializeChannels();
    }

    private bool OnFilterTriggered(object item)
    {
        if (item is LogChannel channel)
        {
            if (!HidesEmptyLogs || channel.RecordCount.HasValue && channel.RecordCount.Value > 0)
                return string.IsNullOrEmpty(FilterText) || channel.Path.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private async Task InitializeChannels()
    {
        foreach (string channelName in await Task.Run(() => elSession.GetLogNames()))
        {
            EventLogConfiguration config;
            try
            {
                config = new EventLogConfiguration(channelName);
            }
            catch (Exception) { continue; }

            if (!config.IsEnabled)
                continue;

            long? recordCount = null;
            try
            {
                EventLogInformation info = await Task.Run(() => elSession.GetLogInformation(config.LogName, PathType.LogName));
                recordCount = info.RecordCount;
            }
            catch (Exception) { }

            LogChannels.Add(new LogChannel { Path = channelName, RecordCount = recordCount });
        }
    }
}
