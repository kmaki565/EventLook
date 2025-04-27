using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EventLook.Model;
using EventLook.View;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace EventLook.ViewModel;

public class MainViewModel : ObservableRecipient
{
    public MainViewModel(IDataService dataService)
    {
        InitializeCommands();

        isRunAsAdmin = ProcessHelper.IsElevated;
        DataService = dataService;
        Events = new ObservableCollection<EventItem>();

        logSourceMgr = new LogSourceMgr(Properties.Settings.Default.StartupLogNames);
        SelectedLogSource = LogSources.FirstOrDefault();

        rangeMgr = new RangeMgr();
        SelectedRange = rangeMgr.GetStartupRange();

        sourceFilter = new Model.SourceFilter();
        levelFilter = new LevelFilter();
        MsgFilter = new MessageFilter();
        IdFilter = new IdFilter();

        filters = new List<FilterBase> { sourceFilter, levelFilter, MsgFilter, IdFilter };

        TimeZones = new ObservableCollection<TimeZoneInfo>(TimeZoneInfo.GetSystemTimeZones());
        SelectedTimeZone = TimeZones.FirstOrDefault(x => x.Id == TimeZoneInfo.Local.Id);
        ShowsMillisec = Properties.Settings.Default.ShowsMillisec;
        ShowsRecordId = Properties.Settings.Default.ShowsRecordId;

        progress = new Progress<ProgressInfo>(ProgressCallback); // Needs to instantiate in UI thread
        progressAutoRefresh = new Progress<ProgressInfo>(AutoRefreshCallback);
        
        stopwatch = new Stopwatch();
        newLoadedUpdateTimer = new DispatcherTimer  // Setup a periodic timer which we'll run later.
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        newLoadedUpdateTimer.Tick += NewLoadedUpdateTimer_Tick;

        Messenger.Register<MainViewModel, ViewCollectionViewSourceMessageToken>(this, (r, m) => r.Handle_ViewCollectionViewSourceMessageToken(m));
        Messenger.Register<MainViewModel, FileToBeProcessedMessageToken>(this, (r, m) => r.Handle_FileToBeProcessedMessageToken(m));
        Messenger.Register<MainViewModel, DetailWindowServiceMessageToken>(this, (r, m) => r.Handle_DetailWindowServiceMessageToken(m));
        Messenger.Register<MainViewModel, LogPickerWindowServiceMessageToken>(this, (r, m) => r.Handle_LogPickerWindowServiceMessageToken(m));
        Messenger.Register<MainViewModel, SettingsWindowServiceMessageToken>(this, (r, m) => r.Handle_SettingsWindowServiceMessageToken(m));
    }

    private readonly LogSourceMgr logSourceMgr;
    private readonly RangeMgr rangeMgr;
    private readonly Model.SourceFilter sourceFilter;
    private readonly LevelFilter levelFilter;
    // I had to bind the property of MessageFilter directly,
    // since NotifyPropertyChanged from inside MessageFilter didn't work.
    public MessageFilter MsgFilter { get; }
    public IdFilter IdFilter { get; }
    private readonly List<FilterBase> filters;
    private readonly Progress<ProgressInfo> progress;
    private readonly Progress<ProgressInfo> progressAutoRefresh;
    private readonly Stopwatch stopwatch;
    private readonly DispatcherTimer newLoadedUpdateTimer;
    private bool readyToRefresh = false;
    private bool isLastReadSuccess = false;
    private Task ongoingTask;
    private readonly TimeSpan newLoadedTimeThreshold = TimeSpan.FromSeconds(5);

    // Services to be injected.
    private readonly IDataService DataService;
    private IShowWindowService<DetailViewModel> DetailWindowService;
    private IShowWindowService<LogPickerViewModel> LogPickerWindowService;
    private IShowWindowService<SettingsViewModel> SettingsWindowService;

