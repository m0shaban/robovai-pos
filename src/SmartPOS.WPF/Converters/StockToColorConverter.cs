using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SmartPOS.Core.Entities;

namespace SmartPOS.WPF.Converters;

public class StockToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Product product)
        {
            if (product.Stock <= product.MinStockLevel)
                return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
            else if (product.Stock <= product.MinStockLevel * 1.5)
                return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
            else
                return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
