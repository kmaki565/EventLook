using EventLook.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.ViewModel
{
    public class DetailViewModel
    {
        public DetailViewModel(EventItem eventItem)
        {
            Event = eventItem;
        }
        public EventItem Event { get; set; }
        public string FormattedXml { get { return TextHelper.FormatXml(Event.Record.ToXml()); } }
    }
}
