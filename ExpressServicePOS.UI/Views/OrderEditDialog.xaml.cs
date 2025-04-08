// File: ExpressServicePOS.UI/Views/OrderEditDialog.xaml.cs
using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using ExpressServicePOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ExpressServicePOS.UI.Views
{
    public partial class OrderEditDialog : Window
    {
        private readonly Order _order;
        private readonly IServiceScope _serviceScope;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<OrderEditDialog> _logger;
        private Customer _selectedCustomer;
        private Driver _selectedDriver;
        private MonthlySubscription _activeSubscription;

        public OrderEditDialog(Order order)
        {
            InitializeComponent();

            _order = order ?? throw new ArgumentNullException(nameof(order));

            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();
            _dbContextFactory = _serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            _logger = _serviceScope.ServiceProvider.GetService<ILogger<OrderEditDialog>>();

            // Initialize subscription label
            lblSubscriptionStatus.Visibility = Visibility.Collapsed;

            LoadCustomers();
            LoadDrivers();
            PopulateFields();

            Closed += (s, e) => _serviceScope.Dispose();
        }

        private async void LoadCustomers()
        {
            try
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    var customers = await dbContext.Customers.OrderBy(c => c.Name).ToListAsync();
                    cmbCustomers.ItemsSource = customers;
                    cmbCustomers.SelectedItem = customers.FirstOrDefault(c => c.Id == _order.CustomerId);
                    _selectedCustomer = _order.Customer;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل بيانات العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadDrivers()
        {
            try
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    var drivers = await dbContext.Drivers.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
                    cmbDrivers.ItemsSource = drivers;
                    cmbDrivers.SelectedItem = drivers.FirstOrDefault(d => d.Id == _order.DriverId);
                    _selectedDriver = _order.Driver;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل بيانات السائقين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateFields()
        {
            txtOrderNumber.Text = _order.OrderNumber;
            dtpOrderDate.SelectedDate = _order.OrderDate;
            txtOrderDescription.Text = _order.OrderDescription;

            switch (_order.DeliveryStatus)
            {
                case DeliveryStatus.Pending:
                    cmbStatus.SelectedIndex = 0;
                    break;
                case DeliveryStatus.PickedUp:
                    cmbStatus.SelectedIndex = 1;
                    break;
                case DeliveryStatus.InTransit:
                    cmbStatus.SelectedIndex = 2;
                    break;
                case DeliveryStatus.Delivered:
                    cmbStatus.SelectedIndex = 3;
                    break;
                case DeliveryStatus.PartialDelivery:
                    cmbStatus.SelectedIndex = 4;
                    break;
                case DeliveryStatus.Failed:
                    cmbStatus.SelectedIndex = 5;
                    break;
                case DeliveryStatus.Cancelled:
                    cmbStatus.SelectedIndex = 6;
                    break;
                default:
                    cmbStatus.SelectedIndex = 0;
                    break;
            }

            txtNotes.Text = _order.Notes;
            txtDriverName.Text = _order.DriverName;
            txtSenderName.Text = _order.SenderName;
            txtSenderPhone.Text = _order.SenderPhone;
            txtRecipientName.Text = _order.RecipientName;
            txtRecipientPhone.Text = _order.RecipientPhone;
            txtPickupLocation.Text = _order.PickupLocation;
            txtDeliveryLocation.Text = _order.DeliveryLocation;
            txtPrice.Text = _order.Price.ToString("N2");
            txtDeliveryFee.Text = _order.DeliveryFee.ToString("N2");
            txtTotal.Text = _order.TotalPrice.ToString("N2");

            // Handle subscription info
            if (_order.IsCoveredBySubscription && _order.Subscription != null)
            {
                _activeSubscription = _order.Subscription;
                string currency = _activeSubscription.Currency == "USD" ? "$" : "ل.ل";
                lblSubscriptionStatus.Content = $"اشتراك شهري نشط - {_activeSubscription.Amount:N2} {currency}";
                lblSubscriptionStatus.Visibility = Visibility.Visible;

                // If order is covered by subscription, disable delivery fee
                txtDeliveryFee.IsEnabled = false;
                txtDeliveryFee.ToolTip = "لا يتم احتساب رسوم توصيل للعملاء ذوي الاشتراك الشهري النشط";
            }
            else
            {
                lblSubscriptionStatus.Visibility = Visibility.Collapsed;
                txtDeliveryFee.IsEnabled = true;
                txtDeliveryFee.ToolTip = null;
            }

            if (_order.Currency == "USD")
                cmbCurrency.SelectedIndex = 0;
            else if (_order.Currency == "LBP")
                cmbCurrency.SelectedIndex = 1;
            else
                cmbCurrency.SelectedIndex = 0;

            chkIsPaid.IsChecked = _order.IsPaid;
        }

        // File: ExpressServicePOS.UI/Views/OrderEditDialog.xaml.cs
        // Update cmbCustomers_SelectionChanged method:

        private async void cmbCustomers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCustomers.SelectedItem is Customer selectedCustomer)
            {
                _selectedCustomer = selectedCustomer;

                // Update this to populate sender fields instead of recipient fields
                txtSenderName.Text = _selectedCustomer.Name;
                txtSenderPhone.Text = _selectedCustomer.Phone;
                txtPickupLocation.Text = _selectedCustomer.Address;

                // Check for active subscription
                try
                {
                    var subscriptionService = _serviceScope.ServiceProvider.GetRequiredService<SubscriptionService>();
                    _activeSubscription = await subscriptionService.GetActiveSubscriptionForCustomerAsync(_selectedCustomer.Id);

                    // Update UI to reflect subscription status
                    if (_activeSubscription != null)
                    {
                        string currency = _activeSubscription.Currency == "USD" ? "$" : "ل.ل";
                        lblSubscriptionStatus.Content = $"اشتراك شهري نشط - {_activeSubscription.Amount:N2} {currency}";
                        lblSubscriptionStatus.Visibility = Visibility.Visible;

                        // If customer has subscription, disable delivery fee
                        txtDeliveryFee.Text = "0.00";
                        txtDeliveryFee.IsEnabled = false;

                        // Add tooltip to explain why delivery fee is disabled
                        txtDeliveryFee.ToolTip = "لا يتم احتساب رسوم توصيل للعملاء ذوي الاشتراك الشهري النشط";
                    }
                    else
                    {
                        lblSubscriptionStatus.Visibility = Visibility.Collapsed;
                        txtDeliveryFee.IsEnabled = true;
                        txtDeliveryFee.ToolTip = null;
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't show to user
                    var logger = _serviceScope.ServiceProvider.GetService<ILogger<OrderEditDialog>>();
                    logger?.LogError(ex, "Error checking customer subscription status");
                }
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
            UpdateTotal();
        }

        private void txtDeliveryFee_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            if (decimal.TryParse(txtPrice.Text, out decimal price) &&
                decimal.TryParse(txtDeliveryFee.Text, out decimal fee))
            {
                decimal total = price + fee;
                txtTotal.Text = total.ToString("N2");
            }
            else
            {
                txtTotal.Text = "0.00";
            }
        }

        private async void btnNewCustomer_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomerDialog();
            if (dialog.ShowDialog() == true)
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    dbContext.Customers.Add(dialog.Customer);
                    await dbContext.SaveChangesAsync();
                }
                LoadCustomers();
            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbCustomers.SelectedItem == null)
                {
                    MessageBox.Show("الرجاء اختيار العميل", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbCustomers.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtOrderDescription.Text))
                {
                    MessageBox.Show("الرجاء إدخال وصف الطلب", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtOrderDescription.Focus();
                    return;
                }

                if (!decimal.TryParse(txtPrice.Text, out decimal price))
                {
                    MessageBox.Show("الرجاء إدخال سعر صحيح", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPrice.Focus();
                    return;
                }

                if (!decimal.TryParse(txtDeliveryFee.Text, out decimal deliveryFee))
                {
                    MessageBox.Show("الرجاء إدخال رسوم توصيل صحيحة", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtDeliveryFee.Focus();
                    return;
                }

                _selectedCustomer = cmbCustomers.SelectedItem as Customer;
                if (_selectedCustomer != null)
                {
                    _order.CustomerId = _selectedCustomer.Id;
                    _order.Customer = _selectedCustomer;
                }

                _selectedDriver = cmbDrivers.SelectedItem as Driver;
                _order.DriverId = _selectedDriver?.Id;
                _order.Driver = _selectedDriver;
                _order.DriverName = txtDriverName.Text;
                _order.OrderDescription = txtOrderDescription.Text;
                _order.OrderDate = dtpOrderDate.SelectedDate ?? DateTime.Now;

                switch (cmbStatus.SelectedIndex)
                {
                    case 0:
                        _order.DeliveryStatus = DeliveryStatus.Pending;
                        break;
                    case 1:
                        _order.DeliveryStatus = DeliveryStatus.PickedUp;
                        break;
                    case 2:
                        _order.DeliveryStatus = DeliveryStatus.InTransit;
                        break;
                    case 3:
                        _order.DeliveryStatus = DeliveryStatus.Delivered;
                        break;
                    case 4:
                        _order.DeliveryStatus = DeliveryStatus.PartialDelivery;
                        break;
                    case 5:
                        _order.DeliveryStatus = DeliveryStatus.Failed;
                        break;
                    case 6:
                        _order.DeliveryStatus = DeliveryStatus.Cancelled;
                        break;
                }

                _order.Notes = txtNotes.Text;
                _order.IsPaid = chkIsPaid.IsChecked ?? false;
                _order.SenderName = txtSenderName.Text;
                _order.SenderPhone = txtSenderPhone.Text;
                _order.RecipientName = txtRecipientName.Text;
                _order.RecipientPhone = txtRecipientPhone.Text;
                _order.PickupLocation = txtPickupLocation.Text;
                _order.DeliveryLocation = txtDeliveryLocation.Text;
                _order.Price = price;
                _order.DeliveryFee = deliveryFee;
                _order.Currency = ((ComboBoxItem)cmbCurrency.SelectedItem).Content.ToString();

                // Update subscription information
                _order.IsCoveredBySubscription = _activeSubscription != null;
                _order.SubscriptionId = _activeSubscription?.Id;

                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    dbContext.Orders.Update(_order);
                    await dbContext.SaveChangesAsync();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ التغييرات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}