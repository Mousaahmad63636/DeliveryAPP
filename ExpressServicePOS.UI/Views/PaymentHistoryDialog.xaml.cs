// File: ExpressServicePOS.UI/Views/PaymentHistoryDialog.xaml.cs
using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using ExpressServicePOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ExpressServicePOS.UI.Views
{
    /// <summary>
    /// Interaction logic for PaymentHistoryDialog.xaml
    /// </summary>
    public partial class PaymentHistoryDialog : Window
    {
        private readonly IServiceScope _serviceScope;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly SubscriptionService _subscriptionService;
        private readonly ILogger<PaymentHistoryDialog> _logger;
        private readonly int _subscriptionId;

        public PaymentHistoryDialog(int subscriptionId)
        {
            InitializeComponent();

            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();
            _dbContextFactory = _serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            _subscriptionService = _serviceScope.ServiceProvider.GetRequiredService<SubscriptionService>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<PaymentHistoryDialog>>();

            _subscriptionId = subscriptionId;

            LoadSubscriptionDetails();
            LoadPaymentHistory();

            Closed += (s, e) => _serviceScope.Dispose();
        }

        private async void LoadSubscriptionDetails()
        {
            try
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    var subscription = await dbContext.MonthlySubscriptions
                        .Include(s => s.Customer)
                        .FirstOrDefaultAsync(s => s.Id == _subscriptionId);

                    if (subscription != null)
                    {
                        string formattedAmount = subscription.Currency == "USD"
                            ? $"{subscription.Amount:N2} $"
                            : $"{subscription.Amount:N0} ل.ل";

                        txtSubscriptionInfo.Text = $"العميل: {subscription.Customer?.Name ?? "غير معروف"} - " +
                                                  $"قيمة الاشتراك: {formattedAmount} - " +
                                                  $"يوم الاستحقاق: {subscription.DayOfMonth}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading subscription details");
                MessageBox.Show($"حدث خطأ أثناء تحميل تفاصيل الاشتراك: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadPaymentHistory()
        {
            try
            {
                var payments = await _subscriptionService.GetPaymentsForSubscriptionAsync(_subscriptionId);

                var viewModels = payments.Select(p => new PaymentViewModel
                {
                    Id = p.Id,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    Currency = p.Currency,
                    AmountFormatted = p.Currency == "USD" ? $"{p.Amount:N2} $" : $"{p.Amount:N0} ل.ل",
                    PeriodStartDate = p.PeriodStartDate,
                    PeriodEndDate = p.PeriodEndDate,
                    PeriodFormatted = $"{p.PeriodStartDate:yyyy-MM-dd} - {p.PeriodEndDate:yyyy-MM-dd}",
                    PaymentMethod = p.PaymentMethod,
                    Notes = p.Notes
                }).ToList();

                dgPayments.ItemsSource = viewModels;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading payment history");
                MessageBox.Show($"حدث خطأ أثناء تحميل سجل المدفوعات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class PaymentViewModel
    {
        public int Id { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string AmountFormatted { get; set; }
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public string PeriodFormatted { get; set; }
        public string PaymentMethod { get; set; }
        public string Notes { get; set; }
    }
}