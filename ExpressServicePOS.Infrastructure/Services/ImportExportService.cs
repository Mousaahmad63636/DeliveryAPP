using CsvHelper;
using CsvHelper.Configuration;
using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using ExpressServicePOS.Infrastructure.Services;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace ExpressServicePOS.Infrastructure.Services
{
    public class ImportExportService : BaseService
    {
        public ImportExportService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<ImportExportService> logger)
            : base(dbContextFactory, logger)
        {
        }

        #region Export Methods

        public async Task<bool> ExportCustomersToCsvAsync(string filePath)
        {
            try
            {
                var customers = await ExecuteDbOperationAsync(async (dbContext) =>
                {
                    return await dbContext.Customers.ToListAsync();
                });

                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                }))
                {
                    csv.WriteRecords(customers);
                }

                Logger.LogInformation($"Successfully exported {customers.Count} customers to CSV: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error exporting customers to CSV");
                return false;
            }
        }

        public async Task<bool> ExportCustomersToExcelAsync(string filePath)
        {
            try
            {
                var customers = await ExecuteDbOperationAsync(async (dbContext) =>
                {
                    return await dbContext.Customers.ToListAsync();
                });

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Customers");

                    // Add headers
                    worksheet.Cell(1, 1).Value = "Id";
                    worksheet.Cell(1, 2).Value = "Name";
                    worksheet.Cell(1, 3).Value = "Address";
                    worksheet.Cell(1, 4).Value = "Phone";
                    worksheet.Cell(1, 5).Value = "Notes";

                    // Add data
                    for (int i = 0; i < customers.Count; i++)
                    {
                        worksheet.Cell(i + 2, 1).Value = customers[i].Id;
                        worksheet.Cell(i + 2, 2).Value = customers[i].Name;
                        worksheet.Cell(i + 2, 3).Value = customers[i].Address;
                        worksheet.Cell(i + 2, 4).Value = customers[i].Phone;
                        worksheet.Cell(i + 2, 5).Value = customers[i].Notes;
                    }

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    // Save to file
                    workbook.SaveAs(filePath);
                }

                Logger.LogInformation($"Successfully exported {customers.Count} customers to Excel: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error exporting customers to Excel");
                return false;
            }
        }

        public async Task<bool> ExportOrdersToCsvAsync(string filePath)
        {
            try
            {
                var orders = await ExecuteDbOperationAsync(async (dbContext) =>
                {
                    return await dbContext.Orders
                        .Include(o => o.Customer)
                        .Select(o => new
                        {
                            o.Id,
                            o.OrderNumber,
                            CustomerName = o.Customer.Name,
                            o.OrderDescription,
                            o.Price,
                            o.DeliveryFee,
                            TotalPrice = o.Price + o.DeliveryFee,
                            o.Currency,
                            o.OrderDate,
                            o.DeliveryDate,
                            Status = o.DeliveryStatus.ToString(),
                            o.DriverName,
                            o.Notes,
                            o.IsPaid
                        })
                        .ToListAsync();
                });

                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                }))
                {
                    csv.WriteRecords(orders);
                }

                Logger.LogInformation($"Successfully exported {orders.Count} orders to CSV: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error exporting orders to CSV");
                return false;
            }
        }

        public async Task<bool> ExportOrdersToExcelAsync(string filePath)
        {
            try
            {
                var orders = await ExecuteDbOperationAsync(async (dbContext) =>
                {
                    return await dbContext.Orders
                        .Include(o => o.Customer)
                        .ToListAsync();
                });

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Orders");

                    // Add headers
                    worksheet.Cell(1, 1).Value = "Order #";
                    worksheet.Cell(1, 2).Value = "Customer";
                    worksheet.Cell(1, 3).Value = "Description";
                    worksheet.Cell(1, 4).Value = "Price";
                    worksheet.Cell(1, 5).Value = "Delivery Fee";
                    worksheet.Cell(1, 6).Value = "Total";
                    worksheet.Cell(1, 7).Value = "Currency";
                    worksheet.Cell(1, 8).Value = "Order Date";
                    worksheet.Cell(1, 9).Value = "Status";
                    worksheet.Cell(1, 10).Value = "Driver";
                    worksheet.Cell(1, 11).Value = "Paid";

                    // Add data
                    for (int i = 0; i < orders.Count; i++)
                    {
                        worksheet.Cell(i + 2, 1).Value = orders[i].OrderNumber;
                        worksheet.Cell(i + 2, 2).Value = orders[i].Customer?.Name;
                        worksheet.Cell(i + 2, 3).Value = orders[i].OrderDescription;
                        worksheet.Cell(i + 2, 4).Value = orders[i].Price;
                        worksheet.Cell(i + 2, 5).Value = orders[i].DeliveryFee;
                        worksheet.Cell(i + 2, 6).Value = orders[i].TotalPrice;
                        worksheet.Cell(i + 2, 7).Value = orders[i].Currency;
                        worksheet.Cell(i + 2, 8).Value = orders[i].OrderDate;
                        worksheet.Cell(i + 2, 9).Value = orders[i].DeliveryStatus.ToString();
                        worksheet.Cell(i + 2, 10).Value = orders[i].DriverName;
                        worksheet.Cell(i + 2, 11).Value = orders[i].IsPaid ? "Yes" : "No";
                    }

                    // Format date columns
                    var dateRange = worksheet.Range(2, 8, orders.Count + 1, 8);
                    dateRange.Style.DateFormat.Format = "yyyy-MM-dd";

                    // Format currency columns
                    var currencyRange = worksheet.Range(2, 4, orders.Count + 1, 6);
                    currencyRange.Style.NumberFormat.Format = "#,##0.00";

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    // Save to file
                    workbook.SaveAs(filePath);
                }

                Logger.LogInformation($"Successfully exported {orders.Count} orders to Excel: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error exporting orders to Excel");
                return false;
            }
        }

        #endregion

        #region Import Methods

        public async Task<(bool Success, int Count)> ImportCustomersFromCsvAsync(string filePath)
        {
            try
            {
                using (var reader = new StreamReader(filePath, Encoding.UTF8))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    var customers = csv.GetRecords<Customer>().ToList();

                    return await ExecuteDbOperationAsync(async (dbContext) =>
                    {
                        foreach (var customer in customers)
                        {
                            // Check if customer already exists
                            var existingCustomer = await dbContext.Customers
                                .FirstOrDefaultAsync(c => c.Phone == customer.Phone);

                            if (existingCustomer == null)
                            {
                                // New customer, add to database
                                dbContext.Customers.Add(customer);
                            }
                            else
                            {
                                // Customer exists, update properties
                                existingCustomer.Name = customer.Name;
                                existingCustomer.Address = customer.Address;
                                existingCustomer.Notes = customer.Notes;

                                dbContext.Customers.Update(existingCustomer);
                            }
                        }

                        await dbContext.SaveChangesAsync();

                        Logger.LogInformation($"Successfully imported {customers.Count} customers from CSV: {filePath}");
                        return (true, customers.Count);
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error importing customers from CSV");
                return (false, 0);
            }
        }

        public async Task<(bool Success, int Count)> ImportCustomersFromExcelAsync(string filePath)
        {
            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1); // First worksheet
                    var rows = worksheet.RowsUsed();

                    return await ExecuteDbOperationAsync(async (dbContext) =>
                    {
                        int importedCount = 0;

                        // Skip the header row (row 1)
                        foreach (var row in rows.Skip(1))
                        {
                            string name = row.Cell(2).GetString();
                            string address = row.Cell(3).GetString();
                            string phone = row.Cell(4).GetString();
                            string notes = row.Cell(5).GetString();

                            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(phone))
                                continue;

                            // Check if customer already exists
                            var existingCustomer = await dbContext.Customers
                                .FirstOrDefaultAsync(c => c.Phone == phone);

                            if (existingCustomer == null)
                            {
                                // New customer, add to database
                                dbContext.Customers.Add(new Customer
                                {
                                    Name = name,
                                    Address = address,
                                    Phone = phone,
                                    Notes = notes
                                });

                                importedCount++;
                            }
                            else
                            {
                                // Customer exists, update properties
                                existingCustomer.Name = name;
                                existingCustomer.Address = address;
                                existingCustomer.Notes = notes;

                                dbContext.Customers.Update(existingCustomer);
                                importedCount++;
                            }
                        }

                        await dbContext.SaveChangesAsync();

                        Logger.LogInformation($"Successfully imported {importedCount} customers from Excel: {filePath}");
                        return (true, importedCount);
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error importing customers from Excel");
                return (false, 0);
            }
        }

        #endregion
    }
}