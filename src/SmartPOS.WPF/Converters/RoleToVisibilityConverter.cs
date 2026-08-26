using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SmartPOS.Core.Entities;

namespace SmartPOS.WPF.Converters;

public class RoleToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is UserRole userRole && parameter is string allowedRoles)
        {
            var roles = allowedRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (roles.Contains(userRole.ToString(), StringComparer.OrdinalIgnoreCase))
            {
                return Visibility.Visible;
            }
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
