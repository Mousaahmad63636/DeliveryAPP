// File: ExpressServicePOS.UI.Views/NewOrderPage.xaml.cs
using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using ExpressServicePOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ExpressServicePOS.UI.Views
{
    public partial class NewOrderPage : Page
    {
        private readonly IServiceScope _serviceScope;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<NewOrderPage> _logger;
        private Customer _selectedCustomer;
        private Driver _selectedDriver;
        private Random _random = new Random();

        public NewOrderPage()
        {
            InitializeComponent();

            // Create a new service scope
            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();

            // Resolve dependencies from the scope
            _dbContext = _serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<NewOrderPage>>();

            // Set today's date by default
            dtpOrderDate.SelectedDate = DateTime.Now;

            // Generate an order number
            GenerateOrderNumber();

            // Load customers and drivers
            LoadCustomers();
            LoadDrivers();

            // Register the Unloaded event to dispose resources
            Unloaded += NewOrderPage_Unloaded;
        }

        private void NewOrderPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Dispose the service scope when the page is unloaded
            _serviceScope?.Dispose();
        }

        private void GenerateOrderNumber()
        {
            // Generate a 4-digit order number
            string orderNumber = _random.Next(1000, 10000).ToString();
            txtOrderNumber.Text = orderNumber;
        }

        private async void LoadCustomers()
        {
            try
            {
                var customers = await _dbContext.Customers.OrderBy(c => c.Name).ToListAsync();
                cmbCustomers.ItemsSource = customers;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading customers");
                MessageBox.Show($"حدث خطأ أثناء تحميل بيانات العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadDrivers()
        {
            try
            {
                var drivers = await _dbContext.Drivers.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
                cmbDrivers.ItemsSource = drivers;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading drivers");
                MessageBox.Show($"حدث خطأ أثناء تحميل بيانات السائقين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmbCustomers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedCustomer = cmbCustomers.SelectedItem as Customer;
            if (_selectedCustomer != null)
            {
                // Auto-fill receipt information
                txtRecipientName.Text = _selectedCustomer.Name;
                txtRecipientPhone.Text = _selectedCustomer.Phone;
                txtPickupLocation.Text = _selectedCustomer.Address;
            }
        }

        private void cmbDrivers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedDriver = cmbDrivers.SelectedItem as Driver;
            if (_selectedDriver != null)
            {
                txtDriverName.Text = _selectedDriver.Name;
            }
        }

        private void txtPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateTotal();
        }

        private void txtDeliveryFee_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateTotal();
        }

        private void CalculateTotal()
        {
            if (decimal.TryParse(txtPrice.Text, out decimal price) && decimal.TryParse(txtDeliveryFee.Text, out decimal fee))
            {
                decimal total = price + fee;
                txtTotal.Text = total.ToString("N2");
            }
            else
            {
                txtTotal.Text = "0.00";
            }
        }

        private void btnNewCustomer_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to the customer page for adding a new customer
            // Implementation would vary based on your navigation approach
            MessageBox.Show("سيتم الانتقال إلى صفحة إضافة عميل جديد", "معلومات", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(txtOrderNumber.Text))
            {
                MessageBox.Show("الرجاء إدخال رقم الطلب", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedCustomer == null)
            {
                MessageBox.Show("الرجاء اختيار العميل", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtOrderDescription.Text))
            {
                MessageBox.Show("الرجاء إدخال وصف الطلب", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtPrice.Text, out decimal price))
            {
                MessageBox.Show("الرجاء إدخال سعر صحيح", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtDeliveryFee.Text, out decimal deliveryFee))
            {
                MessageBox.Show("الرجاء إدخال رسوم توصيل صحيحة", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save order
            bool success = await SaveOrder();
            if (success)
            {
                MessageBox.Show("تم حفظ الطلب بنجاح", "معلومات", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.GoBack();
            }
        }

        private async Task<bool> SaveOrder()
        {
            try
            {
                // Check if order number already exists
                var existingOrder = await _dbContext.Orders.FirstOrDefaultAsync(o => o.OrderNumber == txtOrderNumber.Text);
                if (existingOrder != null)
                {
                    MessageBox.Show("رقم الطلب موجود بالفعل. الرجاء استخدام رقم آخر.", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                // Create order object
                var order = new Order
                {
                    OrderNumber = txtOrderNumber.Text,
                    CustomerId = _selectedCustomer.Id,
                    DriverId = _selectedDriver?.Id,
                    DriverName = txtDriverName.Text,
                    OrderDescription = txtOrderDescription.Text,
                    Price = decimal.Parse(txtPrice.Text),
                    DeliveryFee = decimal.Parse(txtDeliveryFee.Text),
                    OrderDate = dtpOrderDate.SelectedDate ?? DateTime.Now,
                    DeliveryStatus = (DeliveryStatus)cmbStatus.SelectedIndex,
                    Notes = txtNotes.Text,
                    IsPaid = chkIsPaid.IsChecked ?? false,
                    Currency = (cmbCurrency.SelectedItem as ComboBoxItem)?.Content as string ?? "USD",

                    // Receipt information
                    SenderName = txtSenderName.Text,
                    SenderPhone = txtSenderPhone.Text,
                    RecipientName = txtRecipientName.Text,
                    RecipientPhone = txtRecipientPhone.Text,
                    PickupLocation = txtPickupLocation.Text,
                    DeliveryLocation = txtDeliveryLocation.Text,
                    PaymentMethod = (cmbPaymentMethod.SelectedItem as ComboBoxItem)?.Content as string ?? "نقدي",

                    // Receipt checkboxes
                    IsBreakable = chkBreakable.IsChecked ?? false,
                    IsReplacement = chkReplacement.IsChecked ?? false,
                    IsReturned = chkReturned.IsChecked ?? false
                };

                // Save to database
                _dbContext.Orders.Add(order);
                await _dbContext.SaveChangesAsync();

                _logger?.LogInformation($"Order {order.OrderNumber} created successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving order");
                MessageBox.Show($"حدث خطأ أثناء حفظ الطلب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        // New method to preview the receipt
        private async void btnPreviewReceipt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create a temporary order object with current form data
                var tempOrder = new Order
                {
                    OrderNumber = txtOrderNumber.Text,
                    CustomerId = _selectedCustomer?.Id ?? 0,
                    Customer = _selectedCustomer,
                    DriverId = _selectedDriver?.Id,
                    DriverName = txtDriverName.Text,
                    OrderDescription = txtOrderDescription.Text,
                    Price = decimal.TryParse(txtPrice.Text, out decimal price) ? price : 0,
                    DeliveryFee = decimal.TryParse(txtDeliveryFee.Text, out decimal fee) ? fee : 0,
                    OrderDate = dtpOrderDate.SelectedDate ?? DateTime.Now,
                    DeliveryStatus = (DeliveryStatus)cmbStatus.SelectedIndex,
                    IsPaid = chkIsPaid.IsChecked ?? false,
                    Currency = (cmbCurrency.SelectedItem as ComboBoxItem)?.Content as string ?? "USD",

                    // Receipt information
                    RecipientName = txtRecipientName.Text,
                    RecipientPhone = txtRecipientPhone.Text,
                    SenderName = txtSenderName.Text,
                    SenderPhone = txtSenderPhone.Text,
                    PickupLocation = txtPickupLocation.Text,
                    DeliveryLocation = txtDeliveryLocation.Text,
                    PaymentMethod = (cmbPaymentMethod.SelectedItem as ComboBoxItem)?.Content as string ?? "نقدي"
                };

                // Get receipt service
                var receiptService = _serviceScope.ServiceProvider.GetRequiredService<ReceiptService>();

                // Create preview receipt
                var receiptDocument = await receiptService.CreateExpressServiceReceiptAsync(tempOrder);

                // Get print service
                var printService = _serviceScope.ServiceProvider.GetRequiredService<PrintService>();

                // Show print preview
                printService.ShowPrintPreview(receiptDocument);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء معاينة الإيصال: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}