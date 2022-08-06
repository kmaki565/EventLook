using EventLook.Model;
using EventLook.View;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace EventLook.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel(IDataService dataService)
        {
            InitializeCommands();

            MainWindowTitle = "EventLook" + (ProcessHelper.IsElevated ? " (Administrator)" : "");
            DataService = dataService;
            Events = new ObservableCollection<EventItem>();

            logSourceMgr = new LogSourceMgr();
            SelectedLogSource = LogSources.FirstOrDefault();

            rangeMgr = new RangeMgr();
            SelectedRange = Ranges.FirstOrDefault(r => r.DaysFromNow == 3);

            sourceFilter = new Model.SourceFilter();
            levelFilter = new LevelFilter();
            MsgFilter = new MessageFilter();
            IdFilter = new IdFilter();

            filters = new List<FilterBase> { sourceFilter, levelFilter, MsgFilter, IdFilter };

            progress = new Progress<ProgressInfo>(ProgressCallback); // Needs to instantiate in UI thread
            stopwatch = new Stopwatch();

            Messenger.Default.Register<ViewCollectionViewSourceMessageToken>(this, Handle_ViewCollectionViewSourceMessageToken);
            Messenger.Default.Register<FileToBeProcessedMessageToken>(this, Handle_FileToBeProcessedMessageToken);
            Messenger.Default.Register<DetailWindowMessageToken>(this, Handle_DetailWindowMessageToken);
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

        internal IDataService DataService { get; set; }
        private ShowWindowService<DetailWindow, DetailViewModel> showWindowService;

        private ObservableCollection<EventItem> _events;
        public ObservableCollection<EventItem> Events
        {
            get { return _events; }
            set
            {
                if (_events == value)
                    return;

                _events = value;
                RaisePropertyChanged();
            }
        }
        public EventItem SelectedEventItem { get; set; }

        public ObservableCollection<LogSource> LogSources
        {
            get { return logSourceMgr.LogSources; }
        }

        private LogSource selectedLogSource;
        public LogSource SelectedLogSource
        {
            get { return selectedLogSource; }
            set
            {
                if (value == selectedLogSource)
                    return;

                selectedLogSource = value;
                RaisePropertyChanged();

                if (isWindowLoaded)
                    Refresh();
            }
        }

        public ObservableCollection<Range> Ranges
        {
            get { return rangeMgr.Ranges; }
        }

        private Range selectedRange;
        public Range SelectedRange
        {
            get { return selectedRange; }
            set
            {
                if (value == selectedRange)
                    return;

                selectedRange = value;
                RaisePropertyChanged();

                if (isWindowLoaded && !selectedRange.IsCustom)
                    Refresh();
            }
        }

        public ReadOnlyObservableCollection<SourceFilterItem> SourceFilters
        {
            get { return sourceFilter.SourceFilters; }
        }
        public ReadOnlyObservableCollection<LevelFilterItem> LevelFilters
        {
            get { return levelFilter.LevelFilters; }
        }

        private string statusText;
        public string StatusText
        {
            get { return statusText; }
            private set
            {
                if (value == statusText)
                    return;

                statusText = value;
                RaisePropertyChanged();
            }
        }
        private string filterInfoText;
        public string FilterInfoText { get => filterInfoText; set => Set(ref filterInfoText, value); }

        private bool isUpdating = false;
        public bool IsUpdating
        {
            get { return isUpdating; }
            private set
            {
                if (value == isUpdating)
                    return;

                isUpdating = value;
                RaisePropertyChanged();
            }
        }

        private DateTime fromDateTime;
        public DateTime FromDateTime
        {
            get { return fromDateTime; }
            set
            {
                if (value == fromDateTime)
                    return;

                fromDateTime = value;
                RaisePropertyChanged();
            }
        }
        private DateTime toDateTime;
        public DateTime ToDateTime
        {
            get { return toDateTime; }
            set
            {
                if (value == toDateTime)
                    return;

                toDateTime = value;
                RaisePropertyChanged();
            }
        }
        public string MainWindowTitle { get; }

        public void OnLoaded()
        {
            filters.ForEach(f => {
                f.SetCvs(CVS);
                f.FilterUpdated += OnFilterUpdated;
            });

            Refresh();
            isWindowLoaded = true;
        }
        public async void Refresh()
        {
            UpdateDateTimes();

            await Task.Run(() => LoadEvents());

            filters.ForEach(f => f.Refresh(Events));
        }
        public void Cancel()
        {
            if (IsUpdating)
            {
                DataService.Cancel();
                ongoingTask.Wait(); // Assumes the task will be cancelled quickly
            }
        }
        public override void Cleanup()
        {
            Messenger.Default.Unregister<ViewCollectionViewSourceMessageToken>(this);
            Messenger.Default.Unregister<FileToBeProcessedMessageToken>(this);
            base.Cleanup();
        }
        public void ResetFilters()
        {
            filters.ForEach(f => f.Reset());
        }
        private async void ApplySourceFilter()
        {
            // TODO: Workaround as the command is called BEFORE the filter value is actually modified
            await Task.Delay(50);
            sourceFilter.Apply();
        }
        private async void ApplyLevelFilter()
        {
            // TODO: Workaround as the command is called BEFORE the filter value is actually modified
            await Task.Delay(50);
            levelFilter.Apply();
        }
        private void OpenDetails()
        {
            var detailVm = new DetailViewModel(SelectedEventItem);
            showWindowService.Show(detailVm);
        }
        private void ContextMenu(object menuKind)
        {
            if (menuKind is ContextMenuKind kind)
            {
                switch (kind)
                {
                    case ContextMenuKind.FilterToTheSource:
                        if (SelectedEventItem != null)
                        {
                            // Filter to the event item's event source
                            if (sourceFilter.SetSingleFilter(SelectedEventItem.Record.ProviderName))
                                sourceFilter.Apply();
                        }
                        break;
                    case ContextMenuKind.ExcludeTheSource:
                        if (SelectedEventItem != null)
                        {
                            if (sourceFilter.UncheckFilter(SelectedEventItem.Record.ProviderName))
                                sourceFilter.Apply();
                        }
                        break;
                    case ContextMenuKind.ResetFilters:
                        ResetFilters();
                        break;
                    default:
                        break;
                }
            }
        }
        private void OnFilterUpdated(object sender, System.EventArgs e)
        {
            UpdateFilterInfoText();
        }

        public ICommand RefreshCommand
        {
            get;
            private set;
        }
        public ICommand CancelCommand
        {
            get;
            private set;
        }
        public ICommand ApplySourceFilterCommand
        {
            get;
            private set;
        }
        public ICommand ApplyLevelFilterCommand
        {
            get;
            private set;
        }
        public ICommand ResetFiltersCommand
        {
            get;
            private set;
        }
        public ICommand OpenDetailsCommand
        {
            get;
            private set;
        }
        public ICommand ContextMenuCommand
        {
            get;
            private set;
        }

        private void InitializeCommands()
        {
            RefreshCommand = new RelayCommand(Refresh, null);
            CancelCommand = new RelayCommand(Cancel, () => IsUpdating);
            ResetFiltersCommand = new RelayCommand(ResetFilters, null);
            ApplySourceFilterCommand = new RelayCommand(ApplySourceFilter, null);
            ApplyLevelFilterCommand = new RelayCommand(ApplyLevelFilter, null);
            OpenDetailsCommand = new RelayCommand(OpenDetails, null);
            ContextMenuCommand = new RelayCommand<object>(ContextMenu, null);
        }
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
            RaisePropertyChanged("FromDateTime");
            RaisePropertyChanged("ToDateTime");
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

            //TODO: Improve performance with AddRange in ObservableCollection...
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
            SelectedLogSource = logSourceMgr.AddEvtx(token.FilePath);
        }

        private void Handle_DetailWindowMessageToken(DetailWindowMessageToken token)
        {
            showWindowService = token.ShowWindowService;
        }
    }
}