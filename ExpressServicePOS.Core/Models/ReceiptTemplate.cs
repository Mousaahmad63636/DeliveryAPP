// File: ExpressServicePOS.Core.Models/ReceiptTemplate.cs
using System;

namespace ExpressServicePOS.Core.Models
{
    /// <summary>
    /// Represents the receipt template and formatting information for orders.
    /// </summary>
    public class ReceiptTemplate
    {
        public int Id { get; set; }
        public string HeaderText { get; set; } = "EXPRESS SERVICE TEAM";
        public string ContactInfo { get; set; } = "81 / 616919 - 03 / 616919";
        public string Address { get; set; } = "طريق المطار - نزلة عين الدولة";
        public string FooterText { get; set; } = "نحن غير مسؤولين عن محتوى الطرد وعن قانونية البضاعة الموجودة داخله";
        public string LogoPath { get; set; } = string.Empty;
        public bool ShowLogo { get; set; } = true;
        public bool UseColoredOrderNumber { get; set; } = true;
        public string OrderNumberColor { get; set; } = "#e74c3c"; // Red color by default
        public string ReceiptPaperColor { get; set; } = "#ffffff"; // White color by default
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}