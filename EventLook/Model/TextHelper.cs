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

    /// <summary>
    /// Performs AND search (case-insensitive).
    /// Return true if target text contains all included words and does not contain any excluded words.
    /// </summary>
    public static bool IsTextMatched(string target, AndSearchMaterial andSearch)
    {
        return andSearch.WordsToInclude.All(x => target.Contains(x, StringComparison.OrdinalIgnoreCase))
            && andSearch.WordsToExclude.All(x => !target.Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Escapes special characters in a CSV value.
    /// </summary>
    public static string EscapeCsvValue(string value)
    {
        if (value.Contains("\""))
        {
            value = value.Replace("\"", "\"\"");
        }
        if (value.Contains(",") || value.Contains("\n") || value.Contains("\r"))
        {
            value = $"\"{value}\"";
        }
        return value;
    }
}

public class AndSearchMaterial
{
    public AndSearchMaterial(string str)
    {
        //TODO: Exclude search with quoted text is not supported (e.g. -"abc def" is treated as "abc def"),
        // and quoting a minus sign is not supported (e.g. "-abc" is treated as "abc").
        IEnumerable<string> searchWords = TextHelper.SplitQuotedText(str);
        WordsToInclude = searchWords.Where(x => !x.StartsWith('-'));
        // If -abc is supplied, abc will be the excluded keyword.
        WordsToExclude = searchWords.Where(x => Regex.IsMatch(x, @"^-\S+")).Select(x => x.Remove(0, 1));
    }
    public IEnumerable<string> WordsToInclude { get; }
    public IEnumerable<string> WordsToExclude { get; }
}
