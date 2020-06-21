using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace EventLook.View
{
    public class EventLevelToDisplayTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is byte level)
            {
                // In the Event Viewer, Level 0 is shown as Information.
                string levelDisplayName =
                    (level == 1) ? "Critical" :
                    (level == 2) ? "Error" :
                    (level == 3) ? "Warning" :
                    (level == 4) ? "Information" :
                    (level == 5) ? "Verbose" :
                    "Unknown level";

                return $"{level} - {levelDisplayName}";
            }
            else
                throw new ArgumentException("Invalid type object was specified.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // https://stackoverflow.com/a/1039681/5461938
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Doubles the parameter (as original grid length) according to the boolean value.
    /// </summary>
    public class ExpandedToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is bool))
                throw new ArgumentException("The source must be a boolean");

            try
            {
                int origLength = System.Convert.ToInt32(parameter as string);
                return new GridLength((bool)value ? origLength * 2 : origLength);
            }
            catch (Exception)
            {
                throw new ArgumentException("Exception occurred while interpreting parameter.");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
