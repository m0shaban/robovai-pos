using System;
using System.Globalization;
using System.Windows.Data;

namespace SmartPOS.WPF.Converters;

public class EqualityToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Equals(value, parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
        {
            return parameter;
        }
        return Binding.DoNothing;
    }
}
