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

        logSourceMgr = new LogSourceMgr(Properties.Settings.Default.StartupLogSources);
        SelectedLogSource = LogSources.FirstOrDefault();

        rangeMgr = new RangeMgr();
        SelectedRange = Ranges.FirstOrDefault(r => r.DaysFromNow == 7);

        sourceFilter = new Model.SourceFilter();
        levelFilter = new LevelFilter();
        MsgFilter = new MessageFilter();
        IdFilter = new IdFilter();

        filters = new List<FilterBase> { sourceFilter, levelFilter, MsgFilter, IdFilter };

        TimeZones = new ObservableCollection<TimeZoneInfo>(TimeZoneInfo.GetSystemTimeZones());
        SelectedTimeZone = TimeZones.FirstOrDefault(x => x.Id == TimeZoneInfo.Local.Id);

        progress = new Progress<ProgressInfo>(ProgressCallback); // Needs to instantiate in UI thread
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
    // This may be controversial.
    public MessageFilter MsgFilter { get; }
    public IdFilter IdFilter { get; }
    private readonly List<FilterBase> filters;
    private readonly Progress<ProgressInfo> progress;
    private readonly Stopwatch stopwatch;
    private bool isWindowLoaded = false;
    private int loadedEventCount = 0;
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

            if (isWindowLoaded)
            {
                Refresh(carryOver: false);
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

            if (isWindowLoaded && !selectedRange.IsCustom)
                Refresh(carryOver: true);
        }
    }
    public ReadOnlyObservableCollection<SourceFilterItem> SourceFilters { get => sourceFilter.SourceFilters; }
    public ReadOnlyObservableCollection<LevelFilterItem> LevelFilters { get => levelFilter.LevelFilters; }
    private string statusText;
    public string StatusText { get => statusText; set => SetProperty(ref statusText, value); }
    private string filterInfoText;
    public string FilterInfoText { get => filterInfoText; set => SetProperty(ref filterInfoText, value); }
    private bool isUpdating = false;
    public bool IsUpdating { get => isUpdating; set => SetProperty(ref isUpdating, value); }
    private DateTime fromDateTime;
    public DateTime FromDateTime { get => fromDateTime; set => SetProperty(ref fromDateTime, value); }
    private DateTime toDateTime;
    public DateTime ToDateTime { get => toDateTime; set => SetProperty(ref toDateTime, value); }

    public ObservableCollection<TimeZoneInfo> TimeZones { get; private set; }
    private TimeZoneInfo selectedTimeZone;
    public TimeZoneInfo SelectedTimeZone { get => selectedTimeZone; set => SetProperty(ref selectedTimeZone, value); }

    private readonly bool isRunAsAdmin;
    public bool IsRunAsAdmin { get => isRunAsAdmin; }

    public void OnLoaded()
    {
        filters.ForEach(f => {
            f.SetCvs(CVS);
            f.FilterUpdated += OnFilterUpdated;
        });

        Refresh(carryOver: false);
        isWindowLoaded = true;
    }

    public event Action Refreshing;
    public event Action Refreshed;
    private void RefreshForCommand()
    {
        Refresh(carryOver: true);
    }
    private async void Refresh(bool carryOver)
    {
        Refreshing?.Invoke();

        UpdateDateTimes();

        await Task.Run(() => LoadEvents());

        //TODO: It's ideal if we refresh filters while loading events.
        filters.ForEach(f => f.Refresh(Events, carryOver));

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
    private void ResetFilters()
    {
        filters.ForEach(f => f.Reset());
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
        UpdateFilterInfoText();
    }

    #region commands
    public ICommand RefreshCommand { get; private set; }
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
        RefreshCommand = new RelayCommand(RefreshForCommand, () => !IsUpdating);
        CancelCommand = new RelayCommand(Cancel, () => IsUpdating);
        ExitCommand = new RelayCommand(Exit); 
        ResetFiltersCommand = new RelayCommand(ResetFilters);
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
        LaunchEventViewerCommand = new RelayCommand(LaunchEventViewer);
        CopyMessageTextCommand = new RelayCommand(CopyMessageText);
    }
    #endregion

    private void UpdateDateTimes()
    {
        if (SelectedRange.IsCustom)
            return;

        if (SelectedRange.DaysFromNow == 0)
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
            FromDateTime = ToDateTime - TimeSpan.FromDays(SelectedRange.DaysFromNow);
        }

        // These seem necessary to ensure DateTimePicker be updated
        OnPropertyChanged(nameof(FromDateTime));
        OnPropertyChanged(nameof(ToDateTime));
    }
    private async Task LoadEvents()
    {
        Cancel();

        await Update(DataService.ReadEvents(selectedLogSource, FromDateTime, ToDateTime, progress));
    }
    private void ProgressCallback(ProgressInfo progressInfo)
    {
        if (progressInfo.IsFirst)
            Events.Clear();

        //TODO: Improve performance with AddRange in ObservableCollection.
        foreach (var evt in progressInfo.LoadedEvents)
        {
            Events.Add(evt);
        }
        loadedEventCount = Events.Count;
        UpdateStatusText(progressInfo.Message);
    }
    private async Task Update(Task task)
    {
        try
        {
            ongoingTask = task;
            stopwatch.Restart();
            loadedEventCount = 0;
            IsUpdating = true;
            UpdateStatusText();
            UpdateFilterInfoText();
            await task;
        }
        finally
        {
            IsUpdating = false;
            stopwatch.Stop();
            UpdateStatusText();
        }
    }
    private void UpdateStatusText(string additionalNote = "")
    {
        StatusText = IsUpdating ?
            $"Loading {loadedEventCount} events... {additionalNote}" :
            $"{loadedEventCount} events loaded. ({stopwatch.Elapsed.TotalSeconds:F1} sec) {additionalNote}"; // 1 digit after decimal point
    }
    private void UpdateFilterInfoText()
    {
        if (IsUpdating)
            FilterInfoText = "";
        else
        {
            int visibleRowCounts = CVS.View.Cast<object>().Count();
            FilterInfoText = (visibleRowCounts == Events.Count) ? "" : $" {visibleRowCounts} events matched to the filter(s).";
        }
    }
    /// <summary>
    /// Gets or sets the CollectionViewSource which is the proxy for the
    /// collection of Things and the datagrid in which each thing is displayed.
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
    /// Opens a modal window for Settings.
    /// </summary>
    private void OpenSettings()
    {
        var openSettingsVm = new SettingsViewModel(LogPickerWindowService, LogSourceMgr.LogChannelNamesInLogSources(LogSources));
        string previousLogPath = SelectedLogSource?.Path;

        bool? ret = SettingsWindowService.ShowDialog(openSettingsVm);
        
        // Reflect the new settings to UI.
        if (ret == true && openSettingsVm.StartupLogSources.Any())
        {
            Properties.Settings.Default.StartupLogSources = openSettingsVm.StartupLogSources.ToList();
            Properties.Settings.Default.Save();

            // Replace non-file logs with the new ones.
            for (int i = 0; i < LogSources.Count; i++)
            {
                if (LogSources[i].PathType == PathType.LogName)
                    LogSources.RemoveAt(i--);
            }
            foreach (var logName in openSettingsVm.StartupLogSources)
            {
                logSourceMgr.AddLogSource(logName, PathType.LogName, addToBottom: true);
            }

            if (LogSources.Any(x => x.Path == previousLogPath))
                SelectedLogSource = LogSources.First(x => x.Path == previousLogPath);
            else 
                SelectedLogSource = LogSources.FirstOrDefault();
        }
    }
    private void LaunchEventViewer()
    {
        string arg = "";
        if (SelectedLogSource?.PathType == PathType.FilePath)
            arg = $"/l:\"{selectedLogSource.Path}\"";
        else if (selectedLogSource?.PathType == PathType.LogName)
            arg = $"/c:\"{selectedLogSource.Path}\"";

        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = "eventvwr.msc",
            Arguments = arg,
        });
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
        catch (Exception) { }    // Ignore OpenClipboard exception}
    }
}