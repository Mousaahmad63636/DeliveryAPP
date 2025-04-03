using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using ExpressServicePOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ExpressServicePOS.UI.Views
{
    public partial class DashboardPage : Page
    {
        private readonly IServiceScope _serviceScope;
        private readonly AppDbContext _dbContext;
        private readonly CurrencyService _currencyService;
        private readonly ILogger<DashboardPage> _logger;
        private DispatcherTimer _refreshTimer;

        public DashboardPage()
        {
            InitializeComponent();

            // Create a new service scope
            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();

            // Resolve dependencies from the scope
            _dbContext = _serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
            _currencyService = _serviceScope.ServiceProvider.GetRequiredService<CurrencyService>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<DashboardPage>>();

            // Load dashboard data
            LoadDashboardData();

            // Setup refresh timer (refresh every 5 minutes)
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromMinutes(5);
            _refreshTimer.Tick += (s, e) => LoadDashboardData();
            _refreshTimer.Start();

            // Register unload event
            Unloaded += DashboardPage_Unloaded;
        }

        private void DashboardPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _refreshTimer?.Stop();
            _serviceScope?.Dispose();
        }

        private async void LoadDashboardData()
        {
            try
            {
                // Get today's date
                DateTime today = DateTime.Today;
                DateTime tomorrow = today.AddDays(1);

                // Get exchange rate
                decimal exchangeRate = 90000M; // Default
                try
                {
                    var currencySettings = await _currencyService.GetSettingsAsync();
                    exchangeRate = currencySettings.USDToLBPRate;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load currency settings. Using default rate.");
                }

                // Load orders for today
                var todayOrders = await _dbContext.Orders
                    .Where(o => o.OrderDate >= today && o.OrderDate < tomorrow)
                    .ToListAsync();

                // Update today's orders count
                txtOrdersToday.Text = todayOrders.Count.ToString();

                // Orders in transit
                var inTransitOrders = await _dbContext.Orders
                    .Where(o => o.DeliveryStatus == DeliveryStatus.InTransit)
                    .CountAsync();
                txtOrdersInTransit.Text = inTransitOrders.ToString();

                // Calculate today's revenue
                decimal usdRevenue = todayOrders.Where(o => o.Currency == "USD").Sum(o => o.TotalPrice);
                decimal lbpRevenue = todayOrders.Where(o => o.Currency == "LBP").Sum(o => o.TotalPrice);

                // Convert LBP to USD for combined total
                decimal totalRevenueUSD = usdRevenue + (lbpRevenue / exchangeRate);
                txtRevenue.Text = $"{totalRevenueUSD:N2} $ ({lbpRevenue:N0} ل.ل)";

                // Load recent orders - Using static methods to prevent memory leaks
                var recentOrders = await _dbContext.Orders
                    .Include(o => o.Customer)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .Select(o => new RecentOrderViewModel
                    {
                        OrderNumber = o.OrderNumber,
                        CustomerName = o.Customer.Name,
                        Description = o.OrderDescription,
                        Date = o.OrderDate,
                        Status = GetStatusTextStatic(o.DeliveryStatus),
                        Amount = FormatAmountStatic(o.TotalPrice, o.Currency)
                    })
                    .ToListAsync();

                lvRecentOrders.ItemsSource = recentOrders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                MessageBox.Show($"حدث خطأ أثناء تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Fix #1: Changed to static method to prevent memory leak
        private static string GetStatusTextStatic(DeliveryStatus status)
        {
            return status switch
            {
                DeliveryStatus.Pending => "قيد الانتظار",
                DeliveryStatus.PickedUp => "تم الاستلام",
                DeliveryStatus.InTransit => "قيد التوصيل",
                DeliveryStatus.Delivered => "تم التسليم",
                DeliveryStatus.PartialDelivery => "تسليم جزئي",
                DeliveryStatus.Failed => "فشل التوصيل",
                DeliveryStatus.Cancelled => "ملغي",
                _ => "غير معروف"
            };
        }

        // Fix #2: Changed to static method to prevent memory leak
        private static string FormatAmountStatic(decimal amount, string currency)
        {
            return currency == "USD"
                ? $"{amount:N2} $"
                : $"{amount:N0} ل.ل";
        }

        // Keep instance methods for backward compatibility
        private string GetStatusText(DeliveryStatus status)
        {
            return GetStatusTextStatic(status);
        }

        private string FormatAmount(decimal amount, string currency)
        {
            return FormatAmountStatic(amount, currency);
        }
    }

    public class RecentOrderViewModel
    {
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public string Amount { get; set; }
    }
}