using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace EventLook.View;

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

            return $"{levelDisplayName}";
        }
        else
            return "Invalid level";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class EventLevelToImageConverter : IValueConverter
{
    public string CriticalIconPath { get; set; }
    public string ErrorIconPath { get; set; }
    public string WarningIconPath { get; set; }
    public string InformationIconPath { get; set; }
    public string VerboseIconPath { get; set; }
    public string UnknownIconPath { get; set; }
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte level)
        {
            // In the Event Viewer, Level 0 is shown as Information.
            string iconPath =
                (level == 1) ? CriticalIconPath :
                (level == 2) ? ErrorIconPath :
                (level == 3) ? WarningIconPath :
                (level == 4) ? InformationIconPath :
                (level == 5) ? VerboseIconPath :
                UnknownIconPath;

            return new BitmapImage(new Uri(iconPath, UriKind.Relative));
        }
        else
            return new BitmapImage();
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
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    private BooleanToVisibilityConverter _converter = new BooleanToVisibilityConverter();

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        var result = _converter.Convert(value, targetType, parameter, culture) as Visibility?;
        return result == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        var result = _converter.ConvertBack(value, targetType, parameter, culture) as bool?;
        return result == true ? false : true;
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

public class UtcToAnotherTimeZoneStringConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (values[0] is DateTime utcTime && values[1] is TimeZoneInfo timeZone && values[2] is bool showsMillisec)
        {
            DateTime time = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZone);
            return showsMillisec ? time.ToString("yyyy-MM-dd HH:mm:ss.fff") : time.ToString("yyyy-MM-dd HH:mm:ss");
        }
        else
            throw new ArgumentException("Arguments must be 1. UTC time, 2. TimeZoneInfo and 3. boolean for whether to show milliseconds in time of event.");
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
/// <summary>
/// Returns shorthand text of time offset from UTC. For example, JST will be "+09:00".
/// Note the actual time offset could be different due to daylight saving time. 
/// </summary>
public class TimeZoneToOffsetTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeZoneInfo tzi)
        {
            TimeSpan ts = tzi.BaseUtcOffset;
            return $"{(ts >= TimeSpan.Zero ? "+" : "-")}{ts:hh\\:mm}";
        }
        else
            throw new ArgumentException("Argument must be a TimeZoneInfo.");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Generates the status text based on the various indicators.
/// 0: IsUpdating, 1: IsAutoRefreshing, 2: IsAppend, 3: LoadedEventCount, 4: AppendCount, 5: LastElapsedTime, 6: ErrorMessage
/// </summary>
public class StatusTextConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is bool isUpdating && values[1] is bool isAutoRefreshing && values[2] is bool isAppend
            && values[3] is int loadedEventCount && values[4] is int appendCount
            && values[5] is TimeSpan lastEpasedTime && values[6] is string errorMessage)
        {
            if (isUpdating)
                return $"Loading {loadedEventCount} events... {errorMessage}";
            else if (isAutoRefreshing)
                return $"{loadedEventCount} events loaded. Waiting for new events... {errorMessage}";
            else
                return isAppend
                    ? $"{loadedEventCount} ({appendCount} new) events loaded. {errorMessage}"
                    : $"{loadedEventCount} events loaded. ({lastEpasedTime.TotalSeconds:F1} sec) {errorMessage}";  // 1 digit after decimal point
        }
        else
            throw new ArgumentException("Arguments not matched to this converter.");
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
/// <summary>
/// Generates the filter info text based on the various indicators.
/// 0: IsUpdating, 1: AreEventsFiltered, 2: VisibleEventCount
/// </summary>
public class FilterInfoTextConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is bool isUpdating && values[1] is bool areEventsFiltered && values[2] is int visibleEventCount)
        {
            if (isUpdating)
                return "";
            else
                return areEventsFiltered ? $" {visibleEventCount} events matched to the filter(s)." : "";
        }
        else
            throw new ArgumentException("Arguments not matched to this converter.");
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}