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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

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

        progress = new Progress<ProgressInfo>(ProgressCallback); // Needs to instantiate in UI thread
        progressAutoRefresh = new Progress<ProgressInfo>(AutoRefreshCallback);
        
        stopwatch = new Stopwatch();

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
    private bool readyToRefresh = false;
    private bool isLastReadSuccess = false;
    private Task ongoingTask;

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
            if (value == null)
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
            SetProperty(ref selectedRange, value);

            if (readyToRefresh && !selectedRange.IsCustom)
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
            SetProperty(ref isAutoRefreshing, value); 
            RefreshCommand?.NotifyCanExecuteChanged(); 
        } 
    }
    
    private int loadedEventCount = 0;
    public int LoadedEventCount { get => loadedEventCount; set => SetProperty(ref loadedEventCount, value); }

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

    private readonly bool isRunAsAdmin;
    public bool IsRunAsAdmin { get => isRunAsAdmin; }

    private bool isAutoRefreshEnabled;
    public bool IsAutoRefreshEnabled 
    { 
        get => isAutoRefreshEnabled;
        set
        {
            SetProperty(ref isAutoRefreshEnabled, value);
            if (value)
            {
                if (selectedLogSource?.PathType == PathType.LogName)
                    Refresh(reset: false, append: true);    // Fast refresh will kick auto refresh
            }
            else
            {
                DataService.UnsubscribeEvents();
                IsAutoRefreshing = false;
                IsAppend = false;
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

        IsAppend = append && !SelectedRange.IsCustom && selectedLogSource?.PathType == PathType.LogName && isLastReadSuccess;
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
    private async void ApplySourceFilter()
    {
        // Delay is needed to ensure filter update in UI is propagated to the source.
        await Task.Delay(10);
        sourceFilter.Apply();
    }
    private async void ApplyLevelFilter()
    {
        await Task.Delay(10);
        levelFilter.Apply();
    }
    private void OpenDetails()
    {
        var detailVm = new DetailViewModel(SelectedEventItem);
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
    private void FilterToSelectedId()
    {
        if (SelectedEventItem != null)
        {
            IdFilter.IdFilterNum = SelectedEventItem.Record.Id;
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
    public ICommand OpenFileCommand { get; private set; }
    public ICommand OpenLogPickerCommand { get; private set; }
    public ICommand OpenSettingsCommand { get; private set; }
    public ICommand LaunchEventViewerCommand { get; private set; }
    public ICommand CopyMessageTextCommand { get; private set; }

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
        FilterToSelectedIdCommand = new RelayCommand(FilterToSelectedId);
        OpenFileCommand = new RelayCommand(OpenFile);
        OpenLogPickerCommand = new RelayCommand(OpenLogPicker);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        LaunchEventViewerCommand = new RelayCommand(() => ProcessHelper.LaunchEventViewer(SelectedLogSource));
        CopyMessageTextCommand = new RelayCommand(CopyMessageText);
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

        // These seem necessary to ensure DateTimePicker be updated
        OnPropertyChanged(nameof(FromDateTime));
        OnPropertyChanged(nameof(ToDateTime));
    }
    private async Task LoadEvents()
    {
        Cancel();

        await Update(DataService.ReadEvents(selectedLogSource, 
            IsAppend ? Events.First().TimeOfEvent : FromDateTime,
            ToDateTime,
            progress));
    }

    private void ProgressCallback(ProgressInfo progressInfo)
    {
        if (IsAppend)
        {
            if (progressInfo.IsFirst)
                AppendCount = 0;

            foreach (var evt in progressInfo.LoadedEvents)
            {
                Events.Insert(AppendCount, evt);    // We read logs from the newest to the oldest.
                AppendCount++;
            }
        }
        else
        {
            if (progressInfo.IsFirst)
                Events.Clear();

            foreach (var evt in progressInfo.LoadedEvents)
            {
                Events.Add(evt);
            }
        }

        LoadedEventCount = Events.Count;
        ErrorMessage = progressInfo.Message;

        if (progressInfo.IsComplete)
        {
            isLastReadSuccess = Events.Any() && progressInfo.Message == "";
            if (isLastReadSuccess && IsAutoRefreshEnabled && selectedLogSource?.PathType == PathType.LogName)
            {
                // Fast refresh should be done once before enabling auto refresh.
                // Otherwise we'll miss events that came during loading the entire logs.
                if (!IsAppend)
                    Refresh(reset: false, append: true);
                DataService.SubscribeEvents(SelectedLogSource, progressAutoRefresh);
                IsAutoRefreshing = true;
            }
        }
    }

    private void AutoRefreshCallback(ProgressInfo progressInfo)
    {
        if (progressInfo.LoadedEvents.Any())
        {
            int count = 0;
            foreach (var evt in progressInfo.LoadedEvents)  // Single event should be loaded at a time, but just in case.
            {
                Events.Insert(count, evt);
                count++;
            }
            LoadedEventCount = Events.Count;
            filters.ForEach(f => f.Refresh(Events, reset: false));
        }
    }

    private async Task Update(Task task)
    {
        try
        {
            ongoingTask = task;
            stopwatch.Restart();
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
            SelectedLogSource = logSourceMgr.AddLogSource(openLogVm.SelectedChannel.Path, PathType.LogName);
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
            ShowsMillisec = ShowsMillisec
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
        Properties.Settings.Default.Save();

        // Reflect new settings to the UI.
        ShowsMillisec = newSettings.ShowsMillisec;

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

        readyToRefresh = true;
        SelectedRange = Ranges.FirstOrDefault(r => r.Text == newSettings.SelectedRange.Text);   // Fire Refresh
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
}