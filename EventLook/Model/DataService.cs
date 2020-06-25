using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventLook.Model
{
    public interface IDataService
    {
        Task<int> ReadEvents(string eventSource, DateTime fromTime, DateTime toTime, IProgress<ProgressInfo> progress);
        
        void Cancel();
    }
    public class DataService : IDataService
    {
        private CancellationTokenSource cts;
        public async Task<int> ReadEvents(string eventSource, DateTime fromTime, DateTime toTime, IProgress<ProgressInfo> progress)
        {
            using (cts = new CancellationTokenSource())
            {
                // Event records to be sent to the ViewModel
                var eventRecords = new List<EventRecord>();
                string errMsg = "";
                int count = 0;
                bool isFirst = true;
                try
                {
                    string sQuery = string.Format(" *[System[TimeCreated[@SystemTime > '{0}' and @SystemTime <= '{1}']]]",
                        fromTime.ToUniversalTime().ToString("s"),
                        toTime.ToUniversalTime().ToString("s"));

                    var elQuery = new EventLogQuery(eventSource, PathType.LogName, sQuery)
                    {
                        ReverseDirection = true
                    };
                    var reader = new System.Diagnostics.Eventing.Reader.EventLogReader(elQuery);
                    var eventRecord = reader.ReadEvent();
                    Debug.WriteLine("Begin Reading");
                    await Task.Run(() =>
                    {
                        for (; eventRecord != null; eventRecord = reader.ReadEvent())
                        {
                            cts.Token.ThrowIfCancellationRequested();

                            eventRecords.Add(eventRecord);
                            ++count;
                            if (count % 100 == 0)
                            {
                                var info = new ProgressInfo(eventRecords.ConvertAll(e => new EventItem(e)), false, isFirst);
                                cts.Token.ThrowIfCancellationRequested();
                                progress.Report(info);
                                isFirst = false;
                                eventRecords.Clear();
                            }
                        }
                    });
                }
                catch (OperationCanceledException ex)
                {
                    errMsg = ex.Message;
                }
                catch (EventLogNotFoundException ex)
                {
                    errMsg = ex.Message;
                }
                catch (UnauthorizedAccessException)
                {
                    errMsg = "Unauthorized access to the channel. Try Run as Administrator.";
                }
                finally
                {
                    var info_comp = new ProgressInfo(eventRecords.ConvertAll(e => new EventItem(e)), true, isFirst, errMsg);
                    progress.Report(info_comp);
                    Debug.WriteLine("End Reading");
                }
                return count;
            }
        }
        public void Cancel()
        {
            cts.Cancel();
        }
    }
}