    private ObservableCollection<EventItem> _events;
    public ObservableCollection<EventItem> Events { get => _events; set => SetProperty(ref _events, value); }
    public EventItem SelectedEventItem { get; set; }
    public ObservableCollection<LogSource> LogSources { get => logSourceMgr.LogSources; }
    private LogSource selectedLogSource;
    public LogSource SelectedLogSource
    {
        get => selectedLogSource;
        set
        {
            if (value == null || value == selectedLogSource)
                return;

            SetProperty(ref selectedLogSource, value);

            if (readyToRefresh)
            {
                DataService.UnsubscribeEvents();
                Refresh(reset: true);
            }
        }
    }
    public ObservableCollection<Model.Range> Ranges { get => rangeMgr.Ranges; }
    private Model.Range selectedRange;
    public Model.Range SelectedRange
    {
        get => selectedRange;
        set
        {
            if (value == null || value == selectedRange)
                return;

            SetProperty(ref selectedRange, value);

            if (selectedRange.IsCustom)
                TurnOffAutoRefresh();   // We do not support auto refresh for custom range.
            else if (readyToRefresh)
                Refresh(reset: false);
        }
    }
    public ReadOnlyObservableCollection<SourceFilterItem> SourceFilters { get => sourceFilter.SourceFilters; }
    public ReadOnlyObservableCollection<LevelFilterItem> LevelFilters { get => levelFilter.LevelFilters; }
    
    private bool isUpdating = false;
    public bool IsUpdating { get => isUpdating; set => SetProperty(ref isUpdating, value); }

    private bool isAutoRefreshing = false;
    public bool IsAutoRefreshing 
    { 
        get => isAutoRefreshing; 
        set 
        {
            if (value == isAutoRefreshing) 
                return;

            SetProperty(ref isAutoRefreshing, value); 
            RefreshCommand?.NotifyCanExecuteChanged(); 
        } 
    }
    
    private int loadedEventCount = 0;
    public int LoadedEventCount { get => loadedEventCount; set => SetProperty(ref loadedEventCount, value); }

    private int totalEventCount = 0;
    public int TotalEventCount { get => totalEventCount; set => SetProperty(ref totalEventCount, value); }

    private bool isAppend = false;
    public bool IsAppend { get => isAppend; set => SetProperty(ref isAppend, value); }

    private int appendCount = 0;
    public int AppendCount { get => appendCount; set => SetProperty(ref appendCount, value); }

    private int visibleEventCount = 0;
    public int VisibleEventCount { get => visibleEventCount; set => SetProperty(ref visibleEventCount, value); }

    private bool areEventsFiltered = false;
    public bool AreEventsFiltered { get => areEventsFiltered; set => SetProperty(ref areEventsFiltered, value); }

    private string errorMessage = "";
    public string ErrorMessage { get => errorMessage; set => SetProperty(ref errorMessage, value); }

    private TimeSpan lastElapsedTime;
    public TimeSpan LastElapsedTime { get => lastElapsedTime; set => SetProperty(ref lastElapsedTime, value); }

    private DateTime fromDateTime;
    public DateTime FromDateTime { get => fromDateTime; set => SetProperty(ref fromDateTime, value); }
    
    private DateTime toDateTime;
    public DateTime ToDateTime { get => toDateTime; set => SetProperty(ref toDateTime, value); }

    public ObservableCollection<TimeZoneInfo> TimeZones { get; private set; }
    
    private TimeZoneInfo selectedTimeZone;
    public TimeZoneInfo SelectedTimeZone { get => selectedTimeZone; set => SetProperty(ref selectedTimeZone, value); }
    
    public bool ShowsMillisec { get; set; }

    private bool showsRecordId;
    public bool ShowsRecordId { get => showsRecordId; set => SetProperty(ref showsRecordId, value); }

    private readonly bool isRunAsAdmin;
    public bool IsRunAsAdmin { get => isRunAsAdmin; }

