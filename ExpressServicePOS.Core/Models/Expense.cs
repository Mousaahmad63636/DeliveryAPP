using System;

namespace ExpressServicePOS.Core.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public required string Description { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "نقدي"; // Cash by default
        public string Currency { get; set; } = "USD"; // Default currency (USD or LBP)
    }
}