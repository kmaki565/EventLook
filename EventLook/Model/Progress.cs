using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.Model
{
    public class ProgressInfo
    {
        public ProgressInfo(IReadOnlyList<EventItem> eventItems, bool isComplete)
        {
            LoadedEvents = eventItems;
            IsComplete = isComplete;
        }

        public IReadOnlyList<EventItem> LoadedEvents { get; }

        public bool IsComplete { get; }
    }
}