    private bool isAutoRefreshEnabled;
    public bool IsAutoRefreshEnabled 
    { 
        get => isAutoRefreshEnabled;
        set
        {
            if (value == isAutoRefreshEnabled)
                return;

            SetProperty(ref isAutoRefreshEnabled, value);
            if (value)
            {
                // We do not support auto refresh for custom range.
                if (!SelectedRange.IsCustom && SelectedLogSource?.PathType == PathType.LogName)
                    Refresh(reset: false, append: true);    // Fast refresh will kick auto refresh
            }
            else
            {
                TurnOffAutoRefresh();
            }
        }
    }

    public void OnLoaded()
    {
        filters.ForEach(f => {
            f.SetCvs(CVS);
            f.FilterUpdated += OnFilterUpdated;
        });

        Refresh(reset: true);
        readyToRefresh = true;
    }

    public event Action Refreshing;
    public event Action Refreshed;
    private void RefreshForCommand()
    {
        Refresh(reset: false, append: true);
    }
    /// <summary>
    /// Refreshes events to show.
    /// If reset is true, all filters will be cleared.
    /// </summary>
    /// <param name="reset"></param>
    private async void Refresh(bool reset, bool append = false)
    {
        Refreshing?.Invoke();

        IsAppend = append && !SelectedRange.IsCustom && SelectedLogSource?.PathType == PathType.LogName && isLastReadSuccess;
        if (IsAppend)
            UpdateIsNewLoaded(all: true);
        isLastReadSuccess = false;
        IsAutoRefreshing = false;

        UpdateDateTimeInUi();

        if (reset)
            filters.ForEach(f => f.Reset());
        
        await Task.Run(LoadEvents);

        // If the log source selection is changed before completing loading events, we don't want to enumerate
        // the source filter items with the previous log source.
        if (!IsUpdating)
            filters.ForEach(f => f.Refresh(Events, reset));

        Refreshed?.Invoke();
    }
    private void Cancel()
    {
        if (IsUpdating)
        {
            DataService.Cancel();
            ongoingTask.Wait(); // Assumes the task will be cancelled quickly
        }
    }
    private void Exit()
    {
        Application.Current.MainWindow.Close();
    }
    private void ClearFilters()
    {
        filters.ForEach(f => f.Clear());
    }
    private void ApplySourceFilter()
    {
        sourceFilter.Apply();
    }
    private void ApplyLevelFilter()
    {
        levelFilter.Apply();
    }
    private void OpenDetails()
    {
        if (SelectedEventItem == null)
            return;

        var detailVm = new DetailViewModel(CVS.View);
        DetailWindowService.Show(detailVm);
    }
    private void FilterToSelectedSource()
    {
        if (SelectedEventItem != null)
        {
            if (sourceFilter.SetSingleFilter(SelectedEventItem.Record.ProviderName))
                sourceFilter.Apply();
        }
    }
    private void ExcludeSelectedSource()
    {
        if (SelectedEventItem != null)
        {
            if (sourceFilter.UncheckFilter(SelectedEventItem.Record.ProviderName))
                sourceFilter.Apply();
        }
    }
    private void FilterToSelectedLevel()
    {
        if (SelectedEventItem != null)
        {
            if (levelFilter.SetSingleFilter(SelectedEventItem.Record.Level))
                levelFilter.Apply();
        }
    }
    private void ExcludeSelectedLevel()
    {
        if (SelectedEventItem != null)
        {
            if (levelFilter.UncheckFilter(SelectedEventItem.Record.Level))
                levelFilter.Apply();
        }
    }
    private void FilterToSelectedId(bool isExclude)
    {
        if (SelectedEventItem != null)
        {
            IdFilter.IdFilterNum = isExclude ? SelectedEventItem.Record.Id * -1 : SelectedEventItem.Record.Id;
            IdFilter.Apply();
        }
    }
    private void OnFilterUpdated(object sender, EventArgs e)
    {
        VisibleEventCount = CVS.View.Cast<object>().Count();
        AreEventsFiltered = VisibleEventCount < Events.Count;
    }

