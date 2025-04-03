using ExpressServicePOS.Core.Models;
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
    public partial class ReportsPage : Page
    {
        private readonly IServiceScope _serviceScope;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ReportsPage> _logger;

        public ReportsPage()
        {
            InitializeComponent();

            // Create a new service scope
            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();

            // Resolve dependencies from the scope
            _dbContext = _serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<ReportsPage>>();

            // Initialize the page
            InitializePage();

            // Register the Unloaded event to dispose resources
            Unloaded += ReportsPage_Unloaded;
        }

        private void ReportsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Dispose the service scope when the page is unloaded
            _serviceScope?.Dispose();
        }

        private async void InitializePage()
        {
            try
            {
                // Set default dates
                dpFromDate.SelectedDate = DateTime.Now.AddDays(-7);
                dpToDate.SelectedDate = DateTime.Now;

                dpSalesFromDate.SelectedDate = DateTime.Now.AddDays(-30);
                dpSalesToDate.SelectedDate = DateTime.Now;

                // Load customers
                var customers = await _dbContext.Customers.OrderBy(c => c.Name).ToListAsync();
                cmbCustomers.ItemsSource = customers;
                cmbCustomers.DisplayMemberPath = "Name";
                cmbCustomers.SelectedValuePath = "Id";

                if (customers.Any())
                {
                    cmbCustomers.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing reports page");
                MessageBox.Show($"حدث خطأ أثناء تحميل صفحة التقارير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnGenerateCustomerReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbCustomers.SelectedItem == null)
                {
                    MessageBox.Show("الرجاء اختيار العميل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (dpFromDate.SelectedDate == null || dpToDate.SelectedDate == null)
                {
                    MessageBox.Show("الرجاء تحديد التاريخ", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Navigate to customer report page
                var customer = cmbCustomers.SelectedItem as Customer;
                DateTime fromDate = dpFromDate.SelectedDate.Value;
                DateTime toDate = dpToDate.SelectedDate.Value.AddDays(1).AddSeconds(-1); // End of the selected day

                MessageBox.Show($"جاري إنشاء تقرير للعميل: {customer.Name} من {fromDate.ToShortDateString()} إلى {toDate.ToShortDateString()}",
                    "تقرير العميل", MessageBoxButton.OK, MessageBoxImage.Information);

                // TODO: Implement customer report generation logic
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating customer report");
                MessageBox.Show($"حدث خطأ أثناء إنشاء تقرير العميل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnGenerateSalesReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dpSalesFromDate.SelectedDate == null || dpSalesToDate.SelectedDate == null)
                {
                    MessageBox.Show("الرجاء تحديد التاريخ", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Navigate to sales report page
                DateTime fromDate = dpSalesFromDate.SelectedDate.Value;
                DateTime toDate = dpSalesToDate.SelectedDate.Value.AddDays(1).AddSeconds(-1); // End of the selected day

                MessageBox.Show($"جاري إنشاء تقرير المبيعات من {fromDate.ToShortDateString()} إلى {toDate.ToShortDateString()}",
                    "تقرير المبيعات", MessageBoxButton.OK, MessageBoxImage.Information);

                // TODO: Implement sales report generation logic
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating sales report");
                MessageBox.Show($"حدث خطأ أثناء إنشاء تقرير المبيعات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}