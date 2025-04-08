// File: ExpressServicePOS.UI.Converters/BoolToActiveConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace ExpressServicePOS.UI.Converters
{
    public class BoolToActiveConverter : IValueConverter
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
            if (value is string status)
            {
                return status == "نشط";
            }
            return false;
        }
    }
}