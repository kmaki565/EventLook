using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

namespace EventLook.View;

// Source: https://shrinandvyas.blogspot.com/2011/07/attached-property-wpf-textbox-clears.html
public class TextBoxBehavior
{
    #region Attached Property EscapeClearsText

    public static readonly DependencyProperty EscapeClearsTextProperty
       = DependencyProperty.RegisterAttached("EscapeClearsText", typeof(bool), typeof(TextBoxBehavior),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnEscapeClearsTextChanged)));

    private static void OnEscapeClearsTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            var textBox = d as TextBox;
            if (textBox != null)
            {
                textBox.KeyUp -= TextBoxKeyUp;
                textBox.KeyUp += TextBoxKeyUp;
            }
        }
    }

    private static void TextBoxKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            ((TextBox)sender).Text = string.Empty;
        }
    }

    public static void SetEscapeClearsText(DependencyObject dependencyObject, bool escapeClearsText)
    {
        if (!ReferenceEquals(null, dependencyObject))
            dependencyObject.SetValue(EscapeClearsTextProperty, escapeClearsText);
    }

    public static bool GetEscapeClearsText(DependencyObject dependencyObject)
    {
        if (!ReferenceEquals(null, dependencyObject))
            return (bool)dependencyObject.GetValue(EscapeClearsTextProperty);
        return false;
    }

    #endregion Attached Property EscapeClearsText

    #region Attached Property HighlightedXml

    // DependencyProperty for HighlightedXml
    public static readonly DependencyProperty HighlightedXmlProperty =
        DependencyProperty.RegisterAttached(
            "HighlightedXml",
            typeof(string),
            typeof(TextBoxBehavior),
            new FrameworkPropertyMetadata(string.Empty, OnHighlightedXmlChanged)
        );

    /// <summary>
    /// Validate string is proper XML format
    /// </summary>
    private static bool IsValidXml(string xml)
    {
        try
        {
            XElement.Parse(xml);
            return true;
        }
        catch (XmlException)
        {
            return false;
        }
    }

    /// <summary>
    /// Sets the HighlightedXml property on a DependencyObject.
    /// </summary>
    public static void SetHighlightedXml(DependencyObject element, string value)
    {
        element.SetValue(HighlightedXmlProperty, value);
    }

    /// <summary>
    /// Gets the HighlightedXml property from a DependencyObject.
    /// </summary>
    public static string GetHighlightedXml(DependencyObject element)
    {
        return (string)element.GetValue(HighlightedXmlProperty);
    }

    /// <summary>
    /// Called when HighlightedXml property changes.
    /// </summary>
    private static void OnHighlightedXmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RichTextBox richTextBox && e.NewValue is string xml)
        {
            // If invalid XML, skip highlight and display string.
            if (!IsValidXml(xml))
            {
                richTextBox.Document.Blocks.Clear();
                richTextBox.AppendText(xml);
                return;
            }

            // If valid XML, proceed to highlight it
            HighlightXml(richTextBox, xml);
        }
    }

    /// <summary>
    /// Highlights XML in a RichTextBox.
    /// </summary>
    private static void HighlightXml(RichTextBox richTextBox, string xml)
    {
        var doc = richTextBox.Document;
        doc.Blocks.Clear(); // Clear existing content

        // Format the XML string with proper indentation
        var formattedXml = FormatXml(xml);

        var paragraph = new Paragraph();
        var xmlDoc = XDocument.Parse(formattedXml);

        // Add the XML declaration
        var declaration = xmlDoc.Declaration;
        if (declaration != null)
        {
            paragraph.Inlines.Add(new Run($"<?xml version=\"{declaration.Version}\" encoding=\"{declaration.Encoding}\"?>") { Foreground = Brushes.SlateGray });
            paragraph.Inlines.Add(new LineBreak());
        }

        // Add XML elements to paragraph
        AddElementToParagraph(paragraph, xmlDoc.Root, 0);

        // Remove the extra line break at the end
        if (paragraph.Inlines.LastInline is LineBreak)
            paragraph.Inlines.Remove(paragraph.Inlines.LastInline);

        doc.Blocks.Add(paragraph);
    }

    /// <summary>
    /// Adds an XElement to a Paragraph with proper formatting.
    /// </summary>
    private static void AddElementToParagraph(Paragraph paragraph, XElement element, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 2);
        paragraph.Inlines.Add(new Run(indent));

        var prefix = element.GetPrefixOfNamespace(element.Name.Namespace);
        var elementName = !string.IsNullOrEmpty(prefix) ? $"{prefix}:{element.Name.LocalName}" : element.Name.LocalName;
        paragraph.Inlines.Add(new Run($"<{elementName}") { Foreground = Brushes.MediumBlue });

        // Add attributes
        foreach (var attribute in element.Attributes())
        {
            paragraph.Inlines.Add(new Run($" {attribute.Name}") { Foreground = Brushes.Firebrick });
            paragraph.Inlines.Add(new Run("=") { Foreground = Brushes.SlateGray });
            paragraph.Inlines.Add(new Run($"\"{attribute.Value}\"") { Foreground = Brushes.ForestGreen });
        }

        if (element.IsEmpty || string.IsNullOrWhiteSpace(element.Value))
        {
            // Self-closing tag
            paragraph.Inlines.Add(new Run(" />") { Foreground = Brushes.MediumBlue });
            paragraph.Inlines.Add(new LineBreak());
        }
        else
        {
            paragraph.Inlines.Add(new Run(">") { Foreground = Brushes.MediumBlue });

            if (!string.IsNullOrWhiteSpace(element.Value) && !element.HasElements)
            {
                // Add element value
                if (element.Value.Contains("\n"))
                {
                    paragraph.Inlines.Add(new LineBreak());
                    paragraph.Inlines.Add(new Run($"{new string(' ', (indentLevel + 1) * 2)}{element.Value}") { Foreground = Brushes.Black });
                    paragraph.Inlines.Add(new LineBreak());
                    paragraph.Inlines.Add(new Run($"{indent}</{elementName}>") { Foreground = Brushes.MediumBlue });
                    paragraph.Inlines.Add(new LineBreak());
                }
                else
                {
                    paragraph.Inlines.Add(new Run(element.Value) { Foreground = Brushes.Black });
                    paragraph.Inlines.Add(new Run($"</{elementName}>") { Foreground = Brushes.MediumBlue });
                    paragraph.Inlines.Add(new LineBreak());
                }
            }
            else
            {
                // Add child elements
                paragraph.Inlines.Add(new LineBreak());
                foreach (var childElement in element.Elements())
                {
                    AddElementToParagraph(paragraph, childElement, indentLevel + 1);
                }
                paragraph.Inlines.Add(new Run($"{indent}</{elementName}>") { Foreground = Brushes.MediumBlue });
                paragraph.Inlines.Add(new LineBreak());
            }
        }
    }

    /// <summary>
    /// Formats the XML string with proper indentation.
    /// </summary>
    private static string FormatXml(string xml)
    {
        var stringBuilder = new System.Text.StringBuilder();
        var element = XElement.Parse(xml);

        using (var writer = System.Xml.XmlWriter.Create(stringBuilder, new System.Xml.XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = false,
            NewLineOnAttributes = false
        }))
        {
            element.Save(writer);
        }

        return stringBuilder.ToString();
    }


    #endregion Attached Property HighlightedXml
}
