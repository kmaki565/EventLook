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
        public override void Clear()
        {
            RemoveFilter();
            sourceFilters.Clear();
        }
        public override void Reset()
        {
            RemoveFilter();
            foreach (var sf in SourceFilters)
            {
                sf.Selected = true;
            }
        }

        protected override bool IsFilterMatched(EventItem evt)
        {
            return SourceFilters.Where(sf => sf.Selected).Any(sf => String.Compare(sf.Name, evt.Record.ProviderName) == 0);
        }
    }

    public class SourceFilterItem : Monitorable
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (value == _name)
                    return;

                _name = value;
                NotifyPropertyChanged();
            }
        }

        private bool _selected;
        public bool Selected
        {
            get { return _selected; }
            set
            {
                if (value == _selected)
                    return;

                _selected = value;
                NotifyPropertyChanged();
            }
        }
    }
}