    #region commands
    public IRelayCommand RefreshCommand { get; private set; }
    public ICommand CancelCommand { get; private set; }
    public ICommand ExitCommand { get; private set; }
    public ICommand ApplySourceFilterCommand { get; private set; }
    public ICommand ApplyLevelFilterCommand { get; private set; }
    public ICommand ResetFiltersCommand { get; private set; }
    public ICommand OpenDetailsCommand { get; private set; }
    public ICommand FilterToSelectedSourceCommand { get; private set; }
    public ICommand ExcludeSelectedSourceCommand { get; private set; }
    public ICommand FilterToSelectedLevelCommand { get; private set; }
    public ICommand ExcludeSelectedLevelCommand { get; private set; }
    public ICommand FilterToSelectedIdCommand { get; private set; }
    public ICommand ExcludeSelectedIdCommand { get; private set; }
    public ICommand OpenFileCommand { get; private set; }
    public ICommand OpenLogPickerCommand { get; private set; }
    public ICommand OpenSettingsCommand { get; private set; }
    public ICommand LaunchEventViewerCommand { get; private set; }
    public ICommand CopyMessageTextCommand { get; private set; }
	public ICommand ExportToCsvCommand { get; private set; }
    public ICommand RunAsAdminCommand { get; private set; }

    private void InitializeCommands()
    {
        RefreshCommand = new RelayCommand(RefreshForCommand, () => !IsUpdating && !IsAutoRefreshing);
        CancelCommand = new RelayCommand(Cancel, () => IsUpdating);
        ExitCommand = new RelayCommand(Exit); 
        ResetFiltersCommand = new RelayCommand(ClearFilters);
        ApplySourceFilterCommand = new RelayCommand(ApplySourceFilter);
        ApplyLevelFilterCommand = new RelayCommand(ApplyLevelFilter);
        OpenDetailsCommand = new RelayCommand(OpenDetails);
        FilterToSelectedSourceCommand = new RelayCommand(FilterToSelectedSource);
        ExcludeSelectedSourceCommand = new RelayCommand(ExcludeSelectedSource);
        FilterToSelectedLevelCommand = new RelayCommand(FilterToSelectedLevel);
        ExcludeSelectedLevelCommand = new RelayCommand(ExcludeSelectedLevel);
        FilterToSelectedIdCommand = new RelayCommand(() => FilterToSelectedId(isExclude: false));
        ExcludeSelectedIdCommand = new RelayCommand(() => FilterToSelectedId(isExclude: true));
        OpenFileCommand = new RelayCommand(OpenFile);
        OpenLogPickerCommand = new RelayCommand(OpenLogPicker);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        LaunchEventViewerCommand = new RelayCommand(() => ProcessHelper.LaunchEventViewer(SelectedLogSource));
        CopyMessageTextCommand = new RelayCommand(CopyMessageText);
		ExportToCsvCommand = new RelayCommand(ExportToCsv);
        RunAsAdminCommand = new RelayCommand(RunAsAdmin);
    }
    #endregion

