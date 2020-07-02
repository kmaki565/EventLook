using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EventLook.Model
{
    public class MessageFilter : FilterBase
    {
        public MessageFilter()
        {
            MessageFilterText = "";
        }

        private string messageFilterText;
        public string MessageFilterText 
        {
            get { return messageFilterText; }
            set 
            {
                if (value == messageFilterText)
                    return;

                messageFilterText = value;
                NotifyPropertyChanged();

                if (value == "")
                    RemoveFilter();
                else
                    Apply();
            }
        }

        public override void Init(IEnumerable<EventItem> events)
        {
            MessageFilterText = "";
        }
        public override void Clear()
        {
            MessageFilterText = "";
        }
        public override void Reset()
        {
            MessageFilterText = "";
        }

        protected override bool IsFilterMatched(EventItem evt)
        {
            if (MessageFilterText.Any(char.IsUpper))
                return evt.Message.Contains(MessageFilterText);
            else
                return evt.Message.ToLower().Contains(MessageFilterText);
        }
    }
}
