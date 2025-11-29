using System.Windows;
using System.Windows.Controls;

namespace EventLook.Extensions;

internal static class TextBoxExtensions
{
    extension(TextBox textBox)
    {
        // https://learn.microsoft.com/en-us/answers/questions/1806319/how-to-remove-the-clear-button-at-the-end-of-the-t
        public void DeleteFluentClearButton()
        {
            var childButton = textBox.FindChildElementByName("DeleteButton");
            var parentGrid = (childButton as Control)?.Parent;
            if (parentGrid != null)
            {
                if (childButton is UIElement childButtonElement)
                {
                    (parentGrid as Grid)?.Children.Remove(childButtonElement);
                }
            }
        }
    }
}