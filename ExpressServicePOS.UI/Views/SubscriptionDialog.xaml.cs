// File: ExpressServicePOS.UI/Views/SubscriptionDialog.xaml.cs
using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using ExpressServicePOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ExpressServicePOS.UI.Views
{
    /// <summary>
    /// Interaction logic for SubscriptionDialog.xaml
    /// </summary>
    public partial class SubscriptionDialog : Window
    {
        private readonly IServiceScope _serviceScope;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly SubscriptionService _subscriptionService;
        private readonly ILogger<SubscriptionDialog> _logger;
        private MonthlySubscription _subscription;
        private bool _isEditMode;

        public SubscriptionDialog()
        {
            InitializeComponent();

            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();
            _dbContextFactory = _serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            _subscriptionService = _serviceScope.ServiceProvider.GetRequiredService<SubscriptionService>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<SubscriptionDialog>>();

            _subscription = new MonthlySubscription
            {
                StartDate = DateTime.Today,
                IsActive = true,
                DayOfMonth = DateTime.Today.Day, // Default to today's day
                Currency = "USD"
            };

            _isEditMode = false;
            txtTitle.Text = "إضافة اشتراك شهري جديد";

            PopulateDaysOfMonth();
            LoadCustomers();

            dtpStartDate.SelectedDate = DateTime.Today;

            Closed += (s, e) => _serviceScope.Dispose();
        }

        public SubscriptionDialog(MonthlySubscription subscription) : this()
        {
            _subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
            _isEditMode = true;
            txtTitle.Text = "تعديل اشتراك شهري";

            PopulateFields();
        }

        private void PopulateDaysOfMonth()
        {
            for (int day = 1; day <= 31; day++)
            {
                cmbDayOfMonth.Items.Add(day);
            }

            cmbDayOfMonth.SelectedIndex = 0; // Default to 1st day
        }

        private async void LoadCustomers()
        {
            try
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    var customers = await dbContext.Customers.OrderBy(c => c.Name).ToListAsync();
                    cmbCustomers.ItemsSource = customers;

                    if (_isEditMode && _subscription.CustomerId > 0)
                    {
                        cmbCustomers.SelectedItem = customers.FirstOrDefault(c => c.Id == _subscription.CustomerId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading customers");
                MessageBox.Show($"حدث خطأ أثناء تحميل بيانات العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateFields()
        {
            txtAmount.Text = _subscription.Amount.ToString("N2");

            // Set currency
            if (_subscription.Currency == "USD")
                cmbCurrency.SelectedIndex = 0;
            else if (_subscription.Currency == "LBP")
                cmbCurrency.SelectedIndex = 1;

            // Set day of month
            if (_subscription.DayOfMonth >= 1 && _subscription.DayOfMonth <= 31)
                cmbDayOfMonth.SelectedIndex = _subscription.DayOfMonth - 1;

            dtpStartDate.SelectedDate = _subscription.StartDate;
            dtpEndDate.SelectedDate = _subscription.EndDate;

            chkIsActive.IsChecked = _subscription.IsActive;
            txtNotes.Text = _subscription.Notes;
        }

        private async void btnNewCustomer_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomerDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                    {
                        dbContext.Customers.Add(dialog.Customer);
                        await dbContext.SaveChangesAsync();
                        LoadCustomers();

                        // Select the newly added customer
                        await Task.Delay(100); // Small delay to ensure list is updated
                        cmbCustomers.SelectedItem = cmbCustomers.Items.Cast<Customer>()
                            .FirstOrDefault(c => c.Id == dialog.Customer.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error adding new customer");
                    MessageBox.Show($"حدث خطأ أثناء إضافة العميل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (cmbCustomers.SelectedItem == null)
                {
                    MessageBox.Show("الرجاء اختيار العميل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbCustomers.Focus();
                    return;
                }

                if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("الرجاء إدخال قيمة صحيحة للاشتراك", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtAmount.Focus();
                    return;
                }

                if (cmbDayOfMonth.SelectedItem == null)
                {
                    MessageBox.Show("الرجاء اختيار يوم الاستحقاق", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbDayOfMonth.Focus();
                    return;
                }

                if (dtpStartDate.SelectedDate == null)
                {
                    MessageBox.Show("الرجاء اختيار تاريخ بدء الاشتراك", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    dtpStartDate.Focus();
                    return;
                }

                // Update subscription object
                _subscription.CustomerId = ((Customer)cmbCustomers.SelectedItem).Id;
                _subscription.Amount = amount;
                _subscription.Currency = ((ComboBoxItem)cmbCurrency.SelectedItem).Content.ToString();
                _subscription.DayOfMonth = (int)cmbDayOfMonth.SelectedItem;
                _subscription.StartDate = dtpStartDate.SelectedDate.Value;
                _subscription.EndDate = dtpEndDate.SelectedDate;
                _subscription.IsActive = chkIsActive.IsChecked ?? true;
                _subscription.Notes = txtNotes.Text;

                // Save to database
                if (_isEditMode)
                {
                    bool success = await _subscriptionService.UpdateSubscriptionAsync(_subscription);
                    if (success)
                    {
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("حدث خطأ أثناء تحديث الاشتراك", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    await _subscriptionService.CreateSubscriptionAsync(_subscription);
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving subscription");
                MessageBox.Show($"حدث خطأ أثناء حفظ الاشتراك: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}