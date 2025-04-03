using ExpressServicePOS.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace ExpressServicePOS.UI.Views
{
    public partial class ImportExportPage : Page
    {
        private readonly IServiceScope _serviceScope;
        private readonly ImportExportService _importExportService;
        private readonly BackupService _backupService;
        private readonly ILogger<ImportExportPage> _logger;

        public ImportExportPage()
        {
            InitializeComponent();

            // Create a new service scope
            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();

            // Resolve dependencies from the scope
            _importExportService = _serviceScope.ServiceProvider.GetRequiredService<ImportExportService>();
            _backupService = _serviceScope.ServiceProvider.GetRequiredService<BackupService>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<ImportExportPage>>();

            // Register the Unloaded event to dispose resources
            Unloaded += ImportExportPage_Unloaded;
        }

        private void ImportExportPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Dispose the service scope when the page is unloaded
            _serviceScope?.Dispose();
        }

        #region Export Functions

        private async void btnExportCustomersToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = $"Customers_Export_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var success = await _importExportService.ExportCustomersToExcelAsync(saveFileDialog.FileName);
                    if (success)
                    {
                        MessageBox.Show("تم تصدير بيانات العملاء بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("فشل تصدير بيانات العملاء", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting customers to Excel");
                MessageBox.Show($"حدث خطأ أثناء تصدير بيانات العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnExportCustomersToCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = "csv",
                    FileName = $"Customers_Export_{DateTime.Now:yyyyMMdd}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var success = await _importExportService.ExportCustomersToCsvAsync(saveFileDialog.FileName);
                    if (success)
                    {
                        MessageBox.Show("تم تصدير بيانات العملاء بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("فشل تصدير بيانات العملاء", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting customers to CSV");
                MessageBox.Show($"حدث خطأ أثناء تصدير بيانات العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnExportOrdersToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = $"Orders_Export_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var success = await _importExportService.ExportOrdersToExcelAsync(saveFileDialog.FileName);
                    if (success)
                    {
                        MessageBox.Show("تم تصدير بيانات الطلبات بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("فشل تصدير بيانات الطلبات", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting orders to Excel");
                MessageBox.Show($"حدث خطأ أثناء تصدير بيانات الطلبات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnExportOrdersToCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = "csv",
                    FileName = $"Orders_Export_{DateTime.Now:yyyyMMdd}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var success = await _importExportService.ExportOrdersToCsvAsync(saveFileDialog.FileName);
                    if (success)
                    {
                        MessageBox.Show("تم تصدير بيانات الطلبات بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("فشل تصدير بيانات الطلبات", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting orders to CSV");
                MessageBox.Show($"حدث خطأ أثناء تصدير بيانات الطلبات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnExportDriversToExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("سيتم تنفيذ هذه الميزة قريباً", "قيد التطوير", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnExportDriversToCSV_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("سيتم تنفيذ هذه الميزة قريباً", "قيد التطوير", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Import Functions

        private async void btnImportCustomersFromExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var result = MessageBox.Show(
                        "هل أنت متأكد من استيراد بيانات العملاء؟ سيتم تحديث البيانات الموجودة وإضافة البيانات الجديدة.",
                        "تأكيد الاستيراد",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var importResult = await _importExportService.ImportCustomersFromExcelAsync(openFileDialog.FileName);
                        if (importResult.Success)
                        {
                            MessageBox.Show($"تم استيراد {importResult.Count} من بيانات العملاء بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("فشل استيراد بيانات العملاء", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error importing customers from Excel");
                MessageBox.Show($"حدث خطأ أثناء استيراد بيانات العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnImportCustomersFromCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = "csv"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var result = MessageBox.Show(
                        "هل أنت متأكد من استيراد بيانات العملاء؟ سيتم تحديث البيانات الموجودة وإضافة البيانات الجديدة.",
                        "تأكيد الاستيراد",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var importResult = await _importExportService.ImportCustomersFromCsvAsync(openFileDialog.FileName);
                        if (importResult.Success)
                        {
                            MessageBox.Show($"تم استيراد {importResult.Count} من بيانات العملاء بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("فشل استيراد بيانات العملاء", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error importing customers from CSV");
                MessageBox.Show($"حدث خطأ أثناء استيراد بيانات العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Backup Functions

        private async void btnCreateBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderBrowserDialog = new SaveFileDialog
                {
                    Filter = "Backup Files (*.bak)|*.bak",
                    DefaultExt = "bak",
                    FileName = $"ExpressServicePOS_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak"
                };

                if (folderBrowserDialog.ShowDialog() == true)
                {
                    var success = await _backupService.CreateBackupAsync(folderBrowserDialog.FileName);
                    if (success)
                    {
                        MessageBox.Show("تم إنشاء نسخة احتياطية لقاعدة البيانات بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("فشل إنشاء نسخة احتياطية لقاعدة البيانات", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating database backup");
                MessageBox.Show($"حدث خطأ أثناء إنشاء نسخة احتياطية لقاعدة البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnRestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Backup Files (*.bak)|*.bak",
                    DefaultExt = "bak"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var result = MessageBox.Show(
                        "تحذير: سيتم استبدال قاعدة البيانات الحالية بالكامل بالنسخة الاحتياطية. هل أنت متأكد من استعادة قاعدة البيانات؟",
                        "تحذير هام",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Double confirmation for critical operation
                        var confirmResult = MessageBox.Show(
                            "تأكيد نهائي: هل أنت متأكد من استعادة قاعدة البيانات؟ سيتم فقدان جميع البيانات الحالية التي لم يتم تضمينها في النسخة الاحتياطية.",
                            "تأكيد نهائي",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (confirmResult == MessageBoxResult.Yes)
                        {
                            var success = await _backupService.RestoreBackupAsync(openFileDialog.FileName);
                            if (success)
                            {
                                MessageBox.Show("تم استعادة قاعدة البيانات بنجاح. سيتم إعادة تشغيل التطبيق الآن.", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                                // Restart application
                                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                                Application.Current.Shutdown();
                            }
                            else
                            {
                                MessageBox.Show("فشل استعادة قاعدة البيانات", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error restoring database backup");
                MessageBox.Show($"حدث خطأ أثناء استعادة قاعدة البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}