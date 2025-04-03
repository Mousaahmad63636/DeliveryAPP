using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ExpressServicePOS.UI.Views
{
    public partial class CustomersPage : Page
    {
        private readonly IServiceScope _serviceScope;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<CustomersPage> _logger;

        public CustomersPage()
        {
            InitializeComponent();

            // Create a new service scope
            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();

            // Resolve dependencies from the scope
            _dbContext = _serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<CustomersPage>>();

            // Load customers
            LoadCustomers();

            // Register the Unloaded event to dispose resources
            Unloaded += CustomersPage_Unloaded;
        }

        private void CustomersPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Dispose the service scope when the page is unloaded
            _serviceScope?.Dispose();
        }

        private async void LoadCustomers()
        {
            try
            {
                var customers = await _dbContext.Customers.OrderBy(c => c.Name).ToListAsync();
                dgCustomers.ItemsSource = customers;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading customers");
                MessageBox.Show($"حدث خطأ أثناء تحميل بيانات العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddNewCustomer_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomerDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _dbContext.Customers.Add(dialog.Customer);
                    _dbContext.SaveChanges();
                    LoadCustomers();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error adding new customer");
                    MessageBox.Show($"حدث خطأ أثناء إضافة العميل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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

        private async void ApplySearch()
        {
            string searchTerm = txtSearch.Text.Trim();

            try
            {
                if (string.IsNullOrEmpty(searchTerm))
                {
                    LoadCustomers();
                }
                else
                {
                    var customers = await _dbContext.Customers
                        .Where(c => c.Name.Contains(searchTerm) ||
                               c.Phone.Contains(searchTerm) ||
                               c.Address.Contains(searchTerm))
                        .OrderBy(c => c.Name)
                        .ToListAsync();

                    dgCustomers.ItemsSource = customers;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error searching customers");
                MessageBox.Show($"حدث خطأ أثناء البحث: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgCustomers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgCustomers.SelectedItem is Customer selectedCustomer)
            {
                EditCustomer(selectedCustomer.Id);
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int customerId)
            {
                EditCustomer(customerId);
            }
        }

        private void EditCustomer(int customerId)
        {
            try
            {
                var customer = _dbContext.Customers.Find(customerId);
                if (customer != null)
                {
                    var dialog = new CustomerDialog();
                    dialog.Customer = customer;
                    dialog.PopulateFields();

                    if (dialog.ShowDialog() == true)
                    {
                        _dbContext.Entry(customer).State = EntityState.Modified;
                        _dbContext.SaveChanges();
                        LoadCustomers();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error editing customer");
                MessageBox.Show($"حدث خطأ أثناء تعديل بيانات العميل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int customerId)
            {
                // Check if customer has orders
                var hasOrders = await _dbContext.Orders.AnyAsync(o => o.CustomerId == customerId);
                if (hasOrders)
                {
                    MessageBox.Show("لا يمكن حذف هذا العميل لأنه مرتبط بطلبات.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Confirm deletion
                var result = MessageBox.Show("هل أنت متأكد من حذف هذا العميل؟", "تأكيد الحذف",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var customer = await _dbContext.Customers.FindAsync(customerId);
                        if (customer != null)
                        {
                            _dbContext.Customers.Remove(customer);
                            await _dbContext.SaveChangesAsync();
                            LoadCustomers();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error deleting customer");
                        MessageBox.Show($"حدث خطأ أثناء حذف العميل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}