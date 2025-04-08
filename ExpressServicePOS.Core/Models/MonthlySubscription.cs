// File: ExpressServicePOS.Core.Models/MonthlySubscription.cs
using System;
using System.Collections.Generic;

namespace ExpressServicePOS.Core.Models
{
    /// <summary>
    /// Represents a monthly subscription for a customer in the Express Service system.
    /// </summary>
    public class MonthlySubscription
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }
        public decimal Amount { get; set; }
        public int DayOfMonth { get; set; } // Day of the month when payment is due
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string Notes { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD"; // Default currency (USD or LBP)

        // Navigation property for payments related to this subscription
        public virtual ICollection<SubscriptionPayment> Payments { get; set; } = new List<SubscriptionPayment>();
    }
}