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

        protected override void DoFilter(object sender, FilterEventArgs e)
        {
            //TODO: Could use the Base class method

            // Set false if the event does not match any checked items in the CheckComboBox
            if (!(e.Item is EventItem evt))
            {
                e.Accepted = false;
                return;
            }

            if (MessageFilterText.Any(char.IsUpper))
            {
                if (!evt.Message.Contains(MessageFilterText))
                    e.Accepted = false;
            }
            else
            {
                if (!evt.Message.ToLower().Contains(MessageFilterText))
                    e.Accepted = false;
            }
        }
    }
}
