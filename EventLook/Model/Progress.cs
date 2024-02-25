using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.Model;

public class ProgressInfo(IReadOnlyList<EventItem> eventItems, bool isComplete, bool isFirst, int totalEventCount = 0, string message = "")
{
    public IReadOnlyList<EventItem> LoadedEvents { get; } = eventItems;

    public bool IsComplete { get; } = isComplete;
    public bool IsFirst { get; } = isFirst;
    /// <summary>
    /// Record count information for the local Event Log. 
    /// Can be 0 if the count is not determined yet or not applicable. 
    /// For example, when auto refresh is on, or for a .evtx file, it's always 0.
    /// </summary>
    public int RecordCountInfo { get; } = totalEventCount;
    public string Message { get; } = message;
}
