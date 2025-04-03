using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Core.ViewModels;
using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ExpressServicePOS.UI.Views
{
    public partial class OrderLookupPage : Page
    {
        private readonly IServiceScope _serviceScope;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<OrderLookupPage> _logger;

        public OrderLookupPage()
        {
            InitializeComponent();

            // Create service scope
            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();

            // Resolve dependencies
            _dbContext = _serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<OrderLookupPage>>();

            // Register disposal
            Unloaded += OrderLookupPage_Unloaded;
        }

        private void OrderLookupPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _serviceScope?.Dispose();
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string orderNumber = txtOrderNumber.Text.Trim();

                if (string.IsNullOrEmpty(orderNumber))
                {
                    MessageBox.Show("الرجاء إدخال رقم الطلب", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var order = await _dbContext.Orders
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

                if (order == null)
                {
                    MessageBox.Show("لم يتم العثور على الطلب", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Populate order details
                PopulateOrderDetails(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for order");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateOrderDetails(Order order)
        {
            // Populate UI elements with order details
            txtDetailsOrderNumber.Text = order.OrderNumber;
            txtDetailsCustomerName.Text = order.Customer.Name;
            txtDetailsCustomerAddress.Text = order.Customer.Address;

            // Make details panel visible
            orderDetailsPanel.Visibility = Visibility.Visible;
        }
    }
}