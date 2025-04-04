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
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<CustomersPage> _logger;

        public CustomersPage()
        {
            InitializeComponent();

            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();
            _dbContextFactory = _serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<CustomersPage>>();

            LoadCustomers();

            Unloaded += CustomersPage_Unloaded;
        }

        private void CustomersPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _serviceScope?.Dispose();
        }

        private async void LoadCustomers()
        {
            try
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    var customers = await dbContext.Customers.OrderBy(c => c.Name).ToListAsync();
                    dgCustomers.ItemsSource = customers;
                }
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
                    using (var dbContext = _dbContextFactory.CreateDbContext())
                    {
                        dbContext.Customers.Add(dialog.Customer);
                        dbContext.SaveChanges();
                        LoadCustomers();
                    }
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
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    if (string.IsNullOrEmpty(searchTerm))
                    {
                        var customers = await dbContext.Customers.OrderBy(c => c.Name).ToListAsync();
                        dgCustomers.ItemsSource = customers;
                    }
                    else
                    {
                        var customers = await dbContext.Customers
                            .Where(c => c.Name.Contains(searchTerm) ||
                                   c.Phone.Contains(searchTerm) ||
                                   c.Address.Contains(searchTerm))
                            .OrderBy(c => c.Name)
                            .ToListAsync();

                        dgCustomers.ItemsSource = customers;
                    }
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

        private async void EditCustomer(int customerId)
        {
            try
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    var customer = await dbContext.Customers.FindAsync(customerId);
                    if (customer != null)
                    {
                        var dialog = new CustomerDialog();
                        dialog.Customer = customer;
                        dialog.PopulateFields();

                        if (dialog.ShowDialog() == true)
                        {
                            dbContext.Entry(customer).State = EntityState.Modified;
                            await dbContext.SaveChangesAsync();
                            LoadCustomers();
                        }
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
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    // Check if customer has orders
                    var hasOrders = await dbContext.Orders.AnyAsync(o => o.CustomerId == customerId);
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
                            var customer = await dbContext.Customers.FindAsync(customerId);
                            if (customer != null)
                            {
                                dbContext.Customers.Remove(customer);
                                await dbContext.SaveChangesAsync();
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
}