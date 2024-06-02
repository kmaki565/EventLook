using EventLook.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.ViewModel;

public class DetailViewModel(EventItem eventItem, LogSource logSource)
{
    public EventItem Event { get; set; } = eventItem;
    public string FormattedXml
    {
        get
        {
            string xml = Event.GetXml(logSource);
            if (string.IsNullOrEmpty(xml))
                return "Could not retrieve XML.";

            return TextHelper.FormatXml(xml);
        }
    }
}
