using ExpressServicePOS.Core.ViewModels;
using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpressServicePOS.Infrastructure.Services
{
    public class ReportService : BaseService
    {
        public ReportService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<ReportService> logger)
            : base(dbContextFactory, logger)
        {
        }

        public async Task<List<CustomerStatementViewModel>> GenerateCustomerStatementAsync(
            int customerId,
            DateTime startDate,
            DateTime endDate)
        {
            try
            {
                return await ExecuteDbOperationAsync(async (dbContext) =>
                {
                    var statement = await dbContext.Orders
                        .Where(o => o.CustomerId == customerId &&
                                    o.OrderDate >= startDate &&
                                    o.OrderDate <= endDate)
                        .Include(o => o.Customer)
                        .Select(o => new CustomerStatementViewModel
                        {
                            OrderNumber = o.OrderNumber,
                            OrderDescription = o.OrderDescription,
                            OrderDate = o.OrderDate,
                            DeliveryDate = o.DeliveryDate,
                            TotalAmount = o.TotalPrice,
                            Status = o.DeliveryStatus,
                            CustomerName = o.Customer.Name,
                            CustomerPhone = o.Customer.Phone
                        })
                        .ToListAsync();

                    return statement;
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating customer statement");
                throw;
            }
        }
    }
}