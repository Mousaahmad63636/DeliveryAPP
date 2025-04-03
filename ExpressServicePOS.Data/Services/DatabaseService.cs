using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ExpressServicePOS.Data.Services
{
    public class DatabaseService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(AppDbContext context, ILogger<DatabaseService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Initializing database...");

                // Ensure database is created and up to date
                await _context.Database.EnsureCreatedAsync();
                await MigrateIfNeededAsync();

                // Seed initial data if no records exist
                await SeedInitialDataIfEmptyAsync();

                _logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during database initialization");
                throw new InvalidOperationException("فشل تهيئة قاعدة البيانات", ex);
            }
        }

        private async Task MigrateIfNeededAsync()
        {
            try
            {
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation($"Applying {pendingMigrations.Count()} pending migrations");
                    await _context.Database.MigrateAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying database migrations");
                throw;
            }
        }

        private async Task SeedInitialDataIfEmptyAsync()
        {
            try
            {
                // Check if database is empty
                if (!await _context.Customers.AnyAsync())
                {
                    await SeedCustomersAsync();
                }

                if (!await _context.Orders.AnyAsync())
                {
                    await SeedOrdersAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding initial database data");
                throw;
            }
        }

        private async Task SeedCustomersAsync()
        {
            var customers = new[]
            {
                new Customer
                {
                    Name = "محمد أحمد",
                    Address = "الرياض - حي النزهة",
                    Phone = "0501234567",
                    Notes = "عميل منتظم"
                },
                new Customer
                {
                    Name = "سارة خالد",
                    Address = "جدة - حي الروضة",
                    Phone = "0567891234",
                    Notes = "شركة توصيل"
                }
            };

            _context.Customers.AddRange(customers);
            await _context.SaveChangesAsync();
        }

        private async Task SeedOrdersAsync()
        {
            var customers = await _context.Customers.ToListAsync();

            var orders = new[]
            {
                new Order
                {
                    OrderNumber = "0226",
                    CustomerId = customers[0].Id,
                    DriverName = "أحمد محمد",
                    OrderDescription = "طلب توصيل مستندات",
                    Price = 150.00m,
                    DeliveryFee = 30.00m,
                    OrderDate = DateTime.Now.AddDays(-3),
                    DeliveryDate = DateTime.Now.AddDays(-2),
                    DeliveryStatus = DeliveryStatus.Delivered,
                    Notes = "تم التسليم بنجاح",
                    IsPaid = true
                },
                new Order
                {
                    OrderNumber = "0227",
                    CustomerId = customers[1].Id,
                    DriverName = "خالد عبدالله",
                    OrderDescription = "طلب شحن طرد",
                    Price = 200.00m,
                    DeliveryFee = 40.00m,
                    OrderDate = DateTime.Now.AddDays(-1),
                    DeliveryStatus = DeliveryStatus.InTransit,
                    Notes = "قيد التوصيل",
                    IsPaid = false
                }
            };

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();
        }
    }
}