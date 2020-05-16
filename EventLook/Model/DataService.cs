using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventLook.Model
{
    public interface IDataService
    {
        void ReadEvents(string eventSource, int range, IProgress<ProgressInfo> progress);
        void Cancel();
    }
    public class DataService : IDataService
    {
        private CancellationTokenSource cts;
        public void ReadEvents(string eventSource, int range, IProgress<ProgressInfo> progress)
        {
            using (cts = new CancellationTokenSource())
            {
                // Event records to be sent to the ViewModel
                var eventRecords = new List<EventRecord>();
                int count = 0;
                bool isFirst = true;
                try
                {
                    string sQuery = string.Format(" *[System[TimeCreated[@SystemTime > '{0}' and @SystemTime <= '{1}']]]",
    DateTime.UtcNow.AddDays(-1 * range).ToString("s"),
    DateTime.UtcNow.ToString("s"));

                    var elQuery = new EventLogQuery(eventSource, PathType.LogName, sQuery)
                    {
                        ReverseDirection = true
                    };
                    var reader = new System.Diagnostics.Eventing.Reader.EventLogReader(elQuery);

                    for (var eventRecord = reader.ReadEvent(); eventRecord != null; eventRecord = reader.ReadEvent())
                    {
                        cts.Token.ThrowIfCancellationRequested();

                        eventRecords.Add(eventRecord);
                        ++count;
                        if (count % 100 == 0)
                        {
                            progress.Report(new ProgressInfo(eventRecords.ConvertAll(e => new EventItem(e)), false, isFirst));
                            isFirst = false;
                            eventRecords.Clear();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                progress.Report(new ProgressInfo(eventRecords.ConvertAll(e => new EventItem(e)), true, isFirst));
            }
        }
        public void Cancel()
        {
            cts.Cancel();
        }
    }
}
