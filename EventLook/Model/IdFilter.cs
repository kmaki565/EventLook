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
    // Assume Event IDs are always positive (can be 0).
    private readonly List<uint> _filterIds;
    private readonly List<uint> _excludeIds;
    public IdFilter()
    {
        _filterIds = [];
        _excludeIds = [];
    }

    // For testing purpose.
    public IReadOnlyList<uint> FilterIds => _filterIds.AsReadOnly();
    public IReadOnlyList<uint> ExcludeIds => _excludeIds.AsReadOnly();

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
                Apply();
                NotifyPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Updates the filter text with the given ID. 
    /// The filter ID list will be updated accordingly.
    /// </summary>
    /// <param name="id"></param>
    public void AddFilterId(int id, bool isExclude)
    {
        FilterText += ((_filterIds.Count + _excludeIds.Count > 0) ? "," : "")
            + (isExclude ? $"-{id}" : $"{id}");
    }

    public override void Clear()
    {
        FilterText = "";
    }

    protected override bool IsFilterMatched(EventItem evt)
    {
        if (_filterIds.Count + _excludeIds.Count == 0)
            return true;

        if (_excludeIds.Any(fi => evt.Record.Id == fi))
            return false;

        if (_filterIds.Count == 0)
            return true;

        return _filterIds.Any(fi => evt.Record.Id == fi);
    }

    private void UpdateFilterIds(string filterText)
    {
        _filterIds.Clear();
        _excludeIds.Clear();

        string[] filterParts = filterText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in filterParts)
        {
            if (part.Trim() == "-0")
            {
                _excludeIds.Add(0);
                continue;
            }

            // Exception to be handled by the view.
            int id = int.Parse(part.Trim());

            if (id < 0)
                _excludeIds.Add((uint)(-1 * id));
            else
                _filterIds.Add((uint)id);
        }
    }
}
