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

namespace EventLook.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel(IDataService dataService)
        {
            InitializeCommands();
            DataService = dataService;
            Events = new ObservableCollection<EventItem>();

            logSourceMgr = new LogSourceMgr();
            SelectedLogSource = LogSources.FirstOrDefault();

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
        private readonly Progress<ProgressInfo> progress;
        private readonly Stopwatch stopwatch;
        private bool isWindowLoaded = false;
        private bool isUpdating = false;
        private int loadedEventCount = 0;

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
                if (isWindowLoaded) Task.Run(() => LoadEvents());
            }
        }

        private string statusText;
        public string StatusText
        {
            get
            {
                return statusText;
            }
            private set
            {
                if (value == statusText)
                    return;

                statusText = value;
                RaisePropertyChanged();
            }
        }

        public void OnLoaded()
        {
            Task.Run(() => LoadEvents());
            isWindowLoaded = true;
        }
        public override void Cleanup()
        {
            Messenger.Default.Unregister<ViewCollectionViewSourceMessageToken>(this);
            base.Cleanup();
        }

        private void InitializeCommands()
        {
            //TODO: 
        }
        private async Task LoadEvents()
        {
            if (isUpdating)
                DataService.Cancel();

            await Update(DataService.ReadEvents(selectedLogSource.Name, 7, progress));
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
                stopwatch.Restart();
                loadedEventCount = 0;
                isUpdating = true;
                UpdateStatusText();
                await task;
            }
            finally
            {
                isUpdating = false;
                stopwatch.Stop();
                UpdateStatusText();
            }
        }
        private void UpdateStatusText()
        {
            StatusText = isUpdating ?
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