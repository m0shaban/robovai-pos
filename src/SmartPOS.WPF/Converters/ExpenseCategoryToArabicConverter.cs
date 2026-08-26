using System;
using System.Globalization;
using System.Windows.Data;
using SmartPOS.Core.Entities;

namespace SmartPOS.WPF.Converters;

public class ExpenseCategoryToArabicConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ExpenseCategory category)
        {
            return category switch
            {
                ExpenseCategory.Rent => "إيجار",
                ExpenseCategory.Utilities => "فواتير",
                ExpenseCategory.Salaries => "رواتب",
                ExpenseCategory.Supplies => "مستلزمات",
                ExpenseCategory.Maintenance => "صيانة",
                ExpenseCategory.Marketing => "تسويق",
                ExpenseCategory.Transportation => "مواصلات",
                ExpenseCategory.Other => "أخرى",
                _ => "غير معروف"
            };
        }
        return "غير معروف";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string arabicText)
        {
            return arabicText switch
            {
                "إيجار" => ExpenseCategory.Rent,
                "فواتير" => ExpenseCategory.Utilities,
                "رواتب" => ExpenseCategory.Salaries,
                "مستلزمات" => ExpenseCategory.Supplies,
                "صيانة" => ExpenseCategory.Maintenance,
                "تسويق" => ExpenseCategory.Marketing,
                "مواصلات" => ExpenseCategory.Transportation,
                _ => ExpenseCategory.Other
            };
        }
        return ExpenseCategory.Other;
    }
}
