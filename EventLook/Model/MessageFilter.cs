using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

    public override void Refresh(IEnumerable<EventItem> events, bool carryOver)
    {
        if (!carryOver)
            Reset();
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
            // Then, do AND search (case-insensitive) for each group. 
            IEnumerable<string> searchWords = SplitQuotedText(filterText.ToLower());
            if (searchWords.All(x => evt.Message.ToLower().Contains(x)))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Splits a string into tokens, respecting quoted text.
    /// Performs normal split by a space character when a double quote is not in the string, or just one double quote is in it.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static IEnumerable<string> SplitQuotedText(string input)
    {
        // Referred to https://stackoverflow.com/a/5227134/5461938
        return input.Count(x => x == '"') >= 2
            ? Regex.Matches(input, "(?<match>[^\\s\"]+)|\"(?<match>[^\"]*)\"")
                   .Cast<Match>()
                   .Select(m => m.Groups["match"].Value)
                   .Where(x => x != "")
            : input.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }
}
