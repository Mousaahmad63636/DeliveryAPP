using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Infrastructure.Services;
using ExpressServicePOS.UI.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ExpressServicePOS.UI.Views
{
    public partial class SubscriptionsPage : Page
    {
        private readonly IServiceScope _serviceScope;
        private readonly SubscriptionService _subscriptionService;
        private readonly ILogger<SubscriptionsPage> _logger;
        private List<MonthlySubscription> _subscriptions;

        public SubscriptionsPage()
        {
            InitializeComponent();

            // Create a service scope and resolve services
            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();
            _subscriptionService = _serviceScope.ServiceProvider.GetRequiredService<SubscriptionService>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<SubscriptionsPage>>();

            // Subscribe to events
            Loaded += SubscriptionsPage_Loaded;
            Unloaded += SubscriptionsPage_Unloaded;

            // Set up status filter
            cmbStatusFilter.Items.Add("الكل");
            cmbStatusFilter.Items.Add("نشط");
            cmbStatusFilter.Items.Add("غير نشط");
            cmbStatusFilter.SelectedIndex = 0;
        }

        private void SubscriptionsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Dispose the service scope when the page is unloaded
            _serviceScope.Dispose();
        }

        private async void SubscriptionsPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSubscriptions();
        }

        private async Task LoadSubscriptions()
        {
            try
            {
                // Show loading indicator
                progressBar.Visibility = Visibility.Visible;

                // Get subscriptions with null check to fix the NullReferenceException
                _subscriptions = await _subscriptionService?.GetSubscriptionsAsync() ?? new List<MonthlySubscription>();

                // Apply filter if necessary
                ApplyFilter();

                // Update UI elements
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading subscriptions");
                MessageBox.Show($"حدث خطأ أثناء تحميل الاشتراكات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);

                // Set empty list as fallback
                _subscriptions = new List<MonthlySubscription>();
                dgSubscriptions.ItemsSource = _subscriptions;
            }
            finally
            {
                // Hide loading indicator
                progressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyFilter()
        {
            try
            {
                // Skip filtering if subscriptions aren't loaded
                if (_subscriptions == null)
                {
                    dgSubscriptions.ItemsSource = new List<MonthlySubscription>();
                    return;
                }

                // Apply status filter
                var filtered = _subscriptions;
                if (cmbStatusFilter.SelectedIndex == 1) // Active
                {
                    filtered = filtered.Where(s => s.IsActive).ToList();
                }
                else if (cmbStatusFilter.SelectedIndex == 2) // Inactive
                {
                    filtered = filtered.Where(s => !s.IsActive).ToList();
                }

                // Apply search filter if text is entered
                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    // FIXED - Added parentheses around the second nullable bool expression
                    filtered = filtered.Where(s =>
                        (s.Customer?.Name?.ToLower()?.Contains(searchText) ?? false) ||
                        ((s.Notes?.ToLower()?.Contains(searchText)) ?? false)).ToList();
                }

                // Update ItemsSource
                dgSubscriptions.ItemsSource = filtered;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error applying subscription filter");
            }
        }

        private void UpdateStatistics()
        {
            try
            {
                if (_subscriptions == null)
                    return;

                int totalCount = _subscriptions.Count;
                int activeCount = _subscriptions.Count(s => s.IsActive);
                int inactiveCount = totalCount - activeCount;

                // Calculate total monthly revenue
                decimal usdRevenue = _subscriptions.Where(s => s.IsActive && s.Currency == "USD").Sum(s => s.Amount);
                decimal lbpRevenue = _subscriptions.Where(s => s.IsActive && s.Currency == "LBP").Sum(s => s.Amount);

                // Update UI elements
                txtTotalCount.Text = totalCount.ToString();
                txtActiveCount.Text = activeCount.ToString();
                txtInactiveCount.Text = inactiveCount.ToString();
                txtUSDRevenue.Text = usdRevenue.ToString("N2");
                txtLBPRevenue.Text = lbpRevenue.ToString("N0");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating subscription statistics");
            }
        }

        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SubscriptionDialog();
                if (dialog.ShowDialog() == true)
                {
                    // Reload subscriptions after adding
                    await LoadSubscriptions();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error opening subscription dialog");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgSubscriptions.SelectedItem is MonthlySubscription subscription)
                {
                    var dialog = new SubscriptionDialog(subscription);
                    if (dialog.ShowDialog() == true)
                    {
                        // Reload subscriptions after editing
                        await LoadSubscriptions();
                    }
                }
                else
                {
                    MessageBox.Show("الرجاء اختيار اشتراك للتعديل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error editing subscription");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnToggleActive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgSubscriptions.SelectedItem is MonthlySubscription subscription)
                {
                    bool newStatus = !subscription.IsActive;
                    string statusText = newStatus ? "تنشيط" : "إلغاء تنشيط";

                    if (MessageBox.Show($"هل أنت متأكد من {statusText} هذا الاشتراك؟", "تأكيد",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        await _subscriptionService.SetSubscriptionActiveStatusAsync(subscription.Id, newStatus);
                        await LoadSubscriptions();
                    }
                }
                else
                {
                    MessageBox.Show("الرجاء اختيار اشتراك للتعديل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error toggling subscription status");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnRecordPayment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgSubscriptions.SelectedItem is MonthlySubscription subscription)
                {
                    // Verify the subscription is active
                    if (!subscription.IsActive)
                    {
                        MessageBox.Show("لا يمكن تسجيل دفعة لاشتراك غير نشط", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Open the payment dialog
                    var dialog = new PaymentDialog(subscription);
                    if (dialog.ShowDialog() == true)
                    {
                        await LoadSubscriptions();
                        MessageBox.Show("تم تسجيل الدفعة بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("الرجاء اختيار اشتراك لتسجيل الدفعة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error recording payment");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadSubscriptions();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void cmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private async void btnViewPayments_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgSubscriptions.SelectedItem is MonthlySubscription subscription)
                {
                    var dialog = new PaymentHistoryDialog(subscription.Id);
                    dialog.ShowDialog();
                }
                else
                {
                    MessageBox.Show("الرجاء اختيار اشتراك لعرض سجل المدفوعات", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error viewing payment history");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}