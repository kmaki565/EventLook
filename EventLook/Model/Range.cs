using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.Model;

public class Range
{
    public string Text { get; set; }
    public int DaysFromNow { get; set; }
    public bool IsCustom { get; set; }
}
public class RangeMgr
{
    public RangeMgr()
    {
        Ranges = new ObservableCollection<Range>
        {
            new Range() { Text = "Last 24 hours", DaysFromNow = 1, IsCustom = false },
            new Range() { Text = "Last 3 days", DaysFromNow = 3, IsCustom = false },
            new Range() { Text = "Last 7 days", DaysFromNow = 7, IsCustom = false },
            new Range() { Text = "Last 15 days", DaysFromNow = 15, IsCustom = false },
            new Range() { Text = "Last 30 days", DaysFromNow = 30, IsCustom = false },
            new Range() { Text = "All time", DaysFromNow = 0, IsCustom = false },
            new Range() { Text = "Custom range", DaysFromNow = 0, IsCustom = true },
        };
    }
    public ObservableCollection<Range> Ranges { get; set; }

    public Range GetStartupRange()
    {
        // Default is "All time".
        // Note that user's choice "Custom range" is handled as "All time" at startup as we don't save IsCustom property.
        return Ranges.FirstOrDefault(x => x.DaysFromNow == Properties.Settings.Default.StartupRangeDays) ??
            Ranges.First(x => x.DaysFromNow == 0 && x.IsCustom == false);
    }
}
