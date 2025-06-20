﻿// ExpressServicePOS.Infrastructure/Services/SubscriptionService.cs
using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpressServicePOS.Infrastructure.Services
{
    public class SubscriptionService : BaseService
    {
        public SubscriptionService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<SubscriptionService> logger)
            : base(dbContextFactory, logger)
        {
        }

        public async Task<List<MonthlySubscription>> GetSubscriptionsAsync(bool? filterActive = null)
        {
            return await ExecuteDbOperationAsync(async (dbContext) =>
            {
                IQueryable<MonthlySubscription> query = dbContext.MonthlySubscriptions
                    .Include(s => s.Customer)
                    .Include(s => s.Payments);

                if (filterActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == filterActive.Value);
                }

                return await query.OrderBy(s => s.Customer.Name).ToListAsync();
            });
        }
        // Add these methods to ExpressServicePOS.Infrastructure/Services/SubscriptionService.cs

        public async Task<MonthlySubscription> GetActiveSubscriptionForCustomerAsync(int customerId)
        {
            return await ExecuteDbOperationAsync(async (dbContext) =>
            {
                var today = DateTime.Today;
                return await dbContext.MonthlySubscriptions
                    .Include(s => s.Payments)
                    .Where(s => s.CustomerId == customerId &&
                                s.IsActive &&
                                (s.EndDate == null || s.EndDate > today))
                    .FirstOrDefaultAsync();
            });
        }

        public class SubscriptionStatistics
        {
            public int TotalSubscriptions { get; set; }
            public int ActiveSubscriptions { get; set; }
            public int InactiveSubscriptions { get; set; }
            public decimal MonthlyRevenueUSD { get; set; }
            public decimal MonthlyRevenueLBP { get; set; }
            public decimal CurrentMonthPaymentsUSD { get; set; }
            public decimal CurrentMonthPaymentsLBP { get; set; }
        }

        public async Task<SubscriptionStatistics> GetSubscriptionStatisticsAsync()
        {
            return await ExecuteDbOperationAsync(async (dbContext) =>
            {
                var today = DateTime.Today;
                var currentMonth = new DateTime(today.Year, today.Month, 1);
                var nextMonth = currentMonth.AddMonths(1);

                var statistics = new SubscriptionStatistics
                {
                    TotalSubscriptions = await dbContext.MonthlySubscriptions.CountAsync(),
                    ActiveSubscriptions = await dbContext.MonthlySubscriptions
                        .CountAsync(s => s.IsActive && (s.EndDate == null || s.EndDate > today)),
                    InactiveSubscriptions = await dbContext.MonthlySubscriptions
                        .CountAsync(s => !s.IsActive || (s.EndDate != null && s.EndDate <= today))
                };

                // Calculate total monthly revenue from active subscriptions
                var activeSubscriptions = await dbContext.MonthlySubscriptions
                    .Where(s => s.IsActive && (s.EndDate == null || s.EndDate > today))
                    .ToListAsync();

                statistics.MonthlyRevenueUSD = activeSubscriptions
                    .Where(s => s.Currency == "USD")
                    .Sum(s => s.Amount);

                statistics.MonthlyRevenueLBP = activeSubscriptions
                    .Where(s => s.Currency == "LBP")
                    .Sum(s => s.Amount);

                // Calculate current month payments
                var currentMonthPayments = await dbContext.SubscriptionPayments
                    .Where(p => p.PaymentDate >= currentMonth && p.PaymentDate < nextMonth)
                    .ToListAsync();

                statistics.CurrentMonthPaymentsUSD = currentMonthPayments
                    .Where(p => p.Currency == "USD")
                    .Sum(p => p.Amount);

                statistics.CurrentMonthPaymentsLBP = currentMonthPayments
                    .Where(p => p.Currency == "LBP")
                    .Sum(p => p.Amount);

                return statistics;
            });
        }
        public async Task<List<MonthlySubscription>> GetDueSubscriptionsAsync(int daysAhead = 7)
        {
            return await ExecuteDbOperationAsync(async (dbContext) =>
            {
                var today = DateTime.Today;
                var dueDate = today.AddDays(daysAhead);

                // Get all active subscriptions
                var activeSubscriptions = await dbContext.MonthlySubscriptions
                    .Include(s => s.Customer)
                    .Include(s => s.Payments)
                    .Where(s => s.IsActive && (s.EndDate == null || s.EndDate > today))
                    .ToListAsync();

                // Filter subscriptions that have a payment due within the specified period
                var dueSubscriptions = new List<MonthlySubscription>();

                foreach (var subscription in activeSubscriptions)
                {
                    // Find the most recent payment
                    var lastPayment = subscription.Payments
                        .OrderByDescending(p => p.PeriodEndDate)
                        .FirstOrDefault();

                    DateTime nextDueDate;

                    if (lastPayment != null)
                    {
                        // Calculate next due date based on the last payment's period end
                        var lastPeriodEnd = lastPayment.PeriodEndDate;
                        nextDueDate = new DateTime(lastPeriodEnd.Year, lastPeriodEnd.Month, subscription.DayOfMonth);

                        // If the calculated day is before the period end, move to the next month
                        if (nextDueDate <= lastPeriodEnd)
                        {
                            nextDueDate = nextDueDate.AddMonths(1);
                        }
                    }
                    else
                    {
                        // No previous payments, use the start date
                        nextDueDate = new DateTime(subscription.StartDate.Year, subscription.StartDate.Month, subscription.DayOfMonth);

                        // If the calculated day is before the start date, move to the next month
                        if (nextDueDate < subscription.StartDate)
                        {
                            nextDueDate = nextDueDate.AddMonths(1);
                        }
                    }

                    // Check if the next due date is within the specified range
                    if (nextDueDate <= dueDate)
                    {
                        dueSubscriptions.Add(subscription);
                    }
                }

                return dueSubscriptions;
            });
        }

        public async Task<MonthlySubscription> CreateSubscriptionAsync(MonthlySubscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            return await ExecuteDbOperationAsync(async (dbContext) =>
            {
                // Validate day of month
                if (subscription.DayOfMonth < 1 || subscription.DayOfMonth > 31)
                {
                    throw new ArgumentException("Day of month must be between 1 and 31", nameof(subscription.DayOfMonth));
                }

                // Set default start date to today if not specified
                if (subscription.StartDate == default)
                {
                    subscription.StartDate = DateTime.Today;
                }

                dbContext.MonthlySubscriptions.Add(subscription);
                await dbContext.SaveChangesAsync();
                return subscription;
            });
        }

        public async Task<bool> UpdateSubscriptionAsync(MonthlySubscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            return await ExecuteDbOperationAsync(async (dbContext) =>
            {
                // Validate day of month
                if (subscription.DayOfMonth < 1 || subscription.DayOfMonth > 31)
                {
                    throw new ArgumentException("Day of month must be between 1 and 31", nameof(subscription.DayOfMonth));
                }

                var existingSubscription = await dbContext.MonthlySubscriptions.FindAsync(subscription.Id);
                if (existingSubscription == null)
                {
                    return false;
                }

                // Update properties
                existingSubscription.Amount = subscription.Amount;
                existingSubscription.DayOfMonth = subscription.DayOfMonth;
                existingSubscription.IsActive = subscription.IsActive;
                existingSubscription.Notes = subscription.Notes;
                existingSubscription.Currency = subscription.Currency;
                existingSubscription.EndDate = subscription.EndDate;

                dbContext.MonthlySubscriptions.Update(existingSubscription);
                await dbContext.SaveChangesAsync();
                return true;
            });
        }

        public async Task<bool> SetSubscriptionActiveStatusAsync(int subscriptionId, bool active)
        {
            return await ExecuteDbOperationAsync(async (dbContext) =>
            {
                var subscription = await dbContext.MonthlySubscriptions.FindAsync(subscriptionId);
                if (subscription == null)
                {
                    return false;
                }

                subscription.IsActive = active;
                if (!active && subscription.EndDate == null)
                {
                    subscription.EndDate = DateTime.Today;
                }
                else if (active && subscription.EndDate != null)
                {
                    subscription.EndDate = null;
                }

                dbContext.MonthlySubscriptions.Update(subscription);
                await dbContext.SaveChangesAsync();
                return true;
            });
        }

        public async Task<SubscriptionPayment> RecordPaymentAsync(SubscriptionPayment payment)
        {
            if (payment == null)
                throw new ArgumentNullException(nameof(payment));

            return await ExecuteDbOperationAsync(async (dbContext) =>
            {
                // Validate the subscription exists
                var subscription = await dbContext.MonthlySubscriptions
                    .Include(s => s.Payments)
                    .FirstOrDefaultAsync(s => s.Id == payment.SubscriptionId);

                if (subscription == null)
                {
                    throw new ArgumentException($"Subscription with ID {payment.SubscriptionId} not found", nameof(payment.SubscriptionId));
                }

                // Set default payment date to today if not specified
                if (payment.PaymentDate == default)
                {
                    payment.PaymentDate = DateTime.Today;
                }

                // If period dates are not set, calculate them based on subscription settings
                if (payment.PeriodStartDate == default || payment.PeriodEndDate == default)
                {
                    // Find the last payment to determine the period
                    var lastPayment = subscription.Payments
                        .OrderByDescending(p => p.PeriodEndDate)
                        .FirstOrDefault();

                    if (lastPayment != null)
                    {
                        // Next period starts after the last period
                        payment.PeriodStartDate = lastPayment.PeriodEndDate.AddDays(1);
                    }
                    else
                    {
                        // First payment, period starts from subscription start date
                        payment.PeriodStartDate = subscription.StartDate;
                    }

                    // Calculate period end date (typically one month after start date)
                    payment.PeriodEndDate = CalculateNextPeriodEndDate(payment.PeriodStartDate, subscription.DayOfMonth);
                }

                // Set amount from subscription if not specified
                if (payment.Amount == 0)
                {
                    payment.Amount = subscription.Amount;
                }

                // Set currency from subscription if not specified
                if (string.IsNullOrEmpty(payment.Currency))
                {
                    payment.Currency = subscription.Currency;
                }

                dbContext.SubscriptionPayments.Add(payment);
                await dbContext.SaveChangesAsync();
                return payment;
            });
        }

        public async Task<List<SubscriptionPayment>> GetPaymentsForSubscriptionAsync(int subscriptionId)
        {
            return await ExecuteDbOperationAsync(async (dbContext) =>
            {
                return await dbContext.SubscriptionPayments
                    .Where(p => p.SubscriptionId == subscriptionId)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();
            });
        }

        // Helper method to calculate the end date of a period
        private DateTime CalculateNextPeriodEndDate(DateTime startDate, int dayOfMonth)
        {
            // Move to the next month
            DateTime nextMonth = startDate.AddMonths(1);

            // Calculate the end date (day before the next payment due date)
            DateTime endDate = new DateTime(nextMonth.Year, nextMonth.Month, dayOfMonth).AddDays(-1);

            // If the calculated end date is in the same month as the start date,
            // it means the dayOfMonth is later in the month than the start date day
            if (endDate.Month == startDate.Month)
            {
                endDate = endDate.AddMonths(1);
            }

            return endDate;
        }
    }
}