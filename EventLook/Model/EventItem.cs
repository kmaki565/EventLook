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
public class EventItem
{
    public EventItem(EventRecord eventRecord)
    {
        Record = eventRecord;
        TimeOfEvent = eventRecord.TimeCreated?.ToUniversalTime() ?? DateTime.MinValue.ToUniversalTime();
        try
        {
            Message = eventRecord.FormatDescription();

            //TODO: This behavior is not aligned to Event Viewer. We may want to add a setting to opt-in this behavior.
            if (Message == null) 
            {
                var sb = new StringBuilder();
                sb.Append("");
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
    #region Properties
    public EventRecord Record { get; set; }
    public DateTime TimeOfEvent { get; }
    public string Message { get; }
    public string MessageOneLine { get { return Regex.Replace(Message, @"[\r\n]+", " "); } }
    #endregion
}
