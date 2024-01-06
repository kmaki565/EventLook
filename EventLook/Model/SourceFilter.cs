using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EventLook.Model;

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

    public override void Refresh(IEnumerable<EventItem> events, bool reset)
    {
        // Remember filters and their selections before clearing (needs ToList)
        var prevFilters = reset ? null : sourceFilters.Select(f => new { f.Name, f.Selected }).ToList();

        sourceFilters.Clear();

        var distinctSources = events.Select(e => e.Record.ProviderName).Distinct().OrderBy(s => s);
        foreach (var s in distinctSources)
        {
            sourceFilters.Add(new SourceFilterItem
            {
                Name = s,
                Selected = prevFilters?.FirstOrDefault(f => f.Name == s)?.Selected ?? true
            });
        }

        Apply();
    }
    public override void Clear()
    {
        foreach (var sf in sourceFilters)
        {
            sf.Selected = true;
        }
        Apply();
    }
    public override void Reset()
    {
        sourceFilters.Clear();
    }

    protected override bool IsFilterMatched(EventItem evt)
    {
        if (sourceFilters.Count == 0)
            return true;

        return sourceFilters.Where(sf => sf.Selected).Any(sf => string.Equals(sf.Name, evt.Record.ProviderName));
    }

    /// <summary>
    /// Selects (Checks) only filter with the given name, and unchecks the other filters.
    /// </summary>
    /// <param name="name"></param>
    /// <returns>Returns true if success.</returns>
    public bool SetSingleFilter(string name)
    {
        if (sourceFilters.Any(x => x.Name == name) == false)
            return false;

        foreach (var filterItem in sourceFilters)
        {
            filterItem.Selected = (filterItem.Name == name);
        }
        return true;
    }
    /// <summary>
    /// Unchecks filter with the given name. i.e., Filters out the source.
    /// </summary>
    /// <param name="name"></param>
    /// <returns>Returns true if success.</returns>
    public bool UncheckFilter(string name)
    {
        if (sourceFilters.Any(x => x.Name == name) == false)
            return false;

        foreach (var filterItem in sourceFilters)
        {
            if (filterItem.Name == name)
                filterItem.Selected = false;
        }
        return true;
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
