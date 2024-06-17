using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EventLook.Model;

/// <summary>
/// Represents a event to display. 
/// Could not inherit EventLogRecord as it doesn't have a public constructor.
/// </summary>
public class EventItem : IDisposable
{
    public EventItem(LogSource logSource, EventRecord eventRecord)
    {
        LogSource = logSource;
        Record = eventRecord;
        TimeOfEvent = eventRecord.TimeCreated?.ToUniversalTime() ?? DateTime.MinValue.ToUniversalTime();
        try
        {
            Message = eventRecord.FormatDescription();

            // Formatting event message failed. Try to display information in EventData.
            if (Message == null) 
            {
                var sb = new StringBuilder("(EventLook - dump of EventData)\r\n");
                for (int i = 0; i < eventRecord.Properties.Count; i++)
                {
                    if (i > 0) sb.Append("\r\n");

                    var property = eventRecord.Properties[i];
                    if (property.Value is string str)
                    {
                        sb.Append(str);
                    }
                    else if (property.Value is byte[] bytes)
                    {
                        foreach (var bin in bytes)
                        {
                            sb.Append(string.Format("{0:X2}", bin));
                        }    
                    }
                }
                Message = sb.ToString();
            }
        }
        catch (Exception ex)
        {
            Message = "(EventLook) Exception occurred while reading the description:\r\n" + ex.Message;
        }
    }

    public LogSource LogSource { get; }
    public EventRecord Record { get; }
    public DateTime TimeOfEvent { get; }
    public string Message { get; }
    public string MessageOneLine { get { return Regex.Replace(Message, @"[\r\n]+", " "); } }
    /// <summary>
    /// Indicates the time when the event was loaded by the app.
    /// </summary>
    public DateTime TimeLoaded { get; set; }
    /// <summary>
    /// Indicates if the event is newly loaded.
    /// </summary>
    public bool IsNewLoaded { get; set; }

    public void Dispose()
    {
        Record.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Fetches the XML representation of the event from log source and event record ID.
    /// If the XML is already fetched, it returns the cached XML. 
    /// Returns null if XML cannot be fetched.
    /// </summary>
    private string _xml;
    public string GetXml()
    {
        if (_xml == null && Record.RecordId.HasValue)
        {
            EventLogQuery query = new(LogSource.Path, LogSource.PathType, $"*[System/EventRecordID={Record.RecordId.Value}]");
            EventLogReader reader = null;
            EventRecord eventRecord = null;
            try
            {
                reader = new EventLogReader(query);
                eventRecord = reader.ReadEvent();
                if (eventRecord != null)
                {
                    _xml = TextHelper.FormatXml(eventRecord.ToXml());
                }
            }
            finally
            {
                eventRecord?.Dispose();
                reader?.Dispose();
            }
        }
        return _xml;
    }
}
