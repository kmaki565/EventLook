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
        Task<List<EventItem>> FetchInitialEventsAsync();
        Task<List<EventItem>> FetchRestOfEventsAsync();
    }
    public class DataService : IDataService
    {
        public Task<List<EventItem>> FetchInitialEventsAsync()
        {
            return FetchEventsAsync(0, 1, 10);
        }
        public Task<List<EventItem>> FetchRestOfEventsAsync()
        {
            //TODO this could miss events
            return FetchEventsAsync(0, 7);
        }
        private Task<List<EventItem>> FetchEventsAsync(int startDay, int endDay, int quitNum=0)
        {
            string sQuery = string.Format(" *[System[TimeCreated[@SystemTime > '{0}' and @SystemTime <= '{1}']]]",
                DateTime.UtcNow.AddDays(-1 * endDay).ToString("s"),
                DateTime.UtcNow.AddDays(-1 * startDay).ToString("s"));

            var elQuery = new EventLogQuery("System", PathType.LogName, sQuery);
            elQuery.ReverseDirection = true;
            var elReader = new System.Diagnostics.Eventing.Reader.EventLogReader(elQuery);

            var eventList = new List<EventItem>();

            return Task.Run(
                () =>
                {
                    for (var eventInstance = elReader.ReadEvent(); null != eventInstance; eventInstance = elReader.ReadEvent())
                    {
                        quitNum--;
                        eventList.Add(new EventItem(eventInstance));
                        if (quitNum == 0)
                            break;
                    }
                    return eventList;
                });
        }
    }
}
