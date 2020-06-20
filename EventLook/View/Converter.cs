using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
}
