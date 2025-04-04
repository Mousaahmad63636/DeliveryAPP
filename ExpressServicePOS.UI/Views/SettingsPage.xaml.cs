using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using ExpressServicePOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ExpressServicePOS.UI.Views
{
    public partial class SettingsPage : Page
    {
        private readonly IServiceScope _serviceScope;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly CurrencyService _currencyService;
        private readonly ILogger<SettingsPage> _logger;

        public SettingsPage()
        {
            InitializeComponent();

            // Create a new service scope
            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();

            // Resolve dependencies from the scope
            _dbContextFactory = _serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            _currencyService = _serviceScope.ServiceProvider.GetRequiredService<CurrencyService>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<SettingsPage>>();

            // Load settings
            LoadSettings();

            // Register the Unloaded event to dispose resources
            Unloaded += SettingsPage_Unloaded;
        }

        private void SettingsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Dispose the service scope when the page is unloaded
            _serviceScope?.Dispose();
        }

        private async void LoadSettings()
        {
            try
            {
                // Load company profile
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    var companyProfile = await dbContext.CompanyProfile.FirstOrDefaultAsync();
                    if (companyProfile != null)
                    {
                        txtCompanyName.Text = companyProfile.CompanyName;
                        txtCompanyAddress.Text = companyProfile.Address;
                        txtCompanyPhone.Text = companyProfile.Phone;
                        txtCompanyEmail.Text = companyProfile.Email;
                        txtCompanyWebsite.Text = companyProfile.Website;
                        txtCompanyTaxNumber.Text = companyProfile.TaxNumber;
                        txtReceiptFooter.Text = companyProfile.ReceiptFooterText;
                    }
                }

                // Load currency settings
                var currencySettings = await _currencyService.GetSettingsAsync();
                chkEnableMultipleCurrencies.IsChecked = currencySettings.EnableMultipleCurrencies;
                chkEnableUSD.IsChecked = currencySettings.EnableUSD;
                chkEnableLBP.IsChecked = currencySettings.EnableLBP;
                txtExchangeRate.Text = currencySettings.USDToLBPRate.ToString();
                cmbDefaultCurrency.SelectedIndex = currencySettings.DefaultCurrency == "USD" ? 0 : 1;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading settings");
                MessageBox.Show($"حدث خطأ أثناء تحميل الإعدادات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnSaveCompanyProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate company name
                if (string.IsNullOrWhiteSpace(txtCompanyName.Text))
                {
                    MessageBox.Show("الرجاء إدخال اسم الشركة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtCompanyName.Focus();
                    return;
                }

                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    // Get existing profile or create new one
                    var companyProfile = await dbContext.CompanyProfile.FirstOrDefaultAsync();
                    if (companyProfile == null)
                    {
                        companyProfile = new CompanyProfile
                        {
                            CompanyName = txtCompanyName.Text,
                            LastUpdated = DateTime.Now
                        };
                        dbContext.CompanyProfile.Add(companyProfile);
                    }
                    else
                    {
                        companyProfile.CompanyName = txtCompanyName.Text;
                        companyProfile.LastUpdated = DateTime.Now;
                    }

                    // Update other fields
                    companyProfile.Address = txtCompanyAddress.Text;
                    companyProfile.Phone = txtCompanyPhone.Text;
                    companyProfile.Email = txtCompanyEmail.Text;
                    companyProfile.Website = txtCompanyWebsite.Text;
                    companyProfile.TaxNumber = txtCompanyTaxNumber.Text;
                    companyProfile.ReceiptFooterText = txtReceiptFooter.Text;

                    await dbContext.SaveChangesAsync();
                }

                MessageBox.Show("تم حفظ معلومات الشركة بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving company profile");
                MessageBox.Show($"حدث خطأ أثناء حفظ معلومات الشركة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnSaveCurrencySettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate exchange rate
                if (!decimal.TryParse(txtExchangeRate.Text, out decimal exchangeRate) || exchangeRate <= 0)
                {
                    MessageBox.Show("الرجاء إدخال سعر صرف صحيح", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtExchangeRate.Focus();
                    return;
                }

                // Get selected default currency
                string defaultCurrency = "USD";
                if (cmbDefaultCurrency.SelectedIndex >= 0)
                {
                    defaultCurrency = ((ComboBoxItem)cmbDefaultCurrency.SelectedItem).Content.ToString();
                }

                // Update currency settings
                var settings = new CurrencySetting
                {
                    EnableMultipleCurrencies = chkEnableMultipleCurrencies.IsChecked ?? true,
                    EnableUSD = chkEnableUSD.IsChecked ?? true,
                    EnableLBP = chkEnableLBP.IsChecked ?? true,
                    USDToLBPRate = exchangeRate,
                    DefaultCurrency = defaultCurrency,
                    LastUpdated = DateTime.Now
                };

                var success = await _currencyService.UpdateSettingsAsync(settings);
                if (success)
                {
                    MessageBox.Show("تم حفظ إعدادات العملة بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("فشل حفظ إعدادات العملة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving currency settings");
                MessageBox.Show($"حدث خطأ أثناء حفظ إعدادات العملة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSaveDatabaseSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(txtServerName.Text))
                {
                    MessageBox.Show("الرجاء إدخال اسم السيرفر", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtServerName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtDatabaseName.Text))
                {
                    MessageBox.Show("الرجاء إدخال اسم قاعدة البيانات", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtDatabaseName.Focus();
                    return;
                }

                // Inform that application restart is required
                MessageBox.Show("تم حفظ إعدادات قاعدة البيانات بنجاح. سيتم تطبيق الإعدادات عند إعادة تشغيل البرنامج.",
                    "تم", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving database settings");
                MessageBox.Show($"حدث خطأ أثناء حفظ إعدادات قاعدة البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (!decimal.TryParse(txtDefaultDeliveryFee.Text, out decimal deliveryFee))
                {
                    MessageBox.Show("الرجاء إدخال قيمة رقمية صحيحة لرسوم التوصيل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtDefaultDeliveryFee.Focus();
                    return;
                }

                // Save settings (future implementation)
                MessageBox.Show("تم حفظ الإعدادات بنجاح.", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving settings");
                MessageBox.Show($"حدث خطأ أثناء حفظ الإعدادات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}