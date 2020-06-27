using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EventLook.Model
{
    public class SourceFilterItem : INotifyPropertyChanged
    {
        private bool _selected;
        private string _name;

        public string Name
        {
            get => _name; set
            {
                _name = value;
                EmitChange(nameof(Name));
            }
        }
        public bool Selected
        {
            get => _selected; set
            {
                _selected = value;
                EmitChange(nameof(Selected));
            }
        }

        private void EmitChange(params string[] names)
        {
            if (PropertyChanged == null)
                return;
            foreach (var name in names)
                PropertyChanged(this,
                  new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler
                       PropertyChanged;
    }
    public class SourceFilterMgr
    {
        public SourceFilterMgr()
        {
            sourceFilters = new ObservableCollection<SourceFilterItem>();
            SourceFilters = new ReadOnlyObservableCollection<SourceFilterItem>(sourceFilters);
        }

        /// <summary>
        /// Collection of the checkboxes to filter by event source
        /// </summary>
        private readonly ObservableCollection<SourceFilterItem> sourceFilters;
        public ReadOnlyObservableCollection<SourceFilterItem> SourceFilters 
        { 
            get; 
            private set; 
        }

        public void Clear()
        {
            sourceFilters.Clear();
        }
        public void PopulateEvents(IEnumerable<EventItem> events)
        {
            var distinctSources = events.Select(e => e.Record.ProviderName).Distinct().OrderBy(s => s);
            foreach (var s in distinctSources)
            {
                sourceFilters.Add(new SourceFilterItem
                {
                    Name = s,
                    Selected = true
                });
            }
        }

        private bool hasFilterAdded = false;
        public void AddFilter(CollectionViewSource cvs)
        {
            cvs.Filter += FilterBySources;
            hasFilterAdded = true;
        }
        public void RemoveFilter(CollectionViewSource cvs)
        {
            if (hasFilterAdded)
                cvs.Filter -= FilterBySources;
        }
        public void RefreshFilter(CollectionViewSource cvs)
        {
            RemoveFilter(cvs);
            AddFilter(cvs);
        }
        public void ResetFilter(CollectionViewSource cvs)
        {
            foreach (var sf in SourceFilters)
            {
                sf.Selected = true;
            }
            RemoveFilter(cvs);
        }

        private void FilterBySources(object sender, FilterEventArgs e)
        {
            // Set false if the event does not match any checked items in the CheckComboBox
            if (!(e.Item is EventItem evt))
                e.Accepted = false;
            else if (!SourceFilters.Where(sf => sf.Selected).Any(sf => String.Compare(sf.Name, evt.Record.ProviderName) == 0))
                e.Accepted = false;
        }
    }
}
