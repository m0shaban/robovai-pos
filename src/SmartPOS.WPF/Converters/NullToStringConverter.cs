using System;
using System.Globalization;
using System.Windows.Data;

namespace SmartPOS.WPF.Converters;

/// <summary>
/// Converts a null/non-null value to one of two string options.
/// ConverterParameter format: "ValueWhenNotNull|ValueWhenNull"
/// Example: ConverterParameter="تعديل مستخدم|إضافة مستخدم جديد"
/// </summary>
public class NullToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var parts = parameter?.ToString()?.Split('|') ?? Array.Empty<string>();
        var whenNotNull = parts.Length > 0 ? parts[0] : "Edit";
        var whenNull    = parts.Length > 1 ? parts[1] : "New";

        return value == null ? whenNull : whenNotNull;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
