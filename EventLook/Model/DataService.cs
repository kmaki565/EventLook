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
        Task LoadEventsAsync(string eventSource, int range, IProgress<EventItem> progress);
    }
    public class DataService : IDataService
    {
        public Task LoadEventsAsync(string eventSource, int range, IProgress<EventItem> progress)
        {
            string sQuery = string.Format(" *[System[TimeCreated[@SystemTime > '{0}' and @SystemTime <= '{1}']]]",
                DateTime.UtcNow.AddDays(-1 * range).ToString("s"),
                DateTime.UtcNow.ToString("s"));

            var elQuery = new EventLogQuery(eventSource, PathType.LogName, sQuery);
            elQuery.ReverseDirection = true;
            var elReader = new System.Diagnostics.Eventing.Reader.EventLogReader(elQuery);

            return Task.Run(
                () =>
                {
                    int count = 1;
                    for (var eventInstance = elReader.ReadEvent(); eventInstance != null; eventInstance = elReader.ReadEvent(), ++count)
                    {
                        //TODO: This has a performance issue
                        progress.Report(new EventItem(eventInstance));
                    }
                });
        }
    }
}
