using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ExpressServicePOS.Data.Services
{
    public class DatabaseService : BaseService
    {
        public DatabaseService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<DatabaseService> logger)
            : base(dbContextFactory, logger)
        {
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                Logger.LogInformation("Initializing database...");

                await ExecuteDbOperationAsync(async (dbContext) =>
                {
                    try
                    {
                        // Choose one approach: either use migrations or EnsureCreated
                        // For a simple app, EnsureCreated is often sufficient
                        await dbContext.Database.EnsureCreatedAsync();

                        // Don't call MigrateIfNeededAsync if using EnsureCreated
                        // await MigrateIfNeededAsync(dbContext);

                        // Seed initial data if needed
                        await SeedInitialDataIfEmptyAsync(dbContext);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Non-critical error during database initialization");
                        // Don't rethrow - allow app to continue
                    }
                });

                Logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Critical error during database initialization");
                throw new InvalidOperationException("فشل تهيئة قاعدة البيانات", ex);
            }
        }

        private async Task MigrateIfNeededAsync(AppDbContext dbContext)
        {
            try
            {
                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    Logger.LogInformation($"Applying {pendingMigrations.Count()} pending migrations");
                    await dbContext.Database.MigrateAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error applying database migrations");
                throw;
            }
        }

        private async Task SeedInitialDataIfEmptyAsync(AppDbContext dbContext)
        {
            try
            {
                // Check if database is empty
                if (!await dbContext.Customers.AnyAsync())
                {
                    await SeedCustomersAsync(dbContext);
                }

                if (!await dbContext.Orders.AnyAsync())
                {
                    await SeedOrdersAsync(dbContext);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error seeding initial database data");
                throw;
            }
        }

        private async Task SeedCustomersAsync(AppDbContext dbContext)
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

            dbContext.Customers.AddRange(customers);
            await dbContext.SaveChangesAsync();
        }

        private async Task SeedOrdersAsync(AppDbContext dbContext)
        {
            var customers = await dbContext.Customers.ToListAsync();

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

            dbContext.Orders.AddRange(orders);
            await dbContext.SaveChangesAsync();
        }
    }
}