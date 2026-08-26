using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SmartPOS.WPF.Converters;

public class DifferenceToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal diff)
        {
            if (diff < 0) return new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Red #DC2626
            if (diff > 0) return new SolidColorBrush(Color.FromRgb(22, 163, 74));  // Green #16A34A
            return new SolidColorBrush(Color.FromRgb(75, 85, 99)); // Gray #4B5563
        }
        return new SolidColorBrush(Color.FromRgb(75, 85, 99));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
