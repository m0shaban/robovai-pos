using System;
using System.Globalization;
using System.Windows.Data;

namespace SmartPOS.WPF.Converters;

public class BoolToStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive ? "نشط" : "غير نشط";
        }
        return "غير معروف";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
