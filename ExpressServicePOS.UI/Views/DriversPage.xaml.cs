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
    public partial class DriversPage : Page
    {
        private readonly IServiceScope _serviceScope;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<DriversPage> _logger;

        public DriversPage()
        {
            InitializeComponent();

            // Create a new service scope
            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();

            // Resolve dependencies from the scope
            _dbContext = _serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<DriversPage>>();

            // Load drivers
            LoadDrivers();

            // Register the Unloaded event to dispose resources
            Unloaded += DriversPage_Unloaded;
        }

        private void DriversPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Dispose the service scope when the page is unloaded
            _serviceScope?.Dispose();
        }

        private async void LoadDrivers()
        {
            try
            {
                var drivers = await _dbContext.Drivers.OrderBy(d => d.Name).ToListAsync();
                dgDrivers.ItemsSource = drivers;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading drivers");
                MessageBox.Show($"حدث خطأ أثناء تحميل بيانات السائقين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddNewDriver_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new DriverDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _dbContext.Drivers.Add(dialog.Driver);
                    _dbContext.SaveChanges();
                    LoadDrivers();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error adding new driver");
                    MessageBox.Show($"حدث خطأ أثناء إضافة السائق: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    LoadDrivers();
                }
                else
                {
                    var drivers = await _dbContext.Drivers
                        .Where(d => d.Name.Contains(searchTerm) ||
                               d.Phone.Contains(searchTerm) ||
                               d.Email.Contains(searchTerm) ||
                               d.VehiclePlateNumber.Contains(searchTerm) ||
                               d.AssignedZones.Contains(searchTerm))
                        .OrderBy(d => d.Name)
                        .ToListAsync();

                    dgDrivers.ItemsSource = drivers;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error searching drivers");
                MessageBox.Show($"حدث خطأ أثناء البحث: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgDrivers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgDrivers.SelectedItem is Driver selectedDriver)
            {
                EditDriver(selectedDriver.Id);
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int driverId)
            {
                EditDriver(driverId);
            }
        }

        private void EditDriver(int driverId)
        {
            try
            {
                var driver = _dbContext.Drivers.Find(driverId);
                if (driver != null)
                {
                    var dialog = new DriverDialog();
                    dialog.Driver = driver;
                    dialog.PopulateFields();

                    if (dialog.ShowDialog() == true)
                    {
                        _dbContext.Entry(driver).State = EntityState.Modified;
                        _dbContext.SaveChanges();
                        LoadDrivers();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error editing driver");
                MessageBox.Show($"حدث خطأ أثناء تعديل بيانات السائق: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int driverId)
            {
                // Check if driver has orders
                var hasOrders = await _dbContext.Orders.AnyAsync(o => o.DriverId == driverId);
                if (hasOrders)
                {
                    MessageBox.Show("لا يمكن حذف هذا السائق لأنه مرتبط بطلبات.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Confirm deletion
                var result = MessageBox.Show("هل أنت متأكد من حذف هذا السائق؟", "تأكيد الحذف",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var driver = await _dbContext.Drivers.FindAsync(driverId);
                        if (driver != null)
                        {
                            _dbContext.Drivers.Remove(driver);
                            await _dbContext.SaveChangesAsync();
                            LoadDrivers();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error deleting driver");
                        MessageBox.Show($"حدث خطأ أثناء حذف السائق: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}