    private void UpdateDateTimeInUi()
    {
        if (SelectedRange.IsCustom)
            return;

        if (SelectedRange.DaysFromNow == 0) // All time
        {
            FromDateTime = new DateTime(1970, 1, 1, 0, 0, 0);
            ToDateTime = new DateTime(2030, 12, 31, 0, 0, 0);
        }
        else
        {
            // If the log source is a .evtx file, put the file's modified date instead of current time.
            ToDateTime = (SelectedLogSource.PathType == PathType.FilePath) 
                ? SelectedLogSource.FileWriteTime
                : DateTime.Now;
            if (!IsAppend)
                FromDateTime = ToDateTime - TimeSpan.FromDays(SelectedRange.DaysFromNow);
        }
    }
    private async Task LoadEvents()
    {
        Cancel();

        // When appending events, request events logged after the current newest event's time, from older to newer.
        await Update(DataService.ReadEvents(SelectedLogSource, 
            IsAppend ? Events.First().TimeOfEvent : FromDateTime,
            ToDateTime, 
            readFromNew: !IsAppend,
            progress));
    }
    private async Task Update(Task task)
    {
        try
        {
            ongoingTask = task;
            stopwatch.Restart();
            TotalEventCount = 0;
            if (!IsAppend)
                LoadedEventCount = 0;
            IsUpdating = true;
            await task;
        }
        finally
        {
            IsUpdating = false;
            stopwatch.Stop();
            LastElapsedTime = stopwatch.Elapsed;
        }
    }

    private void ProgressCallback(ProgressInfo progressInfo)
    {
        if (IsAppend)
        {
            if (progressInfo.IsFirst)
                AppendCount = 0;

            AppendCount += InsertEvents(progressInfo.LoadedEvents);
        }
        else
        {
            if (progressInfo.IsFirst)
            {
                Events.DisposeAll();
                Events.Clear();
            }

            foreach (var evt in progressInfo.LoadedEvents)
            {
                Events.Add(evt);
            }
        }

        LoadedEventCount = Events.Count;
        // Disregard unless the range is "All time".
        TotalEventCount = (SelectedRange.DaysFromNow == 0 && !SelectedRange.IsCustom) ? progressInfo.RecordCountInfo : 0;
        ErrorMessage = progressInfo.Message;

        if (progressInfo.IsComplete)
        {
            isLastReadSuccess = Events.Any() && progressInfo.Message == "";
            if (isLastReadSuccess && IsAutoRefreshEnabled && !SelectedRange.IsCustom && SelectedLogSource?.PathType == PathType.LogName)
            {
                TurnOnAutoRefresh();
            }
        }
    }

    private void AutoRefreshCallback(ProgressInfo progressInfo)
    {
        if (progressInfo.LoadedEvents.Any())
        {
            InsertEvents(progressInfo.LoadedEvents);    // Single event should be loaded at a time.
            LoadedEventCount = Events.Count;
            filters.ForEach(f => f.Refresh(Events, reset: false));
            // If the range is like "Last x days", just adjust appearance of the date time picker.
            if (!SelectedRange.IsCustom && SelectedRange.DaysFromNow != 0)
                ToDateTime = DateTime.Now;
        }
    }
    private void TurnOnAutoRefresh()
    {
        // Fast refresh should be done once before enabling auto refresh.
        // Otherwise we'll miss events that came during loading the entire logs.
        if (!IsAppend)
            Refresh(reset: false, append: true);
        DataService.SubscribeEvents(SelectedLogSource, progressAutoRefresh);
        TotalEventCount = 0;
        IsAutoRefreshing = true;
    }
    private void TurnOffAutoRefresh()
    {
        DataService.UnsubscribeEvents();
        IsAutoRefreshing = false;
        IsAppend = false;
    }
    private int InsertEvents(IEnumerable<EventItem> events)
    {
        int count = 0;
        foreach (var evt in events)
        {
            evt.IsNewLoaded = true;
            evt.TimeLoaded = DateTime.Now;
            // When appending, we read events from the oldest to the newest, so always insert to the top.
            Events.Insert(0, evt);
            count++;
        }
        if (count > 0 && !newLoadedUpdateTimer.IsEnabled)
            newLoadedUpdateTimer.Start();
        return count;
    }

