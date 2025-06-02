// File: ExpressServicePOS.UI/Views/OrderLookupPage.xaml.cs
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
    public partial class OrderLookupPage : Page
    {
        private readonly IServiceScope _serviceScope;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<OrderLookupPage> _logger;

        public OrderLookupPage()
        {
            InitializeComponent();

            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();
            _dbContextFactory = _serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<OrderLookupPage>>();

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
                string searchText = txtSearch.Text?.Trim();
                if (string.IsNullOrEmpty(searchText))
                {
                    MessageBox.Show("الرجاء إدخال رقم الطلب أو اسم العميل للبحث", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    var query = dbContext.Orders
                        .Include(o => o.Customer)
                        .Include(o => o.Driver)
                        .AsQueryable();

                    // If search is numeric, search by order number
                    if (int.TryParse(searchText, out _) || searchText.All(c => char.IsDigit(c) || c == '-' || c == '/'))
                    {
                        query = query.Where(o => o.OrderNumber.Contains(searchText));
                    }
                    else
                    {
                        // Search by customer name
                        query = query.Where(o => o.Customer.Name.Contains(searchText) ||
                                               o.RecipientName.Contains(searchText) ||
                                               o.SenderName.Contains(searchText));
                    }

                    var results = await query
                        .OrderByDescending(o => o.OrderDate)
                        .ToListAsync();

                    dgResults.ItemsSource = results;

                    lblResultCount.Content = $"عدد النتائج: {results.Count}";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error searching orders");
                MessageBox.Show($"حدث خطأ أثناء البحث: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void txtSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                btnSearch_Click(sender, e);
            }
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            dgResults.ItemsSource = null;
            lblResultCount.Content = "عدد النتائج: 0";
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void dgResults_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgResults.SelectedItem is Order selectedOrder)
            {
                var dialog = new OrderEditDialog(selectedOrder);
                if (dialog.ShowDialog() == true)
                {
                    // Refresh data
                    btnSearch_Click(sender, e);
                }
            }
        }
    }
}