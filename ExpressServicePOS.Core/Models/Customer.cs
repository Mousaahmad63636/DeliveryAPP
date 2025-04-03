using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace ExpressServicePOS.Core.Models
{
    /// <summary>
    /// Represents a customer in the Express Service system.
    /// </summary>
    public class Customer
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Address { get; set; }
        public required string Phone { get; set; }
        public required string Notes { get; set; }
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}