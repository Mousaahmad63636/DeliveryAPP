using ExpressServicePOS.Data.Context;
using ExpressServicePOS.Infrastructure.Services;
using ExpressServicePOS.UI.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ExpressServicePOS.UI
{
    public partial class MainWindow : Window
    {
        private readonly IServiceScope _serviceScope;
        private readonly DatabaseTestService _databaseTestService;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<MainWindow> _logger;
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();

            // Create a new service scope
            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();

            // Resolve dependencies from the scope
            _databaseTestService = _serviceScope.ServiceProvider.GetRequiredService<DatabaseTestService>();
            _dbContext = _serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<MainWindow>>();

            _logger.LogInformation("Application started");

            // Navigate to dashboard by default
            MainFrame.Navigate(new DashboardPage());

            // Register window closing event to dispose resources
            Closed += MainWindow_Closed;

            // Initialize the clock timer
            InitializeTimer();
            UpdateDateTime();
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateDateTime();
        }

        private void UpdateDateTime()
        {
            txtCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
            txtCurrentDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            // Dispose the timer
            _timer?.Stop();

            // Dispose the service scope when the window is closed
            _serviceScope?.Dispose();
        }

        private void btnDashboard_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new DashboardPage());
        }

        private void btnNewOrder_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new NewOrderPage());
        }

        private void btnManageOrders_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new OrdersPage());
        }

        private void btnCustomers_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new CustomersPage());
        }

        private void btnDrivers_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new DriversPage());
        }

        private void btnReports_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReportsPage());
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SettingsPage());
        }

        private void btnOrderLookup_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new OrderLookupPage());
        }

        private void btnImportExport_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ImportExportPage());
        }

        private async void btnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if services were injected
                if (_databaseTestService == null || _dbContext == null)
                {
                    MessageBox.Show("خطأ: لم يتم تهيئة خدمات قاعدة البيانات بشكل صحيح.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Test connection
                var result = await _databaseTestService.TestConnectionAsync();

                if (result.Success)
                {
                    int customerCount = 0;
                    int orderCount = 0;
                    int driverCount = 0;

                    try
                    {
                        // Count entities in database
                        customerCount = await _dbContext.Customers.CountAsync();
                        orderCount = await _dbContext.Orders.CountAsync();
                        driverCount = await _dbContext.Drivers.CountAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error counting entities. Tables may not exist yet.");
                    }

                    MainFrame.Navigate(new ConnectionTestPage(result.Message, customerCount, orderCount, driverCount));
                }
                else
                {
                    MainFrame.Navigate(new ConnectionTestPage(result.Message, 0, 0, 0));
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"حدث خطأ غير متوقع: {ex.Message}";
                if (_logger != null)
                {
                    _logger.LogError(ex, "Error during database test");
                }
                MessageBox.Show(errorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}