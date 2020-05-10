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
        void LoadEvents(string eventSource, int range, IProgress<ProgressInfo> progress);
    }
    public class DataService : IDataService
    {
        public void LoadEvents(string eventSource, int range, IProgress<ProgressInfo> progress)
        {
            string sQuery = string.Format(" *[System[TimeCreated[@SystemTime > '{0}' and @SystemTime <= '{1}']]]",
                DateTime.UtcNow.AddDays(-3 * range).ToString("s"),
                DateTime.UtcNow.ToString("s"));

            var elQuery = new EventLogQuery(eventSource, PathType.LogName, sQuery);
            elQuery.ReverseDirection = true;
            var reader = new System.Diagnostics.Eventing.Reader.EventLogReader(elQuery);

            var accumulatedEvents = new List<EventRecord>();
            int count = 0;
            for (var eventRecord = reader.ReadEvent(); eventRecord != null; eventRecord = reader.ReadEvent())
            {
                accumulatedEvents.Add(eventRecord);
                ++count;
                if (count % 100 == 0)
                {
                    progress.Report(new ProgressInfo(accumulatedEvents.ConvertAll(e => new EventItem(e)), false));
                    accumulatedEvents.Clear();
                }
            }
            progress.Report(new ProgressInfo(accumulatedEvents.ConvertAll(e => new EventItem(e)), true));
        }
    }
}
