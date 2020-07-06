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
            string[] filterlist = {};
            if (MessageFilterText.Any())
            {
                //search fo "|" if exist do OR search
                if(MessageFilterText.Contains('|'))
                {
                    filterlist = MessageFilterText.Split('|');
                    return filterlist.Any(filter => evt.Message.ToLower().Contains(filter.Trim().ToLower()));
                }

                //AND search
                filterlist = MessageFilterText.Split(' ');
                return filterlist.All(filter => evt.Message.ToLower().Contains(filter.ToLower()));
            }

            return false;
        }
    }
}
