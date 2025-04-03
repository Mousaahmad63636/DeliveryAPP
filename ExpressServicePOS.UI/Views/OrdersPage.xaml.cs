// File: ExpressServicePOS.UI.Views/OrdersPage.xaml.cs
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
using System.Windows.Input;

namespace ExpressServicePOS.UI.Views
{
    public partial class OrdersPage : Page
    {
        private readonly IServiceScope _serviceScope;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<OrdersPage> _logger;
        private readonly PrintService _printService;
        private List<OrderViewModel> _orders;

        public OrdersPage()
        {
            InitializeComponent();

            // Create a new service scope
            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();

            // Resolve dependencies from the scope
            _dbContext = _serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<OrdersPage>>();
            _printService = _serviceScope.ServiceProvider.GetRequiredService<PrintService>();

            // Load orders
            LoadOrders();

            // Register the Unloaded event to dispose resources
            Unloaded += OrdersPage_Unloaded;
        }

        private void OrdersPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Dispose the service scope when the page is unloaded
            _serviceScope?.Dispose();
        }

        private async void LoadOrders()
        {
            try
            {
                // Get orders with customer information
                var orders = await _dbContext.Orders
                    .Include(o => o.Customer)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                // Convert to view model
                _orders = orders.Select(o => new OrderViewModel
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.Customer?.Name ?? "غير معروف",
                    OrderDate = o.OrderDate,
                    Status = o.DeliveryStatus,
                    StatusText = GetStatusText(o.DeliveryStatus),
                    Price = o.Price,
                    DeliveryFee = o.DeliveryFee,
                    TotalPrice = o.TotalPrice,
                    IsPaid = o.IsPaid
                }).ToList();

                // Update the DataGrid
                dgOrders.ItemsSource = _orders;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading orders");
                MessageBox.Show($"حدث خطأ أثناء تحميل الطلبات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetStatusText(DeliveryStatus status)
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

        private void btnAddNewOrder_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new NewOrderPage());
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            ApplySearch();
        }

        private void txtSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplySearch();
            }
        }

        private void ApplySearch()
        {
            string searchTerm = txtSearch.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchTerm))
            {
                dgOrders.ItemsSource = _orders;
            }
            else
            {
                var filteredOrders = _orders.Where(o =>
                    o.OrderNumber.ToLower().Contains(searchTerm) ||
                    o.CustomerName.ToLower().Contains(searchTerm) ||
                    GetStatusText(o.Status).ToLower().Contains(searchTerm)
                ).ToList();

                dgOrders.ItemsSource = filteredOrders;
            }
        }

        private void dgOrders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgOrders.SelectedItem is OrderViewModel selectedOrder)
            {
                // Show order details
                MessageBox.Show($"عرض تفاصيل الطلب رقم {selectedOrder.OrderNumber}", "تفاصيل الطلب", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int orderId)
            {
                // Navigate to the edit order page (to be implemented)
                MessageBox.Show($"تعديل الطلب رقم {orderId}", "تعديل الطلب", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int orderId)
            {
                // Confirm deletion
                var result = MessageBox.Show("هل أنت متأكد من حذف هذا الطلب؟", "تأكيد الحذف",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Find the order
                        var order = await _dbContext.Orders.FindAsync(orderId);
                        if (order != null)
                        {
                            // Remove the order
                            _dbContext.Orders.Remove(order);
                            await _dbContext.SaveChangesAsync();

                            // Reload orders
                            LoadOrders();

                            MessageBox.Show("تم حذف الطلب بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error deleting order");
                        MessageBox.Show($"حدث خطأ أثناء حذف الطلب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void btnPrintReceipt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int orderId)
                {
                    // Create Express Service format receipt document exactly matching the image
                    var receiptDocument = await _printService.CreateExpressServiceReceiptAsync(orderId);

                    // Show print preview
                    _printService.ShowPrintPreview(receiptDocument);

                    _logger?.LogInformation($"Print Express Service receipt requested for order ID: {orderId}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error printing receipt");
                MessageBox.Show($"حدث خطأ أثناء طباعة الإيصال: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // View Model for Orders
    public class OrderViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public DeliveryStatus Status { get; set; }
        public string StatusText { get; set; }
        public decimal Price { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsPaid { get; set; }
    }
}