// File: ExpressServicePOS.Core.Models/Customer.cs
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

        /// <summary>
        /// Collection of orders associated with this customer.
        /// </summary>
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        /// <summary>
        /// Collection of monthly subscriptions associated with this customer.
        /// </summary>
        public virtual ICollection<MonthlySubscription> Subscriptions { get; set; } = new List<MonthlySubscription>();
    }
}