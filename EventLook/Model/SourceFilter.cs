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
    public class SourceFilter : FilterBase
    {
        public SourceFilter()
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

        public override void Init(IEnumerable<EventItem> events)
        {
            sourceFilters.Clear();
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
        public override void Clear(CollectionViewSource cvs)
        {
            RemoveFilter(cvs);
            sourceFilters.Clear();
        }
        public override void Reset(CollectionViewSource cvs)
        {
            RemoveFilter(cvs);
            foreach (var sf in SourceFilters)
            {
                sf.Selected = true;
            }
        }

        protected override void DoFilter(object sender, FilterEventArgs e)
        {
            // Set false if the event does not match any checked items in the CheckComboBox
            if (!(e.Item is EventItem evt))
                e.Accepted = false;
            else if (!SourceFilters.Where(sf => sf.Selected).Any(sf => String.Compare(sf.Name, evt.Record.ProviderName) == 0))
                e.Accepted = false;
        }
    }

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
}
