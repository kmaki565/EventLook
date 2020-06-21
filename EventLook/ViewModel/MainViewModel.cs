using EventLook.Model;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
            DataService = dataService;
            Events = new ObservableCollection<EventItem>();
            SourceFilters = new ObservableCollection<SourceFilterItem>();

            logSourceMgr = new LogSourceMgr();
            SelectedLogSource = LogSources.FirstOrDefault();

            rangeMgr = new RangeMgr();
            SelectedRange = Ranges.FirstOrDefault(r => r.DaysFromNow == 3);

            progress = new Progress<ProgressInfo>(ProgressCallback); // Needs to instantiate in UI thread
            stopwatch = new Stopwatch();

            //--------------------------------------------------------------
            // This 'registers' the instance of this view model to receive messages with this type of token.  This 
            // is used to receive a reference from the view that the collectionViewSource has been instantiated
            // and to receive a reference to the CollectionViewSource which will be used in the view model for 
            // filtering
            Messenger.Default.Register<ViewCollectionViewSourceMessageToken>(this, Handle_ViewCollectionViewSourceMessageToken);
        }
        private readonly LogSourceMgr logSourceMgr;
        private readonly RangeMgr rangeMgr;
        private readonly Progress<ProgressInfo> progress;
        private readonly Stopwatch stopwatch;
        private bool isWindowLoaded = false;
        private int loadedEventCount = 0;
        private Task ongoingTask;

        internal IDataService DataService { get; set; }

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

        private ObservableCollection<SourceFilterItem> sourceFilters;
        public ObservableCollection<SourceFilterItem> SourceFilters 
        {
            get => sourceFilters;
            set
            {
                if (sourceFilters == value)
                    return;
                sourceFilters = value;
                ApplySourceFilter();
            }
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

        public void OnLoaded()
        {
            Refresh();
            isWindowLoaded = true;
        }
        public void RefreshCancel()
        {
            if (IsUpdating)
                Cancel();
            else
                Refresh();
        }
        public async void Refresh()
        {
            SourceFilters.Clear();
            UpdateDateTimes();

            await Task.Run(() => LoadEvents());

            var distinctSources = Events.Select(e => e.Record.ProviderName).Distinct();
            foreach (var s in distinctSources)
            {
                SourceFilters.Add(new SourceFilterItem
                {
                    Name = s,
                    Selected = true
                });
            }
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
            base.Cleanup();
        }
        public void ResetFilters()
        {
            RemoveSourceFilter();
        }
        private void RemoveSourceFilter()
        {
            CVS.Filter -= new FilterEventHandler(FilterBySources);
            foreach (var sf in SourceFilters)
            {
                sf.Selected = false;
            }
        }
        private void FilterBySources(object sender, FilterEventArgs e)
        {
            var src = e.Item as EventItem;
            if (src == null)
                e.Accepted = false;
            else if (SourceFilters.Where(sf => sf.Selected).Any(sf => String.Compare(sf.Name, src.Record.ProviderName) != 0))
                e.Accepted = false;
        }
        private void ApplySourceFilter()
        {
           // CVS.Filter += new FilterEventHandler(FilterBySources);
        }
        public ICommand RefreshCancelCommand
        {
            get;
            private set;
        }
        public ICommand ResetFiltersCommand
        {
            get;
            private set;
        }

        private void InitializeCommands()
        {
            RefreshCancelCommand = new RelayCommand(RefreshCancel, null);
            ResetFiltersCommand = new RelayCommand(ResetFilters, null);
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
                ToDateTime = DateTime.Now;
                FromDateTime = ToDateTime - TimeSpan.FromDays(SelectedRange.DaysFromNow);
            }
            RaisePropertyChanged("FromDateTime");   // Workaround for (probably) DateTimePicker's bug
            RaisePropertyChanged("ToDateTime");
        }
        private async Task LoadEvents()
        {
            Cancel();

            await Update(DataService.ReadEvents(selectedLogSource.Name, FromDateTime, ToDateTime, progress));
        }
        private void ProgressCallback(ProgressInfo progressInfo)
        {
            if (progressInfo.IsFirst)
                Events.Clear();

            //TODO: AddRange
            foreach (var evt in progressInfo.LoadedEvents)
            {
                Events.Add(evt);
            }
            loadedEventCount = Events.Count;
            UpdateStatusText();
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
                await task;
            }
            finally
            {
                IsUpdating = false;
                stopwatch.Stop();
                UpdateStatusText();
            }
        }
        private void UpdateStatusText()
        {
            StatusText = IsUpdating ?
                $"Loading {loadedEventCount} events..." :
                $"{loadedEventCount} events loaded. ({stopwatch.Elapsed.TotalSeconds:F1} sec)"; // 1 digit after decimal point
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
    }
}