﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.Model;

public class LevelFilter : FilterBase
{
    public LevelFilter()
    {
        levelFilters = new ObservableCollection<LevelFilterItem>();
        LevelFilters = new ReadOnlyObservableCollection<LevelFilterItem>(levelFilters);
    }

    /// <summary>
    /// Collection of the checkboxes to filter by event level
    /// </summary>
    private readonly ObservableCollection<LevelFilterItem> levelFilters;
    public ReadOnlyObservableCollection<LevelFilterItem> LevelFilters
    {
        get;
        private set;
    }

    public override void Populate(IEnumerable<EventItem> events, bool reset)
    {
        // Remember filters and their selections before clearing (needs ToList)
        var prevFilters = reset ? null : levelFilters.Select(f => new { f.Level, f.Selected }).ToList();

        levelFilters.Clear();

        var distinctLevels = events.Select(e => e.Record.Level).Distinct().OrderBy(lv => lv);
        foreach (var lv in distinctLevels)
        {
            levelFilters.Add(new LevelFilterItem
            {
                Level = lv,
                Selected = prevFilters?.FirstOrDefault(f => f.Level == lv)?.Selected ?? true
            });
        }

        Apply();
    }
    public override void Clear()
    {
        foreach (var lf in levelFilters)
        {
            lf.Selected = true;
        }
        Apply();
    }
    public override void Reset()
    {
        levelFilters.Clear();
    }
    public override void Apply()
    {
        if (IsFilterSelectionsChanged())
        {
            base.Apply();
        }
    }

    protected override bool IsFilterMatched(EventItem evt)
    {
        if (levelFilters.Count == 0)
            return true;

        return levelFilters.Where(lf => lf.Selected).Any(lf => lf.Level == evt.Record.Level);
    }

    /// <summary>
    /// Selects (Checks) only filter with the given level, and unchecks the other filters.
    /// </summary>
    /// <returns>Returns true if success.</returns>
    public bool SetSingleFilter(byte? level)
    {
        if (levelFilters.Any(x => x.Level == level) == false)
            return false;

        foreach (var filterItem in levelFilters)
        {
            filterItem.Selected = (filterItem.Level == level);
        }
        return true;
    }
    /// <summary>
    /// Unchecks filter with the given level. i.e., Filters out the level.
    /// </summary>
    /// <returns>Returns true if success.</returns>
    public bool UncheckFilter(byte? level)
    {
        if (levelFilters.Any(x => x.Level == level) == false)
            return false;

        foreach (var filterItem in levelFilters)
        {
            if (filterItem.Level == level)
                filterItem.Selected = false;
        }
        return true;
    }

    private readonly List<LevelFilterItem> _prevFilters = new List<LevelFilterItem>();
    private bool IsFilterSelectionsChanged()
    {
        bool changed = levelFilters.Count != _prevFilters.Count || levelFilters.Where((lf, i) => lf.Level != _prevFilters[i].Level || lf.Selected != _prevFilters[i].Selected).Any();

        if (changed)
        {
            _prevFilters.Clear();
            foreach (var lf in levelFilters)
            {
                _prevFilters.Add(new LevelFilterItem { Level = lf.Level, Selected = lf.Selected });
            }
        }
        return changed;
    }
}

public class LevelFilterItem : Monitorable
{
    private byte? level;
    public byte? Level
    {
        get { return level; }
        set
        {
            if (value == level)
                return;

            level = value;
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

    public override string ToString()
    {
        if (!level.HasValue)
            return "Invalid level";

        // In the Event Viewer, Level 0 is shown as Information.
        return (level.Value == 1) ? "Critical" :
            (level.Value == 2) ? "Error" :
            (level.Value == 3) ? "Warning" :
            (level.Value == 4) ? "Information" :
            (level.Value == 5) ? "Verbose" :
            "Unknown level";
    }
}
