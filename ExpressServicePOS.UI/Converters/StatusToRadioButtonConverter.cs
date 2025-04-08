// File: ExpressServicePOS.UI.Converters/StatusToRadioButtonConverter.cs
using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace ExpressServicePOS.UI.Converters
{
    public class StatusToRadioButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Handle null control safely
            if (parameter == null)
                return false;

            // Check if the value matches the parameter
            bool isActive = (value is bool b) && b;
            string paramValue = parameter.ToString();

            return (isActive && paramValue == "Active") || (!isActive && paramValue == "Inactive");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Only convert when this is the selected radio button
            if (value is bool isSelected && isSelected && parameter != null)
            {
                return parameter.ToString() == "Active";
            }

            return null;
        }
    }
}