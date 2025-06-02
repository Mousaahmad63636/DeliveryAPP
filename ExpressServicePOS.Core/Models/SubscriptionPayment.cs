// ExpressServicePOS.Core/Models/SubscriptionPayment.cs
using System;

namespace ExpressServicePOS.Core.Models
{
    public class SubscriptionPayment
    {
        public int Id { get; set; }
        public int SubscriptionId { get; set; }
        public virtual MonthlySubscription? Subscription { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime PeriodStartDate { get; set; } // Start of the period covered by this payment
        public DateTime PeriodEndDate { get; set; }   // End of the period covered by this payment
        public string PaymentMethod { get; set; } = "نقدي"; // Cash by default
        public string Currency { get; set; } = "USD"; // Default currency (USD or LBP)
        public string Notes { get; set; } = string.Empty;
    }
}