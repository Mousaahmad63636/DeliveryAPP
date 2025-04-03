using System;

namespace ExpressServicePOS.Core.Models
{
    /// <summary>
    /// Represents currency configuration settings for the system.
    /// </summary>
    public class CurrencySetting
    {
        public int Id { get; set; }
        public bool EnableMultipleCurrencies { get; set; } = true;
        public bool EnableUSD { get; set; } = true;
        public bool EnableLBP { get; set; } = true;
        public decimal USDToLBPRate { get; set; } = 90000M;
        public string DefaultCurrency { get; set; } = "USD";
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}