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
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
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
        public void OnLoaded()
        {
            LoadEvents();
            isWindowLoaded = true;
        }
        public override void Cleanup()
        {
            Messenger.Default.Unregister<ViewCollectionViewSourceMessageToken>(this);
            base.Cleanup();
        }

        private void LoadEvents()
        {
            if (isUpdating)
                DataService.Cancel();
            isUpdating = true;
            stopwatch.Restart();
            StatusText = "Loading...";
            Task.Run(() => DataService.ReadEvents(selectedLogSource.Name, 7, progress));
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

            if (progressInfo.IsComplete)
            {
                isUpdating = false;
                stopwatch.Stop();
                StatusText = $"{Events.Count} events loaded. ({stopwatch.Elapsed.TotalSeconds:F1} sec)"; // 1 digit after decimal point
            }
            else
            {
                StatusText = $"Loading {Events.Count} events...";
            }
        }
        /// <summary>
        /// Gets or sets the IDownloadDataService member
        /// </summary>
        internal IDataService DataService { get; set; }
        /// <summary>
        /// Gets or sets the CollectionViewSource which is the proxy for the 
        /// collection of Things and the datagrid in which each thing is displayed.
        /// </summary>
        private CollectionViewSource CVS { get; set; }
        private readonly LogSourceMgr logSourceMgr;
        private readonly Progress<ProgressInfo> progress;
        private readonly Stopwatch stopwatch;
        private bool isWindowLoaded = false;
        private bool isUpdating = false;

        #region Properties (Displayable in View)
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
                selectedLogSource = value;
                if (isWindowLoaded) LoadEvents();
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
        #endregion

        private void InitializeCommands()
        {
            //TODO: 
        }

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