    private void NewLoadedUpdateTimer_Tick(object sender, EventArgs e)
    {
        // Update the flag and stop the timer if there are no more events with the flag on.
        if (UpdateIsNewLoaded() == false)
            newLoadedUpdateTimer.Stop();
    }
    /// <summary>
    /// Turns off the newly-loaded flag of the events that were loaded before the threshold time
    /// and returns whether there are still events with the flag on.
    /// This must be called from the UI thread.
    /// </summary>
    /// <param name="all">When true, all events' flag will be turned off.</param>
    private bool UpdateIsNewLoaded(bool all = false)
    {
        Events.TakeWhile(e => e.IsNewLoaded)
            .Where(e => all || DateTime.Now - e.TimeLoaded >= newLoadedTimeThreshold)
            .ToList().ForEach(e => e.IsNewLoaded = false);

        return Events.Any(e => e.IsNewLoaded);
    }

    /// <summary>
    /// Gets or sets the CollectionViewSource which is the proxy for the
    /// collection of Events and the datagrid in which each Event is displayed.
    /// </summary>
    private CollectionViewSource CVS { get; set; }
    /// <summary>
    /// This method handles a message received from the View which enables a reference to the
    /// instantiated CollectionViewSource to be used in the ViewModel.
    /// </summary>
    private void Handle_ViewCollectionViewSourceMessageToken(ViewCollectionViewSourceMessageToken token)
    {
        CVS = token.CVS;
    }

    /// <summary>
    /// This method handles a message received from the View which passes a file name that was Drag & Dropped.
    /// </summary>
    private void Handle_FileToBeProcessedMessageToken(FileToBeProcessedMessageToken token)
    {
        SelectedLogSource = logSourceMgr.AddLogSource(token.FilePath, PathType.FilePath);
    }
    private void Handle_DetailWindowServiceMessageToken(DetailWindowServiceMessageToken token)
    {
        DetailWindowService = token.DetailWindowService;
    }
    private void Handle_LogPickerWindowServiceMessageToken(LogPickerWindowServiceMessageToken token)
    {
        LogPickerWindowService = token.LogPickerWindowService;
    }
    private void Handle_SettingsWindowServiceMessageToken(SettingsWindowServiceMessageToken token)
    {
        SettingsWindowService = token.SettingsWindowService;
    }

