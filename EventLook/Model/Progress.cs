using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.Model;

public class ProgressInfo
{
    public ProgressInfo(IReadOnlyList<EventItem> eventItems, bool isComplete, bool isFirst, string message = "")
    {
        LoadedEvents = eventItems;
        IsComplete = isComplete;
        IsFirst = isFirst;
        Message = message;
    }

    public IReadOnlyList<EventItem> LoadedEvents { get; }

    public bool IsComplete { get; }
    public bool IsFirst { get; }
    public string Message { get; set; }
}
