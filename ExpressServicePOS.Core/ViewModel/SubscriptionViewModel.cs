// File: ExpressServicePOS.Core/ViewModels/SubscriptionViewModel.cs
using ExpressServicePOS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpressServicePOS.Core.ViewModels
{
    public class SubscriptionViewModel
    {
        public int Id { get; set; }
        public Customer Customer { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public int DayOfMonth { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public string StatusText { get; set; }
        public string Notes { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? NextPaymentDate { get; set; }

        // Added property for formatted amount
        public string AmountFormatted => $"{Amount:N2} {Currency}";

        // For filtering and sorting
        public string SearchText => $"{Customer?.Name} {Notes}".ToLower();

        // Default parameterless constructor
        public SubscriptionViewModel() { }

        // Constructor taking a MonthlySubscription
        public SubscriptionViewModel(MonthlySubscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            Id = subscription.Id;
            Customer = subscription.Customer;
            Amount = subscription.Amount;
            Currency = subscription.Currency;
            DayOfMonth = subscription.DayOfMonth;
            StartDate = subscription.StartDate;
            EndDate = subscription.EndDate;
            IsActive = subscription.IsActive;
            StatusText = subscription.IsActive ? "نشط" : "غير نشط";
            Notes = subscription.Notes;

            // Calculate last payment date from the collection
            LastPaymentDate = subscription.Payments?.Count > 0
                ? subscription.Payments.Max(p => p.PaymentDate)
                : null;

            // Calculate next payment date
            NextPaymentDate = CalculateNextPaymentDate(subscription);
        }

        // Static method for creating from model
        public static SubscriptionViewModel FromModel(MonthlySubscription subscription)
        {
            return new SubscriptionViewModel(subscription);
        }

        private static DateTime? CalculateNextPaymentDate(MonthlySubscription subscription)
        {
            if (!subscription.IsActive || (subscription.EndDate.HasValue && subscription.EndDate.Value < DateTime.Today))
                return null;

            var today = DateTime.Today;
            var lastPayment = subscription.Payments?.OrderByDescending(p => p.PeriodEndDate).FirstOrDefault();

            if (lastPayment != null)
            {
                // Calculate based on last payment period end date
                var nextDueMonth = lastPayment.PeriodEndDate.AddDays(1);
                return new DateTime(nextDueMonth.Year, nextDueMonth.Month, subscription.DayOfMonth);
            }
            else
            {
                // Calculate based on subscription start date
                var dueDay = new DateTime(today.Year, today.Month, subscription.DayOfMonth);
                if (dueDay < today)
                    dueDay = dueDay.AddMonths(1);
                return dueDay;
            }
        }
    }
}