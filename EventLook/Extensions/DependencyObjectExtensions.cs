using System.Windows;
using System.Windows.Media;

namespace EventLook.Extensions;

internal static class DependencyObjectExtensions
{
    extension(DependencyObject tree)
    {
        // https://learn.microsoft.com/en-us/answers/questions/1806319/how-to-remove-the-clear-button-at-the-end-of-the-t
        public DependencyObject FindChildElementByName(string sName)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(tree); i++)
            {
                var child = VisualTreeHelper.GetChild(tree, i);

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (child != null && (child as FrameworkElement)?.Name == sName)
                    return child;

                var childInSubtree = FindChildElementByName(child, sName);
                if (childInSubtree != null)
                    return childInSubtree;
            }

            return null;
        }
    }
}