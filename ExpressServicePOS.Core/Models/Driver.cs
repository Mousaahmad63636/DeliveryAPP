using System;
using System.Collections.Generic;

namespace ExpressServicePOS.Core.Models
{
    /// <summary>
    /// Represents a delivery driver in the Express Service system.
    /// </summary>
    public class Driver
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Phone { get; set; }
        public string Email { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string VehiclePlateNumber { get; set; } = string.Empty;
        public string AssignedZones { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime DateHired { get; set; } = DateTime.Now;
        public string Notes { get; set; } = string.Empty;

        // Navigation property for orders assigned to this driver
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}