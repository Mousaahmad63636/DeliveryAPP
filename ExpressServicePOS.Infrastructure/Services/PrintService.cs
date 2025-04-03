// File: ExpressServicePOS.Infrastructure.Services/PrintService.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Core.ViewModels;
using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpressServicePOS.Infrastructure.Services
{
    public class PrintService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<PrintService> _logger;
        private readonly ReceiptService _receiptService;

        public PrintService(AppDbContext dbContext, ILogger<PrintService> logger, ReceiptService receiptService)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
        }

        /// <summary>
        /// Creates and displays a printable receipt for an order.
        /// </summary>
        /// <param name="orderId">The ID of the order to print.</param>
        /// <returns>A FlowDocument containing the formatted receipt.</returns>
        public async Task<FlowDocument> CreateOrderReceiptAsync(int orderId)
        {
            try
            {
                // Get order with customer details
                var order = await _dbContext.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Driver)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    _logger.LogWarning($"Order with ID {orderId} not found for printing receipt");
                    throw new ArgumentException($"Order with ID {orderId} not found");
                }

                // Get company profile for receipt header
                var companyProfile = await _dbContext.CompanyProfile.FirstOrDefaultAsync();
                if (companyProfile == null)
                {
                    companyProfile = new CompanyProfile
                    {
                        CompanyName = "Express Service",
                        Address = "Beirut, Lebanon"
                    };
                }

                // Create a FlowDocument
                var document = new FlowDocument
                {
                    FontFamily = new System.Windows.Media.FontFamily("Arial"),
                    FontSize = 12,
                    PagePadding = new Thickness(50)
                };

                // Create sections
                Section headerSection = new Section();
                Section contentSection = new Section();
                Section footerSection = new Section();

                // Header styling
                headerSection.FontSize = 16;
                headerSection.TextAlignment = TextAlignment.Center;

                // Add company info
                Paragraph companyPara = new Paragraph(new Run(companyProfile.CompanyName))
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = 18
                };
                headerSection.Blocks.Add(companyPara);

                if (!string.IsNullOrEmpty(companyProfile.Address))
                {
                    headerSection.Blocks.Add(new Paragraph(new Run(companyProfile.Address)));
                }

                if (!string.IsNullOrEmpty(companyProfile.Phone))
                {
                    headerSection.Blocks.Add(new Paragraph(new Run($"Tel: {companyProfile.Phone}")));
                }

                // Add receipt title
                Paragraph receiptTitle = new Paragraph(new Run("Receipt"))
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Margin = new Thickness(0, 10, 0, 10)
                };
                headerSection.Blocks.Add(receiptTitle);

                // Order details
                Paragraph orderDetails = new Paragraph()
                {
                    TextAlignment = TextAlignment.Left,
                    FontSize = 12
                };
                orderDetails.Inlines.Add(new Run($"Order #: {order.OrderNumber}") { FontWeight = FontWeights.Bold });
                orderDetails.Inlines.Add(new LineBreak());
                orderDetails.Inlines.Add(new Run($"Date: {order.OrderDate:yyyy-MM-dd HH:mm}"));
                orderDetails.Inlines.Add(new LineBreak());
                orderDetails.Inlines.Add(new Run($"Customer: {order.Customer?.Name}"));
                orderDetails.Inlines.Add(new LineBreak());
                orderDetails.Inlines.Add(new Run($"Address: {order.Customer?.Address}"));
                orderDetails.Inlines.Add(new LineBreak());
                orderDetails.Inlines.Add(new Run($"Phone: {order.Customer?.Phone}"));
                orderDetails.Inlines.Add(new LineBreak());

                if (order.Driver != null)
                {
                    orderDetails.Inlines.Add(new Run($"Driver: {order.Driver.Name}"));
                    orderDetails.Inlines.Add(new LineBreak());
                }
                else if (!string.IsNullOrEmpty(order.DriverName))
                {
                    orderDetails.Inlines.Add(new Run($"Driver: {order.DriverName}"));
                    orderDetails.Inlines.Add(new LineBreak());
                }

                contentSection.Blocks.Add(orderDetails);

                // Order description
                Paragraph descriptionPara = new Paragraph(new Run($"Description: {order.OrderDescription}"))
                {
                    Margin = new Thickness(0, 10, 0, 10)
                };
                contentSection.Blocks.Add(descriptionPara);

                // Add line
                Paragraph line = new Paragraph(new Run("----------------------------------------"))
                {
                    TextAlignment = TextAlignment.Center
                };
                contentSection.Blocks.Add(line);

                // Order financial details
                Paragraph financialDetails = new Paragraph()
                {
                    TextAlignment = TextAlignment.Right,
                    FontSize = 12
                };
                financialDetails.Inlines.Add(new Run($"Price: {order.Price:N2} {order.Currency}"));
                financialDetails.Inlines.Add(new LineBreak());
                financialDetails.Inlines.Add(new Run($"Delivery Fee: {order.DeliveryFee:N2} {order.Currency}"));
                financialDetails.Inlines.Add(new LineBreak());
                financialDetails.Inlines.Add(new Run($"Total: {order.TotalPrice:N2} {order.Currency}") { FontWeight = FontWeights.Bold });
                contentSection.Blocks.Add(financialDetails);

                // Add status
                Paragraph statusPara = new Paragraph(new Run($"Status: {order.DeliveryStatus}"))
                {
                    Margin = new Thickness(0, 10, 0, 0)
                };
                contentSection.Blocks.Add(statusPara);

                // Payment status
                Paragraph paymentPara = new Paragraph(new Run($"Payment: {(order.IsPaid ? "Paid" : "Not Paid")}"));
                contentSection.Blocks.Add(paymentPara);

                // Footer with thank you message
                Paragraph footerPara = new Paragraph(new Run(companyProfile.ReceiptFooterText))
                {
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0),
                    FontStyle = FontStyles.Italic
                };
                footerSection.Blocks.Add(footerPara);

                // Add sections to document
                document.Blocks.Add(headerSection);
                document.Blocks.Add(contentSection);
                document.Blocks.Add(footerSection);

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating receipt for order ID {orderId}");
                throw;
            }
        }

        /// <summary>
        /// Creates and displays a printable customer statement for a given period.
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <param name="startDate">The start date of the period</param>
        /// <param name="endDate">The end date of the period</param>
        /// <returns>A FlowDocument containing the formatted customer statement</returns>
        public async Task<FlowDocument> CreateCustomerStatementAsync(int customerId, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Get customer
                var customer = await _dbContext.Customers.FindAsync(customerId);
                if (customer == null)
                {
                    _logger.LogWarning($"Customer with ID {customerId} not found for printing statement");
                    throw new ArgumentException($"Customer with ID {customerId} not found");
                }

                // Get orders for the period
                var orders = await _dbContext.Orders
                    .Where(o => o.CustomerId == customerId && o.OrderDate >= startDate && o.OrderDate <= endDate)
                    .OrderBy(o => o.OrderDate)
                    .ToListAsync();

                // Get company profile
                var companyProfile = await _dbContext.CompanyProfile.FirstOrDefaultAsync();
                if (companyProfile == null)
                {
                    companyProfile = new CompanyProfile
                    {
                        CompanyName = "Express Service",
                        Address = "Beirut, Lebanon"
                    };
                }

                // Create a FlowDocument
                var document = new FlowDocument
                {
                    FontFamily = new System.Windows.Media.FontFamily("Arial"),
                    FontSize = 12,
                    PagePadding = new Thickness(50)
                };

                // Header section
                Section headerSection = new Section()
                {
                    TextAlignment = TextAlignment.Center
                };

                Paragraph companyPara = new Paragraph(new Run(companyProfile.CompanyName))
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = 18
                };
                headerSection.Blocks.Add(companyPara);

                // Add company address and phone if available
                if (!string.IsNullOrEmpty(companyProfile.Address))
                {
                    headerSection.Blocks.Add(new Paragraph(new Run(companyProfile.Address)));
                }

                if (!string.IsNullOrEmpty(companyProfile.Phone))
                {
                    headerSection.Blocks.Add(new Paragraph(new Run($"Tel: {companyProfile.Phone}")));
                }

                // Add statement title
                Paragraph statementTitle = new Paragraph(new Run("Customer Statement"))
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = 16,
                    Margin = new Thickness(0, 10, 0, 10)
                };
                headerSection.Blocks.Add(statementTitle);

                // Customer information section
                Section customerSection = new Section()
                {
                    TextAlignment = TextAlignment.Left
                };

                Paragraph customerInfo = new Paragraph();
                customerInfo.Inlines.Add(new Run("Customer: ") { FontWeight = FontWeights.Bold });
                customerInfo.Inlines.Add(new Run(customer.Name));
                customerInfo.Inlines.Add(new LineBreak());
                customerInfo.Inlines.Add(new Run("Address: ") { FontWeight = FontWeights.Bold });
                customerInfo.Inlines.Add(new Run(customer.Address));
                customerInfo.Inlines.Add(new LineBreak());
                customerInfo.Inlines.Add(new Run("Phone: ") { FontWeight = FontWeights.Bold });
                customerInfo.Inlines.Add(new Run(customer.Phone));
                customerInfo.Inlines.Add(new LineBreak());
                customerInfo.Inlines.Add(new Run("Period: ") { FontWeight = FontWeights.Bold });
                customerInfo.Inlines.Add(new Run($"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"));
                customerSection.Blocks.Add(customerInfo);

                // Orders table section
                Section ordersSection = new Section();

                Paragraph tableHeader = new Paragraph()
                {
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 20, 0, 5)
                };

                // Add table header
                Table table = new Table();
                table.CellSpacing = 0;
                table.BorderBrush = System.Windows.Media.Brushes.Black;
                table.BorderThickness = new Thickness(1);

                // Define columns
                table.Columns.Add(new TableColumn() { Width = new GridLength(80) });  // Order #
                table.Columns.Add(new TableColumn() { Width = new GridLength(150) }); // Description
                table.Columns.Add(new TableColumn() { Width = new GridLength(100) }); // Date
                table.Columns.Add(new TableColumn() { Width = new GridLength(100) }); // Status
                table.Columns.Add(new TableColumn() { Width = new GridLength(100) }); // Amount
                table.Columns.Add(new TableColumn() { Width = new GridLength(100) }); // Total
                table.Columns.Add(new TableColumn() { Width = new GridLength(80) });  // Paid

                // Add header row
                TableRowGroup headerRowGroup = new TableRowGroup();
                TableRow headerRow = new TableRow();
                headerRow.Background = System.Windows.Media.Brushes.LightGray;

                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Order #"))));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Description"))));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Date"))));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Status"))));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Amount"))));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Total"))));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Paid"))));

                headerRowGroup.Rows.Add(headerRow);
                table.RowGroups.Add(headerRowGroup);

                // Add data rows
                TableRowGroup dataRowGroup = new TableRowGroup();

                decimal totalAmount = 0;
                string mainCurrency = "USD"; // Default currency

                foreach (var order in orders)
                {
                    TableRow row = new TableRow();

                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.OrderNumber))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.OrderDescription))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.OrderDate.ToString("yyyy-MM-dd")))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.DeliveryStatus.ToString()))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{order.Price:N2} {order.Currency}"))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{order.TotalPrice:N2} {order.Currency}"))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.IsPaid ? "Yes" : "No"))));

                    dataRowGroup.Rows.Add(row);

                    // Accumulate total (simplified - assuming same currency)
                    mainCurrency = order.Currency;
                    totalAmount += order.TotalPrice;
                }

                table.RowGroups.Add(dataRowGroup);

                // Add summary row
                TableRowGroup summaryRowGroup = new TableRowGroup();
                TableRow summaryRow = new TableRow();
                summaryRow.Background = System.Windows.Media.Brushes.LightGray;

                // Merge cells for "Total" label (spanning 5 columns)
                TableCell totalLabelCell = new TableCell(new Paragraph(new Run("Total:") { FontWeight = FontWeights.Bold }))
                {
                    ColumnSpan = 5,
                    TextAlignment = TextAlignment.Right
                };

                TableCell totalValueCell = new TableCell(new Paragraph(new Run($"{totalAmount:N2} {mainCurrency}") { FontWeight = FontWeights.Bold }));

                // Empty cell for "Paid" column
                TableCell emptyCell = new TableCell(new Paragraph(new Run("")));

                summaryRow.Cells.Add(totalLabelCell);
                summaryRow.Cells.Add(totalValueCell);
                summaryRow.Cells.Add(emptyCell);

                summaryRowGroup.Rows.Add(summaryRow);
                table.RowGroups.Add(summaryRowGroup);

                // Add table to document
                ordersSection.Blocks.Add(table);

                // Footer section
                Section footerSection = new Section();
                Paragraph footerPara = new Paragraph(new Run("Thank you for your business"))
                {
                    TextAlignment = TextAlignment.Center,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 20, 0, 0)
                };
                footerSection.Blocks.Add(footerPara);

                // Add all sections to document
                document.Blocks.Add(headerSection);
                document.Blocks.Add(customerSection);
                document.Blocks.Add(ordersSection);
                document.Blocks.Add(footerSection);

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating statement for customer ID {customerId}");
                throw;
            }
        }

        /// <summary>
        /// Creates a printable sales report for a given period.
        /// </summary>
        /// <param name="startDate">The start date of the period</param>
        /// <param name="endDate">The end date of the period</param>
        /// <returns>A FlowDocument containing the formatted sales report</returns>
        public async Task<FlowDocument> CreateSalesReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Get orders for the period
                var orders = await _dbContext.Orders
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                    .Include(o => o.Customer)
                    .OrderBy(o => o.OrderDate)
                    .ToListAsync();

                // Get company profile
                var companyProfile = await _dbContext.CompanyProfile.FirstOrDefaultAsync();
                if (companyProfile == null)
                {
                    companyProfile = new CompanyProfile
                    {
                        CompanyName = "Express Service",
                        Address = "Beirut, Lebanon"
                    };
                }

                // Create a FlowDocument
                var document = new FlowDocument
                {
                    FontFamily = new System.Windows.Media.FontFamily("Arial"),
                    FontSize = 12,
                    PagePadding = new Thickness(50)
                };

                // Header section
                Section headerSection = new Section()
                {
                    TextAlignment = TextAlignment.Center
                };

                Paragraph companyPara = new Paragraph(new Run(companyProfile.CompanyName))
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = 18
                };
                headerSection.Blocks.Add(companyPara);

                // Add company address and phone if available
                if (!string.IsNullOrEmpty(companyProfile.Address))
                {
                    headerSection.Blocks.Add(new Paragraph(new Run(companyProfile.Address)));
                }

                if (!string.IsNullOrEmpty(companyProfile.Phone))
                {
                    headerSection.Blocks.Add(new Paragraph(new Run($"Tel: {companyProfile.Phone}")));
                }

                // Add report title
                Paragraph reportTitle = new Paragraph(new Run("Sales Report"))
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = 16,
                    Margin = new Thickness(0, 10, 0, 10)
                };
                headerSection.Blocks.Add(reportTitle);

                // Report period
                Paragraph periodPara = new Paragraph(new Run($"Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"))
                {
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                headerSection.Blocks.Add(periodPara);

                // Summary section
                Section summarySection = new Section();

                // Calculate summary data
                int totalOrders = orders.Count;
                int pendingOrders = orders.Count(o => o.DeliveryStatus == DeliveryStatus.Pending);
                int inTransitOrders = orders.Count(o => o.DeliveryStatus == DeliveryStatus.InTransit);
                int deliveredOrders = orders.Count(o => o.DeliveryStatus == DeliveryStatus.Delivered);
                int cancelledOrders = orders.Count(o => o.DeliveryStatus == DeliveryStatus.Cancelled);
                int paidOrders = orders.Count(o => o.IsPaid);

                // Separate by currency
                var usdOrders = orders.Where(o => o.Currency == "USD").ToList();
                var lbpOrders = orders.Where(o => o.Currency == "LBP").ToList();

                decimal totalUSD = usdOrders.Sum(o => o.TotalPrice);
                decimal totalLBP = lbpOrders.Sum(o => o.TotalPrice);

                Paragraph summaryPara = new Paragraph();
                summaryPara.Inlines.Add(new Run("SUMMARY") { FontWeight = FontWeights.Bold });
                summaryPara.Inlines.Add(new LineBreak());
                summaryPara.Inlines.Add(new Run($"Total Orders: {totalOrders}"));
                summaryPara.Inlines.Add(new LineBreak());
                summaryPara.Inlines.Add(new Run($"Pending: {pendingOrders}"));
                summaryPara.Inlines.Add(new LineBreak());
                summaryPara.Inlines.Add(new Run($"In Transit: {inTransitOrders}"));
                summaryPara.Inlines.Add(new LineBreak());
                summaryPara.Inlines.Add(new Run($"Delivered: {deliveredOrders}"));
                summaryPara.Inlines.Add(new LineBreak());
                summaryPara.Inlines.Add(new Run($"Cancelled: {cancelledOrders}"));
                summaryPara.Inlines.Add(new LineBreak());
                summaryPara.Inlines.Add(new Run($"Paid Orders: {paidOrders} ({(totalOrders > 0 ? (double)paidOrders / totalOrders * 100 : 0):F1}%)"));
                summaryPara.Inlines.Add(new LineBreak());
                summaryPara.Inlines.Add(new LineBreak());
                summaryPara.Inlines.Add(new Run("REVENUE") { FontWeight = FontWeights.Bold });
                summaryPara.Inlines.Add(new LineBreak());
                summaryPara.Inlines.Add(new Run($"Total Revenue (USD): {totalUSD:N2} USD"));
                summaryPara.Inlines.Add(new LineBreak());
                summaryPara.Inlines.Add(new Run($"Total Revenue (LBP): {totalLBP:N2} LBP"));

                summarySection.Blocks.Add(summaryPara);

                // Orders table section
                Section ordersSection = new Section();

                Paragraph ordersHeader = new Paragraph(new Run("ORDER DETAILS"))
                {
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 20, 0, 10)
                };
                ordersSection.Blocks.Add(ordersHeader);

                // Create table
                Table table = new Table();
                table.CellSpacing = 0;
                table.BorderBrush = System.Windows.Media.Brushes.Black;
                table.BorderThickness = new Thickness(1);

                // Define columns
                table.Columns.Add(new TableColumn() { Width = new GridLength(80) });  // Order #
                table.Columns.Add(new TableColumn() { Width = new GridLength(120) }); // Date
                table.Columns.Add(new TableColumn() { Width = new GridLength(150) }); // Customer
                table.Columns.Add(new TableColumn() { Width = new GridLength(100) }); // Status
                table.Columns.Add(new TableColumn() { Width = new GridLength(100) }); // Amount
                table.Columns.Add(new TableColumn() { Width = new GridLength(80) });  // Currency
                table.Columns.Add(new TableColumn() { Width = new GridLength(80) });  // Paid

                // Add header row
                TableRowGroup headerRowGroup = new TableRowGroup();
                TableRow headerRow = new TableRow();
                headerRow.Background = System.Windows.Media.Brushes.LightGray;

                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Order #"))));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Date"))));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Customer"))));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Status"))));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Amount"))));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Currency"))));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Paid"))));

                headerRowGroup.Rows.Add(headerRow);
                table.RowGroups.Add(headerRowGroup);

                // Add data rows
                TableRowGroup dataRowGroup = new TableRowGroup();

                foreach (var order in orders)
                {
                    TableRow row = new TableRow();

                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.OrderNumber))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.OrderDate.ToString("yyyy-MM-dd")))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.Customer?.Name ?? ""))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.DeliveryStatus.ToString()))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{order.TotalPrice:N2}"))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.Currency))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.IsPaid ? "Yes" : "No"))));

                    dataRowGroup.Rows.Add(row);
                }

                table.RowGroups.Add(dataRowGroup);
                ordersSection.Blocks.Add(table);

                // Add all sections to document
                document.Blocks.Add(headerSection);
                document.Blocks.Add(summarySection);
                document.Blocks.Add(ordersSection);

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sales report");
                throw;
            }
        }

        /// <summary>
        /// Creates and displays a printed receipt in the Express Service format.
        /// </summary>
        /// <param name="orderId">The ID of the order to print.</param>
        /// <returns>A FlowDocument containing the formatted receipt.</returns>
        public async Task<FlowDocument> CreateExpressServiceReceiptAsync(int orderId)
        {
            try
            {
                // Get order with customer details
                var order = await _dbContext.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Driver)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    _logger.LogWarning($"Order with ID {orderId} not found for printing receipt");
                    throw new ArgumentException($"Order with ID {orderId} not found");
                }

                // Use the ReceiptService to create the receipt
                return await _receiptService.CreateExpressServiceReceiptAsync(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating Express Service receipt for order ID {orderId}");
                throw;
            }
        }

        /// <summary>
        /// Shows the print preview dialog for a document
        /// </summary>
        /// <param name="document">The FlowDocument to preview</param>
        public void ShowPrintPreview(FlowDocument document)
        {
            try
            {
                // Create print dialog
                PrintDialog printDialog = new PrintDialog();

                // Create document paginator
                IDocumentPaginatorSource paginatorSource = document;

                // Show print preview
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintDocument(paginatorSource.DocumentPaginator, "Print Document");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing print preview");
                throw;
            }
        }
    }
}