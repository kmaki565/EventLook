using EventLook.Model;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
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

            //--------------------------------------------------------------
            // This 'registers' the instance of this view model to receive messages with this type of token.  This 
            // is used to receive a reference from the view that the collectionViewSource has been instantiated
            // and to receive a reference to the CollectionViewSource which will be used in the view model for 
            // filtering
            Messenger.Default.Register<ViewCollectionViewSourceMessageToken>(this, Handle_ViewCollectionViewSourceMessageToken);
        }
        public void OnLoaded()
        {
            StatusText = "Loading...";
            var progress = new Progress<ProgressInfo>(ProgressCallback); // Needs to instantiate in UI thread
            Task.Run(() => DataService.LoadEvents("System", 3, progress));
        }
        public override void Cleanup()
        {
            Messenger.Default.Unregister<ViewCollectionViewSourceMessageToken>(this);
            base.Cleanup();
        }

        private void ProgressCallback(ProgressInfo progressInfo)
        {
            //TODO: AddRange
            foreach (var evt in progressInfo.LoadedEvents)
            {
                Events.Add(evt);
            }

            StatusText = progressInfo.IsComplete 
                ? $"{Events.Count} events loaded."
                : $"Loading {Events.Count} events...";
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