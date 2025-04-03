// File: ExpressServicePOS.Core.Models/Order.cs
using System;

namespace ExpressServicePOS.Core.Models
{
    public class Order
    {
        public int Id { get; set; }
        public required string OrderNumber { get; set; }
        public int CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        // Add Driver relationship
        public int? DriverId { get; set; }
        public virtual Driver? Driver { get; set; }

        public required string DriverName { get; set; }
        public required string OrderDescription { get; set; }
        public decimal Price { get; set; }
        public decimal DeliveryFee { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public DeliveryStatus DeliveryStatus { get; set; }
        public string DeliveryNotes { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsPaid { get; set; }

        // Add currency properties
        public string Currency { get; set; } = "USD"; // Default currency (USD or LBP)
        public decimal ExchangeRate { get; set; } = 1; // Exchange rate at the time of order creation

        // Receipt-specific fields
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderPhone { get; set; } = string.Empty;
        public string PickupLocation { get; set; } = string.Empty;
        public string DeliveryLocation { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "نقدي"; // Cash by default

        // Additional receipt checkboxes
        public bool IsBreakable { get; set; } = false;
        public bool IsReplacement { get; set; } = false;
        public bool IsReturned { get; set; } = false;

        // Computed properties
        public decimal TotalPrice => Price + DeliveryFee;

        // Get price in specific currency
        public decimal GetPriceInCurrency(string targetCurrency, decimal currentRate)
        {
            if (Currency == targetCurrency)
                return Price;

            if (Currency == "USD" && targetCurrency == "LBP")
                return Price * (currentRate / ExchangeRate);

            if (Currency == "LBP" && targetCurrency == "USD")
                return Price / (currentRate / ExchangeRate);

            return Price; // Default fallback
        }

        // Get total price in specific currency
        public decimal GetTotalPriceInCurrency(string targetCurrency, decimal currentRate)
        {
            if (Currency == targetCurrency)
                return TotalPrice;

            if (Currency == "USD" && targetCurrency == "LBP")
                return TotalPrice * (currentRate / ExchangeRate);

            if (Currency == "LBP" && targetCurrency == "USD")
                return TotalPrice / (currentRate / ExchangeRate);

            return TotalPrice; // Default fallback
        }
    }

    // Enhanced Delivery Status Enum
    public enum DeliveryStatus
    {
        Pending = 0,           // طلب جديد
        PickedUp = 1,          // تم استلام الطلب
        InTransit = 2,         // قيد التوصيل
        Delivered = 3,         // تم التسليم
        PartialDelivery = 4,   // تسليم جزئي
        Failed = 5,            // فشل التوصيل
        Cancelled = 6          // ملغى
    }
}