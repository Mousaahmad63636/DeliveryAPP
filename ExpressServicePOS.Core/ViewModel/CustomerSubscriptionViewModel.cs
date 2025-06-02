// ExpressServicePOS.Core/ViewModels/CustomerSubscriptionViewModel.cs
using ExpressServicePOS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpressServicePOS.Core.ViewModels
{
    public class CustomerSubscriptionViewModel
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string FormattedAmount => Currency == "USD" ? $"{Amount:N2} $" : $"{Amount:N0} ل.ل";
        public int DayOfMonth { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public string StatusText => IsActive ? "نشط" : "غير نشط";
        public string Notes { get; set; }
        public decimal TotalPaid { get; set; }
        public string FormattedTotalPaid => Currency == "USD" ? $"{TotalPaid:N2} $" : $"{TotalPaid:N0} ل.ل";
        public int PaymentsCount { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? NextPaymentDueDate { get; set; }
        public bool PaymentIsDue { get; set; }
        public int DaysTillNextPayment { get; set; }

        public static CustomerSubscriptionViewModel FromModel(MonthlySubscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            var lastPayment = subscription.Payments?.OrderByDescending(p => p.PaymentDate).FirstOrDefault();
            var nextPaymentDueDate = CalculateNextPaymentDueDate(subscription, lastPayment);
            var daysUntilNextPayment = nextPaymentDueDate.HasValue ?
                (int)(nextPaymentDueDate.Value - DateTime.Today).TotalDays : 0;

            return new CustomerSubscriptionViewModel
            {
                Id = subscription.Id,
                CustomerId = subscription.CustomerId,
                CustomerName = subscription.Customer?.Name ?? "Unknown",
                CustomerPhone = subscription.Customer?.Phone ?? "",
                Amount = subscription.Amount,
                Currency = subscription.Currency,
                DayOfMonth = subscription.DayOfMonth,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                IsActive = subscription.IsActive,
                Notes = subscription.Notes,
                TotalPaid = subscription.Payments?.Sum(p => p.Amount) ?? 0m,
                PaymentsCount = subscription.Payments?.Count ?? 0,
                LastPaymentDate = lastPayment?.PaymentDate,
                NextPaymentDueDate = nextPaymentDueDate,
                PaymentIsDue = CheckIfPaymentIsDue(nextPaymentDueDate),
                DaysTillNextPayment = daysUntilNextPayment
            };
        }

        private static DateTime? CalculateNextPaymentDueDate(MonthlySubscription subscription, SubscriptionPayment lastPayment)
        {
            if (!subscription.IsActive || (subscription.EndDate.HasValue && subscription.EndDate.Value < DateTime.Today))
                return null;

            if (lastPayment != null)
            {
                // Calculate next payment date based on the last payment's period end
                var periodEnd = lastPayment.PeriodEndDate;
                return new DateTime(periodEnd.Year, periodEnd.Month, subscription.DayOfMonth)
                    .AddMonths(1);
            }
            else
            {
                // No previous payments, calculate from start date
                var nextDueDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, subscription.DayOfMonth);
                if (nextDueDate < DateTime.Today)
                {
                    nextDueDate = nextDueDate.AddMonths(1);
                }
                return nextDueDate;
            }
        }

        private static bool CheckIfPaymentIsDue(DateTime? nextPaymentDueDate)
        {
            if (!nextPaymentDueDate.HasValue)
                return false;

            return nextPaymentDueDate.Value <= DateTime.Today.AddDays(7);
        }
    }
}