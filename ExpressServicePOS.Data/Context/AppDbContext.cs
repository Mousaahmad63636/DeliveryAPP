// File: ExpressServicePOS.Data.Context/AppDbContext.cs
using ExpressServicePOS.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace ExpressServicePOS.Data.Context
{
    /// <summary>
    /// The main database context for the Express Service POS system.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppDbContext"/> class.
        /// </summary>
        /// <param name="options">The options to be used by the context.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            // Ensure database directory exists
            var connectionString = Database.GetConnectionString();
            if (connectionString != null && connectionString.Contains("AttachDbFilename"))
            {
                var startIndex = connectionString.IndexOf("AttachDbFilename=") + "AttachDbFilename=".Length;
                var endIndex = connectionString.IndexOf(';', startIndex);
                if (endIndex == -1) endIndex = connectionString.Length;

                var filePath = connectionString.Substring(startIndex, endIndex - startIndex);
                var directory = Path.GetDirectoryName(filePath);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }

        /// <summary>
        /// Gets or sets the customers dataset.
        /// </summary>
        public DbSet<Customer> Customers { get; set; }

        /// <summary>
        /// Gets or sets the orders dataset.
        /// </summary>
        public DbSet<Order> Orders { get; set; }

        /// <summary>
        /// Gets or sets the drivers dataset.
        /// </summary>
        public DbSet<Driver> Drivers { get; set; }

        /// <summary>
        /// Gets or sets the currency settings dataset.
        /// </summary>
        public DbSet<CurrencySetting> CurrencySettings { get; set; }

        /// <summary>
        /// Gets or sets the company profile dataset.
        /// </summary>
        public DbSet<CompanyProfile> CompanyProfile { get; set; }

        /// <summary>
        /// Gets or sets the receipt templates dataset.
        /// </summary>
        public DbSet<ReceiptTemplate> ReceiptTemplates { get; set; }

        /// <summary>
        /// Gets or sets the monthly subscriptions dataset.
        /// </summary>
        public DbSet<MonthlySubscription> MonthlySubscriptions { get; set; }

        /// <summary>
        /// Gets or sets the subscription payments dataset.
        /// </summary>
        public DbSet<SubscriptionPayment> SubscriptionPayments { get; set; }

        /// <summary>
        /// Optional configuration to suppress warning about dynamic dates in seed data.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            // Suppress the pending model changes warning if needed
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        /// <summary>
        /// Configures the model that was discovered by convention from the entity types.
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Customer entity
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.Phone).HasMaxLength(20);
            });

            // Configure Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(10);
                entity.Property(e => e.DriverName).HasMaxLength(100);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DeliveryFee).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ExchangeRate).HasColumnType("decimal(18,4)");
                entity.Property(e => e.Currency).HasMaxLength(3).HasDefaultValue("USD");

                // Receipt-specific fields
                entity.Property(e => e.RecipientName).HasMaxLength(100);
                entity.Property(e => e.RecipientPhone).HasMaxLength(20);
                entity.Property(e => e.SenderName).HasMaxLength(100);
                entity.Property(e => e.SenderPhone).HasMaxLength(20);
                entity.Property(e => e.PickupLocation).HasMaxLength(200);
                entity.Property(e => e.DeliveryLocation).HasMaxLength(200);
                entity.Property(e => e.PaymentMethod).HasMaxLength(50);
                entity.Property(e => e.IsBreakable).HasDefaultValue(false);
                entity.Property(e => e.IsReplacement).HasDefaultValue(false);
                entity.Property(e => e.IsReturned).HasDefaultValue(false);

                // Configure relationships
                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Orders)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Add optional relationship with Driver
                entity.HasOne(e => e.Driver)
                    .WithMany(d => d.Orders)
                    .HasForeignKey(e => e.DriverId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                // Add relationship with MonthlySubscription
                entity.HasOne(o => o.Subscription)
                    .WithMany()
                    .HasForeignKey(o => o.SubscriptionId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure Driver entity
            modelBuilder.Entity<Driver>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.VehicleType).HasMaxLength(50);
                entity.Property(e => e.VehiclePlateNumber).HasMaxLength(20);
                entity.Property(e => e.AssignedZones).HasMaxLength(200);
            });

            // Configure CurrencySetting entity
            modelBuilder.Entity<CurrencySetting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.USDToLBPRate).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DefaultCurrency).HasMaxLength(3);

                // Add seed data for default currency settings with fixed date
                entity.HasData(
                    new CurrencySetting
                    {
                        Id = 1,
                        EnableMultipleCurrencies = true,
                        EnableUSD = true,
                        EnableLBP = true,
                        USDToLBPRate = 90000M,
                        DefaultCurrency = "USD",
                        LastUpdated = new DateTime(2025, 4, 1) // Fixed date instead of DateTime.Now
                    }
                );
            });

            // Configure CompanyProfile entity
            modelBuilder.Entity<CompanyProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Website).HasMaxLength(100);
                entity.Property(e => e.TaxNumber).HasMaxLength(50);
                entity.Property(e => e.LogoPath).HasMaxLength(500);
                entity.Property(e => e.ReceiptFooterText).HasMaxLength(500);

                // Add seed data for default company profile with fixed date
                entity.HasData(
                    new CompanyProfile
                    {
                        Id = 1,
                        CompanyName = "Express Service",
                        Address = "Beirut, Lebanon",
                        Phone = "",
                        Email = "",
                        Website = "",
                        TaxNumber = "",
                        ReceiptFooterText = "شكراً لاختياركم خدمة إكسبرس",
                        LastUpdated = new DateTime(2025, 4, 1) // Fixed date instead of DateTime.Now
                    }
                );
            });

            // Configure ReceiptTemplate entity
            modelBuilder.Entity<ReceiptTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.HeaderText).HasMaxLength(100);
                entity.Property(e => e.ContactInfo).HasMaxLength(100);
                entity.Property(e => e.FooterText).HasMaxLength(200);
                entity.Property(e => e.LogoPath).HasMaxLength(500);
                entity.Property(e => e.OrderNumberColor).HasMaxLength(20);
                entity.Property(e => e.ReceiptPaperColor).HasMaxLength(20);

                // Add seed data for default receipt template
                entity.HasData(
                    new ReceiptTemplate
                    {
                        Id = 1,
                        HeaderText = "EXPRESS SERVICE TEAM",
                        ContactInfo = "81 169919 - 03 169919",
                        FooterText = "شكراً لاختياركم خدمة إكسبرس",
                        ShowLogo = true,
                        UseColoredOrderNumber = true,
                        OrderNumberColor = "#e74c3c",
                        ReceiptPaperColor = "#d5f5e3",
                        LastUpdated = new DateTime(2025, 4, 1)
                    }
                );
            });

            // Configure MonthlySubscription entity
            modelBuilder.Entity<MonthlySubscription>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Currency).HasMaxLength(3).HasDefaultValue("USD");
                entity.Property(e => e.Notes).HasMaxLength(500);

                // Configure relationship with Customer
                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Subscriptions)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure SubscriptionPayment entity
            modelBuilder.Entity<SubscriptionPayment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PaymentMethod).HasMaxLength(50);
                entity.Property(e => e.Currency).HasMaxLength(3).HasDefaultValue("USD");
                entity.Property(e => e.Notes).HasMaxLength(500);

                // Configure relationship with MonthlySubscription
                entity.HasOne(e => e.Subscription)
                    .WithMany(s => s.Payments)
                    .HasForeignKey(e => e.SubscriptionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}