using System;
using ExpressServicePOS.Core.Models;

namespace ExpressServicePOS.Core.ViewModels
{
    public class CustomerStatementViewModel
    {
        public required string OrderNumber { get; set; }
        public required string OrderDescription { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public DeliveryStatus Status { get; set; }
        public required string CustomerName { get; set; }
        public required string CustomerPhone { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string DeliveryNotes { get; set; } = string.Empty;
    }

    public class OrderLookupViewModel
    {
        public int OrderId { get; set; }
        public required string OrderNumber { get; set; }
        public required string CustomerName { get; set; }
        public required string CustomerAddress { get; set; }
        public required string OrderDescription { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public DeliveryStatus Status { get; set; }
        public string DeliveryNotes { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }
}