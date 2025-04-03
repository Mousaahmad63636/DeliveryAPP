using System;

namespace ExpressServicePOS.Core.Models
{
    /// <summary>
    /// Stores company information for receipts, reports, and branding.
    /// </summary>
    public class CompanyProfile
    {
        public int Id { get; set; }
        public required string CompanyName { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string LogoPath { get; set; } = string.Empty;
        public string ReceiptFooterText { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}