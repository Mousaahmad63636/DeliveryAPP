// File: ExpressServicePOS.UI/App.xaml.cs
using ExpressServicePOS.Data;
using ExpressServicePOS.Data.Context;
using ExpressServicePOS.Data.Services;
using ExpressServicePOS.Infrastructure.Services;
using ExpressServicePOS.UI.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Windows;

namespace ExpressServicePOS.UI
{
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Configure Serilog
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "app-.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            services.AddLogging(builder =>
            {
                builder.AddSerilog(dispose: true);
            });

            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Get connection string from configuration
            string connectionString = configuration.GetConnectionString("DefaultConnection") ??
                "Server=.\\posserver;Database=ExpressServicePOS;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

            // Register DbContextFactory for thread-safe DbContext creation
            services.AddDbContextFactory<AppDbContext>(options =>
            {
                options.UseSqlServer(connectionString,
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null);
                    });
            });

            // Also register scoped DbContext for backward compatibility
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(connectionString,
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null);
                    });
            }, ServiceLifetime.Scoped);

            // Register core services
            services.AddScoped<DatabaseTestService>();
            services.AddScoped<DatabaseService>();
            services.AddScoped<DatabaseInitializer>();
            services.AddScoped<ReportService>();
            services.AddScoped<ReceiptService>();
            services.AddScoped<ImportExportService>();
            services.AddScoped<PrintService>();
            services.AddScoped<CurrencyService>();
            services.AddScoped<BackupService>();

            // Register pages
            services.AddTransient<MainWindow>();
            services.AddTransient<DashboardPage>();
            services.AddTransient<CustomersPage>();
            services.AddTransient<OrdersPage>();
            services.AddTransient<NewOrderPage>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<ConnectionTestPage>();
            services.AddTransient<DriversPage>();
            services.AddTransient<ImportExportPage>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    try
                    {
                        var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
                        dbInitializer.Initialize();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error initializing database");
                        MessageBox.Show($"خطأ في تهيئة قاعدة البيانات: {ex.Message}\n\nتأكد من تشغيل SQL Server وإنشاء قاعدة البيانات.",
                            "خطأ في قاعدة البيانات", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    try
                    {
                        var dbService = scope.ServiceProvider.GetRequiredService<DatabaseService>();
                        await dbService.InitializeDatabaseAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error in database service initialization");
                        MessageBox.Show($"خطأ في خدمة قاعدة البيانات: {ex.Message}",
                            "خطأ في قاعدة البيانات", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    try
                    {
                        var currencyService = scope.ServiceProvider.GetRequiredService<CurrencyService>();
                        await currencyService.LoadSettingsAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error loading currency settings");
                        MessageBox.Show($"خطأ في تحميل إعدادات العملة: {ex.Message}",
                            "خطأ في الإعدادات", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                Current.MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred during application startup.");
                MessageBox.Show($"خطأ أثناء بدء التطبيق: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            (ServiceProvider as IDisposable)?.Dispose();
            base.OnExit(e);
        }
    }
}