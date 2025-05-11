using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EventLook.Model;

public class IdFilter : FilterBase
{
    private List<int> _filterIds;
    public IdFilter()
    {
        _filterIds = [];
    }

    private string _filterText = "";
    public string FilterText
    {
        get { return _filterText; }
        set
        {
            if (value != _filterText)
            {
                _filterText = value;
                UpdateFilterIds(_filterText);
                NotifyPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Updates the filter text with the given ID. 
    /// The filter ID list will be updated accordingly.
    /// </summary>
    /// <param name="id"></param>
    public void AddFilterId(int id)
    {
        if (_filterIds.Contains(id))
            return;

        FilterText = (_filterIds.Count == 0) ? id.ToString() : FilterText + "," + id.ToString();
    }

    public override void Refresh(IEnumerable<EventItem> events, bool reset)
    {
        if (reset)
            Clear();
    }
    public override void Clear()
    {
        FilterText = "";
    }

    protected override bool IsFilterMatched(EventItem evt)
    {
        if (_filterIds.Count == 0)
            return true;

        IEnumerable<int> excludeIds = _filterIds.Where(fi => fi < 0).Select(fi => fi * -1);
        if (excludeIds.Any(fi => evt.Record.Id == fi))
            return false;

        IEnumerable<int> includeIds = _filterIds.Where(fi => fi >= 0);
        if (includeIds.Count() == 0)
            return true;

        return includeIds.Any(fi => evt.Record.Id == fi);
    }

    private void UpdateFilterIds(string filterText)
    {
        if (filterText is null)
            throw new Exception("Filter text cannot be null.");

        _filterIds.Clear();

        string[] filterParts = filterText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in filterParts)
        {
            // Exception to be handled in the view.
            _filterIds.Add(int.Parse(part.Trim()));
        }
        Apply();
    }
}
