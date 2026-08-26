using System;
using System.Globalization;
using System.Windows.Data;
using SmartPOS.Core.Entities;

namespace SmartPOS.WPF.Converters;

public class StockToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Product product)
        {
            if (product.Stock <= product.MinStockLevel)
                return "نفذت الكمية";
            else if (product.Stock <= product.MinStockLevel * 1.5)
                return "كمية قليلة";
            else
                return "متوفر";
        }
        return "غير معروف";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
