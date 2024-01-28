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

        // First, make text groups for OR search.
        var filterGroups = MessageFilterText.Split('|').Where(x => !string.IsNullOrWhiteSpace(x));
        foreach (var filterText in filterGroups)
        {
            // Then, do AND search (case-insensitive) for each group. 
            //TODO: Do just once to build search words to improve performance.
            IEnumerable<string> searchWords = TextHelper.SplitQuotedText(filterText.ToLower());
            List<string> includedWords = searchWords.Where(x => !TextHelper.IsWordToExclude(x)).ToList();
            List<string> excludedWords = searchWords.Where(x => TextHelper.IsWordToExclude(x)).Select(x => x.Remove(0, 1)).ToList();
            string target = evt.Message.ToLower();
            if (excludedWords.All(x => !target.Contains(x)) && includedWords.All(x => target.Contains(x)))
                return true;
        }
        return false;
    }
}
