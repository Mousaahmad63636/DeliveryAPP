using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using ExpressServicePOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ExpressServicePOS.UI.Views
{
    public partial class PaymentDialog : Window
    {
        private readonly IServiceScope _serviceScope;
        private readonly SubscriptionService _subscriptionService;
        private readonly ILogger<PaymentDialog> _logger;
        private readonly MonthlySubscription _subscription;
        private readonly SubscriptionPayment _payment;

        public PaymentDialog(MonthlySubscription subscription)
        {
            InitializeComponent();

            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();
            _subscriptionService = _serviceScope.ServiceProvider.GetRequiredService<SubscriptionService>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<PaymentDialog>>();

            _subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));

            // Initialize payment object
            _payment = new SubscriptionPayment
            {
                SubscriptionId = _subscription.Id,
                Amount = _subscription.Amount,
                PaymentDate = DateTime.Today,
                Currency = _subscription.Currency,
                PaymentMethod = "نقدي"
            };

            // Initialize UI
            txtCustomerInfo.Text = $"العميل: {_subscription.Customer?.Name ?? "غير معروف"} - مبلغ الاشتراك: {FormatAmount(_subscription.Amount, _subscription.Currency)}";
            txtAmount.Text = _subscription.Amount.ToString("N2");
            txtCurrency.Text = _subscription.Currency == "USD" ? "$" : "ل.ل";
            dtpPaymentDate.SelectedDate = DateTime.Today;

            // Calculate and set period dates
            CalculatePeriodDates();

            Closed += (s, e) => _serviceScope.Dispose();
        }

        private void CalculatePeriodDates()
        {
            DateTime periodStart;
            DateTime periodEnd;

            // Find the most recent payment
            var lastPayment = _subscription.Payments
                .OrderByDescending(p => p.PeriodEndDate)
                .FirstOrDefault();

            if (lastPayment != null)
            {
                // Next period starts after the last period
                periodStart = lastPayment.PeriodEndDate.AddDays(1);
            }
            else
            {
                // First payment, period starts from subscription start date
                periodStart = _subscription.StartDate;
            }

            // Calculate period end date based on the day of month
            var nextMonth = periodStart.AddMonths(1);
            periodEnd = new DateTime(nextMonth.Year, nextMonth.Month, _subscription.DayOfMonth).AddDays(-1);

            // If the calculated end date is in the same month as the start date,
            // it means the dayOfMonth is later in the month than the start date day
            if (periodEnd.Month == periodStart.Month)
            {
                periodEnd = periodEnd.AddMonths(1);
            }

            // Set dates in UI
            dtpPeriodStartDate.SelectedDate = periodStart;
            dtpPeriodEndDate.SelectedDate = periodEnd;

            // Set dates in payment object
            _payment.PeriodStartDate = periodStart;
            _payment.PeriodEndDate = periodEnd;
        }

        private string FormatAmount(decimal amount, string currency)
        {
            return currency == "USD"
                ? $"{amount:N2} $"
                : $"{amount:N0} ل.ل";
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("الرجاء إدخال قيمة صحيحة للدفعة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtAmount.Focus();
                    return;
                }

                if (dtpPaymentDate.SelectedDate == null)
                {
                    MessageBox.Show("الرجاء اختيار تاريخ الدفع", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    dtpPaymentDate.Focus();
                    return;
                }

                // Update payment object
                _payment.Amount = amount;
                _payment.PaymentDate = dtpPaymentDate.SelectedDate.Value;
                _payment.PaymentMethod = ((ComboBoxItem)cmbPaymentMethod.SelectedItem).Content.ToString();
                _payment.Notes = txtNotes.Text;

                // Save to database
                await _subscriptionService.RecordPaymentAsync(_payment);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error recording payment");
                MessageBox.Show($"حدث خطأ أثناء تسجيل الدفعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}