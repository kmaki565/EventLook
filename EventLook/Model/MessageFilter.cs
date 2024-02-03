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
    private readonly List<AndSearchMaterial> andSearchGroups;
    public MessageFilter()
    {
        andSearchGroups = new List<AndSearchMaterial>();
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

            andSearchGroups.Clear();
            // Make text groups for OR search, then build AND-search materials for each group.
            foreach (var groupText in messageFilterText.Split('|').Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                andSearchGroups.Add(new AndSearchMaterial(groupText));
            }

            Apply();
        }
    }

    public override void Refresh(IEnumerable<EventItem> events, bool reset)
    {
        if (reset)
            Clear();
    }
    public override void Clear()
    {
        MessageFilterText = "";
    }

    protected override bool IsFilterMatched(EventItem evt)
    {
        if (MessageFilterText == "")
            return true;

        // If any of groups is matched, return true (= OR search).
        return andSearchGroups.Any(x => TextHelper.IsTextMatched(evt.Message, x));
    }
}
