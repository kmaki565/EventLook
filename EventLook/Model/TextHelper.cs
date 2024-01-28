using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace EventLook.Model;

public static class TextHelper
{
    public static string FormatXml(string xmlString)
    {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xmlString);

        StringBuilder sb = new StringBuilder();
        System.IO.TextWriter tr = new System.IO.StringWriter(sb);
        XmlTextWriter wr = new XmlTextWriter(tr);
        
        wr.Formatting = Formatting.Indented;
        doc.Save(wr);
        wr.Close();

        return sb.ToString();
    }

    /// <summary>
    /// Splits a string into tokens, respecting quoted text.
    /// Performs normal split by a space character when a double quote is not in the string, or just one double quote is in it.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static IEnumerable<string> SplitQuotedText(string input)
    {
        // Referred to https://stackoverflow.com/a/5227134/5461938
        return input.Count(x => x == '"') >= 2
            ? Regex.Matches(input, "(?<match>[^\\s\"]+)|\"(?<match>[^\"]*)\"")
                   .Cast<Match>()
                   .Select(m => m.Groups["match"].Value)
                   .Where(x => x != "")
            : input.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }

    public static bool IsWordToExclude(string word)
    {
        return word.StartsWith('-') && !word.Contains(' ');
    }
}