    private void OpenFile()
    {
        OpenFileDialog openFileDialog = new()
        {
            Filter = "Event Log files (*.evtx)|*.evtx",
            Title = "Open .evtx file"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            SelectedLogSource = logSourceMgr.AddLogSource(openFileDialog.FileName, PathType.FilePath);
        }
    }
    /// <summary>
    /// Opens a modal window to choose an Event Log channel to be added to the log source.
    /// </summary>
    private void OpenLogPicker()
    {
        var openLogVm = new LogPickerViewModel();
        bool? ret = LogPickerWindowService.ShowDialog(openLogVm);
        if (ret == true && !string.IsNullOrEmpty(openLogVm.SelectedChannel?.Path))
        {
            // If the chosen source is already in the list, just select it.
            SelectedLogSource = LogSources.FirstOrDefault(x => x.Path == openLogVm.SelectedChannel.Path) ??
                logSourceMgr.AddLogSource(openLogVm.SelectedChannel.Path, PathType.LogName, addToBottom: true);

            Properties.Settings.Default.StartupLogNames = LogSources.Where(x => x.PathType == PathType.LogName).Select(x => x.Path).ToList();
            Properties.Settings.Default.Save();
        }
    }
    /// <summary>
    /// Opens a modal window for Settings ("Options").
    /// </summary>
    private void OpenSettings()
    {
        var originalSettings = new SettingsData
        {
            LogSources = LogSources.ToList(),
            Ranges = Ranges.ToList(),
            SelectedRange = rangeMgr.GetStartupRange(),
            ShowsMillisec = ShowsMillisec,
            ShowsRecordId = ShowsRecordId
        };
        var openSettingsVm = new SettingsViewModel(LogPickerWindowService, originalSettings);
        string previousLogPath = SelectedLogSource?.Path;

        bool? ret = SettingsWindowService.ShowDialog(openSettingsVm);
        
        if (ret != true)    // Cancel
            return;

        // Save new settings.
        SettingsData newSettings = openSettingsVm.GetSettingsInfo();
        Properties.Settings.Default.StartupLogNames = newSettings.LogSources.Select(x => x.Path).ToList();
        Properties.Settings.Default.StartupRangeDays = newSettings.SelectedRange.DaysFromNow;
        Properties.Settings.Default.ShowsMillisec = newSettings.ShowsMillisec;
        Properties.Settings.Default.ShowsRecordId = newSettings.ShowsRecordId;
        Properties.Settings.Default.Save();

        // Reflect new settings to the UI.
        ShowsMillisec = newSettings.ShowsMillisec;
        ShowsRecordId = newSettings.ShowsRecordId;

        // Replace non-file logs with the new ones.
        readyToRefresh = false; // Avoid Refresh being called multiple times.
        for (int i = 0; i < LogSources.Count; i++)
        {
            if (LogSources[i].PathType == PathType.LogName)
                LogSources.RemoveAt(i--);
        }
        foreach (var logSource in newSettings.LogSources)
        {
            logSourceMgr.AddLogSource(logSource.Path, PathType.LogName, addToBottom: true);
        }

        if (LogSources.Any(x => x.Path == previousLogPath))
            SelectedLogSource = LogSources.First(x => x.Path == previousLogPath);
        else
            SelectedLogSource = LogSources.FirstOrDefault();

        SelectedRange = Ranges.FirstOrDefault(r => r.Text == newSettings.SelectedRange.Text);
        
        readyToRefresh = true;
        Refresh(reset: true);
    }
    /// <summary>
    /// Copies Message text of the selected log to clipboard.
    /// </summary>
    private void CopyMessageText()
    {
        if (SelectedEventItem == null)
            return;

        try
        {
            Clipboard.SetText(SelectedEventItem.Message);
        }
        catch (Exception) { }    // Ignore OpenClipboard exception
    }

    /// <summary>
    /// Exports the current events to a CSV file.
    /// </summary>
	private void ExportToCsv()
    {
        try
        {
            var saveFileDialog = new SaveFileDialog
            {
				Title = "Export to CSV",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = $"ExportedEvents_{DateTime.Now.ToString("yyyyMMdd-HHmmss")}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var sb = new StringBuilder();
				sb.AppendLine("RecordId,Time,Provider,Level,EventId,Message");

                // Ensure the CollectionViewSource is initialized and has a view
                if (CVS?.View != null)
                {
                    foreach (EventItem eventItem in CVS.View)
                    {
                        var eventRecord = eventItem.Record;

                        var line = string.Format("{0},{1},{2},{3},{4},{5}",
							TextHelper.EscapeCsvValue(eventRecord.RecordId.ToString()),
							TextHelper.EscapeCsvValue(eventRecord.TimeCreated.ToString()),
							TextHelper.EscapeCsvValue(eventRecord.ProviderName),
							TextHelper.EscapeCsvValue(eventRecord.Level.ToString()),
							TextHelper.EscapeCsvValue(eventRecord.Id.ToString()),
							TextHelper.EscapeCsvValue(eventItem.MessageOneLine));
                        sb.AppendLine(line);
                    }
                }

                var filePath = saveFileDialog.FileName;
				File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

                var result = MessageBox.Show($"Events exported. Click OK to open file location.", "Export Complete", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                if (result == MessageBoxResult.OK)
                {
                    Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred while exporting: {ex.Message}", "Export to CSV", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Restarts the application with administrator privileges.
    /// </summary>
    private void RunAsAdmin()
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule.FileName;

            var startInfo = new ProcessStartInfo(exePath)
            {
                UseShellExecute = true,
                Verb = "runas"
            };

            Process.Start(startInfo);

			Application.Current.MainWindow.Close();
        }
		catch
        {
            MessageBox.Show("Failed to restart as administrator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}