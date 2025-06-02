using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ExpressServicePOS.UI.Views
{
    public partial class DashboardPage : Page
    {
        private readonly IServiceScope _serviceScope;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<DashboardPage> _logger;

        public DashboardPage()
        {
            InitializeComponent();

            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();
            _dbContextFactory = _serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<DashboardPage>>();

            Loaded += DashboardPage_Loaded;
            Unloaded += DashboardPage_Unloaded;
        }

        private void DashboardPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _serviceScope?.Dispose();
        }

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    // Load statistics
                    int totalOrders = await dbContext.Orders.CountAsync();
                    int pendingOrders = await dbContext.Orders.CountAsync(o => o.DeliveryStatus == DeliveryStatus.Pending);
                    int todayOrders = await dbContext.Orders.CountAsync(o => o.OrderDate.Date == DateTime.Today.Date);
                    int totalCustomers = await dbContext.Customers.CountAsync();

                    // Update UI
                    txtTotalOrders.Text = totalOrders.ToString();
                    txtPendingOrders.Text = pendingOrders.ToString();
                    txtTodayOrders.Text = todayOrders.ToString();
                    txtTotalCustomers.Text = totalCustomers.ToString();

                    // Load recent orders
                    var recentOrders = await dbContext.Orders
                        .Include(o => o.Customer)
                        .OrderByDescending(o => o.OrderDate)
                        .Take(10)
                        .Select(o => new
                        {
                            o.OrderNumber,
                            CustomerName = o.Customer.Name,
                            o.DriverName,
                            o.OrderDate,
                            DeliveryStatus = o.DeliveryStatus.ToString(),
                            FormattedAmount = o.Currency == "USD" ? $"{o.TotalPrice:N2} $" : $"{o.TotalPrice:N0} ل.ل."
                        })
                        .ToListAsync();

                    dgRecentOrders.ItemsSource = recentOrders;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading dashboard data");
                MessageBox.Show($"حدث خطأ أثناء تحميل بيانات لوحة المعلومات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnNewOrder_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new NewOrderPage());
        }

        private void btnTrackOrder_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new OrderLookupPage());
        }

        private void btnAddCustomer_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomerDialog();
            dialog.ShowDialog();
        }

        private void btnAddDriver_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new DriverDialog();
            dialog.ShowDialog();
        }

        private void btnManageOrders_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new OrdersPage());
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new SettingsPage());
        }
    }
}