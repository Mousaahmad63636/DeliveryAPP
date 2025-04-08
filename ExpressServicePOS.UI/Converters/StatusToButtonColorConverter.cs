// File: ExpressServicePOS.UI.Converters/StatusToButtonColorConverter.cs
using ExpressServicePOS.Core.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ExpressServicePOS.UI.Converters
{
    public class StatusToButtonColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DeliveryStatus status)
            {
                return status switch
                {
                    DeliveryStatus.Pending => new SolidColorBrush(Color.FromRgb(52, 152, 219)),    // Blue
                    DeliveryStatus.PickedUp => new SolidColorBrush(Color.FromRgb(155, 89, 182)),   // Purple
                    DeliveryStatus.InTransit => new SolidColorBrush(Color.FromRgb(243, 156, 18)),  // Orange
                    DeliveryStatus.Delivered => new SolidColorBrush(Color.FromRgb(46, 204, 113)),  // Green
                    DeliveryStatus.PartialDelivery => new SolidColorBrush(Color.FromRgb(230, 126, 34)), // Dark Orange
                    DeliveryStatus.Failed => new SolidColorBrush(Color.FromRgb(231, 76, 60)),      // Red
                    DeliveryStatus.Cancelled => new SolidColorBrush(Color.FromRgb(149, 165, 166)), // Gray
                    _ => new SolidColorBrush(Colors.Black)
                };
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}