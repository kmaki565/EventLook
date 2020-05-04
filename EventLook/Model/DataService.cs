using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.Model
{
    public interface IDataService
    {
        IEnumerable<EventRecord> GetEvents();
    }
    public class DataService : IDataService
    {
        public IEnumerable<EventRecord> GetEvents()
        {
            string sQuery = string.Format(" *[System[TimeCreated[@SystemTime >= '{0}' and @SystemTime <= '{1}']]]",
                DateTime.UtcNow.AddDays(-7).ToString("s"),
                DateTime.UtcNow.ToString("s"));

            var elQuery = new EventLogQuery("System", PathType.LogName, sQuery);
            elQuery.ReverseDirection = true;
            var elReader = new System.Diagnostics.Eventing.Reader.EventLogReader(elQuery);

            var eventList = new List<EventRecord>();
            for (var eventInstance = elReader.ReadEvent();
                null != eventInstance; eventInstance = elReader.ReadEvent())
            {
                eventList.Add(eventInstance);
            }
            return eventList;
        }
    }
}
