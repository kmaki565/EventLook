using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EventLook.Model;

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

    public override void Reset()
    {
        MessageFilterText = "";
    }

    protected override bool IsFilterMatched(EventItem evt)
    {
        // First, make text groups for OR search.
        var filterGroups = MessageFilterText.Split('|').Where(x => !string.IsNullOrWhiteSpace(x));
        foreach (var filterText in filterGroups)
        {
            // Then, do AND search for each group. 
            var searchWords = filterText.ToLower().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (searchWords.All(x => evt.Message.ToLower().Contains(x)))
                return true;
        }
        return false;
    }
}
