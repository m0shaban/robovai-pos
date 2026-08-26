using System;
using System.Globalization;
using System.Windows.Data;
using SmartPOS.Core.Entities;

namespace SmartPOS.WPF.Converters;

public class UnitTypeToArabicConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is UnitType unit)
        {
            return unit switch
            {
                UnitType.Piece => "قطعة",
                UnitType.Box => "علبة",
                UnitType.Carton => "كرتونة",
                UnitType.Kilogram => "كيلوجرام",
                UnitType.Liter => "لتر",
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
