using System.Windows;

namespace SmartPOS.WPF.Helpers
{
    public static class LocalizationHelper
    {
        public static string GetString(string key, string fallback)
        {
            if (System.Windows.Application.Current != null && System.Windows.Application.Current.Resources.Contains(key))
            {
                var val = System.Windows.Application.Current.Resources[key]?.ToString();
                if (!string.IsNullOrEmpty(val))
                    return val;
            }
            return fallback;
        }
    }
}
