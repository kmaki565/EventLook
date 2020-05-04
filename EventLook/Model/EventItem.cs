using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.Model
{
    /// <summary>
    /// Represents a event to display. 
    /// Could not inherit EventLogRecord as it doesn't have a public constructor.
    /// </summary>
    public class EventItem
    {
        public EventItem(EventRecord eventRecord)
        {
            _event = eventRecord;
            TimeCreated = _event.TimeCreated ?? DateTime.MinValue;
            ProviderName = _event.ProviderName;
            LevelDisplayName = _event.LevelDisplayName;
            EventId = _event.Id;
            Message = _event.FormatDescription();
        }
        #region Properties (to display in View)
        public DateTime TimeCreated { get; }
        public string ProviderName { get; }
        public string LevelDisplayName { get; }
        public int EventId { get; }
        public string Message { get; }
        #endregion

        private EventRecord _event;
        public EventRecord GetEventRecord()
        {
            return _event;
        }
    }
}
