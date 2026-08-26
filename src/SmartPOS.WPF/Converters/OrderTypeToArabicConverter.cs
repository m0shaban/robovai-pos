using System;
using System.Globalization;
using System.Windows.Data;
using SmartPOS.Core.Entities;

namespace SmartPOS.WPF.Converters;

public class OrderTypeToArabicConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is OrderType orderType)
        {
            return orderType switch
            {
                OrderType.DineIn => "صالة",
                OrderType.Takeaway => "تيك أواي",
                OrderType.Delivery => "دليفري",
                _ => "غير معروف"
            };
        }
        return "غير معروف";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
