using EventLook.Model;
using System.Collections.Generic;
using System.Windows.Documents;

namespace EventLook.ViewModel;

public class SettingsData
{
    public List<LogSource> LogSources { get; set; }
    public List<Range> Ranges { get; set; }
    public Range SelectedRange { get; set; }
    public bool ShowsMillisec { get; set; }
    public bool ShowsRecordId { get; set; }
}