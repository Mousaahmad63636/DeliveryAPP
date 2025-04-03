using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace ExpressServicePOS.Data
{
    /// <summary>
    /// Provides functionality for initializing the database.
    /// </summary>
    public class DbInitializer
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DbInitializer> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbInitializer"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="logger">The logger.</param>
        public DbInitializer(AppDbContext context, ILogger<DbInitializer> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes the database by ensuring it is created and migrated to the latest version.
        /// </summary>
        public void Initialize()
        {
            try
            {
                _logger.LogInformation("Initializing database...");

                // Apply pending migrations
                _context.Database.Migrate();

                // Seed test data if needed
                if (!_context.Customers.Any())
                {
                    SeedTestData();
                }

                _logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }
        }

        /// <summary>
        /// Seeds the database with test data for testing purposes.
        /// </summary>
        private void SeedTestData()
        {
            _logger.LogInformation("Seeding test data...");

            // Add test customers
            var customer1 = new Customer
            {
                Name = "محمد أحمد",
                Address = "الرياض - حي النزهة",
                Phone = "0501234567",
                Notes = "عميل منتظم"
            };

            var customer2 = new Customer
            {
                Name = "سارة خالد",
                Address = "جدة - حي الروضة",
                Phone = "0567891234",
                Notes = "شركة توصيل"
            };

            _context.Customers.AddRange(customer1, customer2);
            _context.SaveChanges();

            var order1 = new Order
            {
                OrderNumber = "0226",
                CustomerId = customer1.Id,
                DriverName = "أحمد محمد",
                OrderDescription = "طلب توصيل مستندات",
                Price = 150.00m,
                DeliveryFee = 30.00m,
                OrderDate = DateTime.Now.AddDays(-3),
                DeliveryDate = DateTime.Now.AddDays(-2),
                DeliveryStatus = DeliveryStatus.Delivered,
                Notes = "تم التسليم بنجاح",
                IsPaid = true
            };

            var order2 = new Order
            {
                OrderNumber = "0227",
                CustomerId = customer2.Id,
                DriverName = "خالد عبدالله",
                OrderDescription = "طلب شحن طرد",
                Price = 200.00m,
                DeliveryFee = 40.00m,
                OrderDate = DateTime.Now.AddDays(-1),
                DeliveryStatus = DeliveryStatus.InTransit,
                Notes = "قيد التوصيل",
                IsPaid = false
            };

            _context.Orders.AddRange(order1, order2);
            _context.SaveChanges();

            _logger.LogInformation("Test data seeding completed.");
        }
    }
}