// File: ExpressServicePOS.UI.Converters/StatusToButtonLabelConverter.cs
using ExpressServicePOS.Core.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace ExpressServicePOS.UI.Converters
{
    public class StatusToButtonLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DeliveryStatus status)
            {
                return status switch
                {
                    DeliveryStatus.Pending => "قيد الانتظار",
                    DeliveryStatus.PickedUp => "تم الاستلام",
                    DeliveryStatus.InTransit => "قيد التوصيل",
                    DeliveryStatus.Delivered => "تم التسليم",
                    DeliveryStatus.PartialDelivery => "تسليم جزئي",
                    DeliveryStatus.Failed => "فشل التوصيل",
                    DeliveryStatus.Cancelled => "ملغى",
                    _ => "غير معروف"
                };
            }
            return "غير معروف";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}