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
    public EventItem(EventRecord eventRecord)
    {
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

    public EventRecord Record { get; set; }
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

    public void Dispose() => Record.Dispose();
}
