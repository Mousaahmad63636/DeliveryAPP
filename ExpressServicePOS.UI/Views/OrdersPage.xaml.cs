// File: ExpressServicePOS.UI/Views/OrdersPage.xaml.cs
using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using ExpressServicePOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ExpressServicePOS.UI.Views
{
    public class OrderViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerPhone { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DatePaid { get; set; }
        public DeliveryStatus Status { get; set; }
        public string StatusText { get; set; }
        public decimal Price { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsPaid { get; set; }
        public string DriverName { get; set; }
        public string DriverVehicleNumber { get; set; }
        public string OrderDescription { get; set; }
        public string Notes { get; set; }
        public int CustomerId { get; set; }
        public int? DriverId { get; set; }
        public string RecipientName { get; set; }
        public string RecipientPhone { get; set; }
        public string SenderName { get; set; }
        public string SenderPhone { get; set; }
        public string PickupLocation { get; set; }
        public string DeliveryLocation { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
        public bool IsBreakable { get; set; }
        public bool IsReplacement { get; set; }
        public bool IsReturned { get; set; }

        // Subscription-related properties
        public bool IsCoveredBySubscription { get; set; }
        public int? SubscriptionId { get; set; }
        public string ProfitDisplay { get; set; }
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Collapsed;
            }
            return false;
        }
    }

    public partial class OrdersPage : Page
    {
        private readonly IServiceScope _serviceScope;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<OrdersPage> _logger;
        private readonly PrintService _printService;
        private List<OrderViewModel> _orders;
        private List<OrderViewModel> _filteredOrders;
        private List<Customer> _customers;
        private Customer _selectedCustomer;
        private DateTime? _startDate;
        private DateTime? _endDate;

        public OrdersPage()
        {
            InitializeComponent();

            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();
            _dbContextFactory = _serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<OrdersPage>>();
            _printService = _serviceScope.ServiceProvider.GetRequiredService<PrintService>();

            // Set default date range (current month)
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            _startDate = firstDayOfMonth;
            _endDate = today;

            dpStartDate.SelectedDate = _startDate;
            dpEndDate.SelectedDate = _endDate;

            LoadOrders();
            LoadCustomers();
            Unloaded += OrdersPage_Unloaded;
        }

        private void OrdersPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _serviceScope?.Dispose();
        }

        private async void LoadCustomers()
        {
            try
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    _customers = await dbContext.Customers.OrderBy(c => c.Name).ToListAsync();

                    // Add an "All Customers" option at the beginning
                    var allCustomersOption = new Customer
                    {
                        Id = -1,
                        Name = "-- جميع العملاء --",
                        Address = "",
                        Phone = "",
                        Notes = ""
                    };

                    var customersList = new List<Customer> { allCustomersOption };
                    customersList.AddRange(_customers);

                    cmbCustomers.ItemsSource = customersList;
                    cmbCustomers.SelectedIndex = 0; // Select "All Customers" by default
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading customers");
                MessageBox.Show($"حدث خطأ أثناء تحميل بيانات العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadOrders()
        {
            try
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    var orders = await dbContext.Orders
                        .Include(o => o.Customer)
                        .Include(o => o.Driver)
                        .Include(o => o.Subscription) // Include subscription information
                        .OrderByDescending(o => o.OrderDate)
                        .ToListAsync();

                    _orders = orders.Select(o => new OrderViewModel
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        CustomerName = o.Customer?.Name ?? "غير معروف",
                        CustomerAddress = o.Customer?.Address ?? "",
                        CustomerPhone = o.Customer?.Phone ?? "",
                        OrderDate = o.OrderDate,
                        DatePaid = o.IsPaid ? o.DeliveryDate ?? o.OrderDate : null,
                        Status = o.DeliveryStatus,
                        StatusText = GetStatusText(o.DeliveryStatus),
                        Price = o.Price,
                        DeliveryFee = o.DeliveryFee,
                        TotalPrice = o.TotalPrice,
                        IsPaid = o.IsPaid,
                        DriverName = o.Driver?.Name ?? o.DriverName,
                        DriverVehicleNumber = o.Driver?.VehiclePlateNumber ?? "",
                        OrderDescription = o.OrderDescription,
                        Notes = o.Notes,
                        CustomerId = o.CustomerId,
                        DriverId = o.DriverId,
                        RecipientName = o.RecipientName,
                        RecipientPhone = o.RecipientPhone,
                        SenderName = o.SenderName,
                        SenderPhone = o.SenderPhone,
                        PickupLocation = o.PickupLocation,
                        DeliveryLocation = o.DeliveryLocation,
                        Currency = o.Currency,
                        PaymentMethod = o.PaymentMethod,
                        IsBreakable = o.IsBreakable,
                        IsReplacement = o.IsReplacement,
                        IsReturned = o.IsReturned,

                        // Add subscription information
                        IsCoveredBySubscription = o.IsCoveredBySubscription,
                        SubscriptionId = o.SubscriptionId,

                        // Format profit display based on subscription
                        ProfitDisplay = o.IsCoveredBySubscription && o.Subscription != null
                            ? $"اشتراك شهري: {FormatAmount(o.Subscription.Amount, o.Subscription.Currency)}"
                            : FormatAmount(o.DeliveryFee, o.Currency)
                    }).ToList();

                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading orders");
                MessageBox.Show($"حدث خطأ أثناء تحميل الطلبات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FormatAmount(decimal amount, string currency)
        {
            return currency == "USD"
                ? $"{amount:N2} $"
                : $"{amount:N0} ل.ل";
        }

        private void ApplyFilters()
        {
            if (_orders == null || !_orders.Any())
            {
                _filteredOrders = new List<OrderViewModel>();
                dgOrders.ItemsSource = _filteredOrders;
                UpdateSummary();
                return;
            }

            // Start with all orders
            var filteredOrders = _orders;

            // Apply customer filter if selected
            if (_selectedCustomer != null && _selectedCustomer.Id > 0)
            {
                filteredOrders = filteredOrders.Where(o => o.CustomerId == _selectedCustomer.Id).ToList();
            }

            // Apply date range filter
            if (_startDate.HasValue)
            {
                filteredOrders = filteredOrders.Where(o => o.OrderDate.Date >= _startDate.Value.Date).ToList();
            }

            if (_endDate.HasValue)
            {
                filteredOrders = filteredOrders.Where(o => o.OrderDate.Date <= _endDate.Value.Date).ToList();
            }

            // Apply search filter if any
            string searchTerm = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                filteredOrders = filteredOrders.Where(o =>
                    o.OrderNumber.ToLower().Contains(searchTerm) ||
                    o.CustomerName.ToLower().Contains(searchTerm) ||
                    o.CustomerPhone.ToLower().Contains(searchTerm) ||
                    (o.RecipientPhone != null && o.RecipientPhone.ToLower().Contains(searchTerm)) ||
                    o.Id.ToString().Contains(searchTerm) ||
                    (o.DriverName != null && o.DriverName.ToLower().Contains(searchTerm)) ||
                    (o.DriverVehicleNumber != null && o.DriverVehicleNumber.ToLower().Contains(searchTerm))
                ).ToList();
            }

            _filteredOrders = filteredOrders;
            dgOrders.ItemsSource = _filteredOrders;

            UpdateSummary();
        }

        private void UpdateSummary()
        {
            if (_filteredOrders == null || !_filteredOrders.Any())
            {
                txtTotalPrice.Text = "0.00";
                txtTotalProfit.Text = "0.00";
                txtGrandTotal.Text = "0.00";
                txtDeliveredCount.Text = "0";
                txtPendingCount.Text = "0";
                return;
            }

            decimal totalPrice = _filteredOrders.Sum(o => o.Price);
            decimal totalProfit = _filteredOrders.Sum(o => o.DeliveryFee);
            decimal grandTotal = _filteredOrders.Sum(o => o.TotalPrice);

            int deliveredCount = _filteredOrders.Count(o => o.Status == DeliveryStatus.Delivered);
            int pendingCount = _filteredOrders.Count(o => o.Status != DeliveryStatus.Delivered);

            txtTotalPrice.Text = totalPrice.ToString("N2");
            txtTotalProfit.Text = totalProfit.ToString("N2");
            txtGrandTotal.Text = grandTotal.ToString("N2");
            txtDeliveredCount.Text = deliveredCount.ToString();
            txtPendingCount.Text = pendingCount.ToString();
        }

        private string GetStatusText(DeliveryStatus status)
        {
            return status switch
            {
                DeliveryStatus.Pending => "قيد الانتظار",
                DeliveryStatus.PickedUp => "تم الاستلام",
                DeliveryStatus.InTransit => "قيد التوصيل",
                DeliveryStatus.Delivered => "تم التسليم",
                DeliveryStatus.PartialDelivery => "تسليم جزئي",
                DeliveryStatus.Failed => "فشل التوصيل",
                DeliveryStatus.Cancelled => "ملغي",
                _ => "غير معروف"
            };
        }

        private DeliveryStatus GetStatusFromText(string statusText)
        {
            return statusText switch
            {
                "قيد الانتظار" => DeliveryStatus.Pending,
                "تم الاستلام" => DeliveryStatus.PickedUp,
                "قيد التوصيل" => DeliveryStatus.InTransit,
                "تم التسليم" => DeliveryStatus.Delivered,
                "تسليم جزئي" => DeliveryStatus.PartialDelivery,
                "فشل التوصيل" => DeliveryStatus.Failed,
                "ملغي" => DeliveryStatus.Cancelled,
                _ => DeliveryStatus.Pending
            };
        }

        private void btnAddNewOrder_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new NewOrderPage());
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void txtSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyFilters();
            }
        }

        private void cmbCustomers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCustomers.SelectedItem is Customer customer)
            {
                if (customer.Id == -1) // "All Customers" option
                {
                    _selectedCustomer = null;
                }
                else
                {
                    _selectedCustomer = customer;
                }

                ApplyFilters();
            }
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (sender == dpStartDate)
            {
                _startDate = dpStartDate.SelectedDate;
            }
            else if (sender == dpEndDate)
            {
                _endDate = dpEndDate.SelectedDate;
            }

            ApplyFilters();
        }

        private void btnResetFilter_Click(object sender, RoutedEventArgs e)
        {
            // Reset all filters
            cmbCustomers.SelectedIndex = 0; // "All Customers"
            _selectedCustomer = null;

            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            dpStartDate.SelectedDate = firstDayOfMonth;
            dpEndDate.SelectedDate = today;

            _startDate = firstDayOfMonth;
            _endDate = today;

            txtSearch.Text = string.Empty;

            ApplyFilters();
        }

        private void dgOrders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgOrders.SelectedItem is OrderViewModel selectedOrder)
            {
                EditOrder(selectedOrder.Id);
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int orderId)
            {
                EditOrder(orderId);
            }
        }

        private async void EditOrder(int orderId)
        {
            try
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    var order = await dbContext.Orders
                        .Include(o => o.Customer)
                        .Include(o => o.Driver)
                        .Include(o => o.Subscription)
                        .FirstOrDefaultAsync(o => o.Id == orderId);

                    if (order == null)
                    {
                        MessageBox.Show("الطلب غير موجود", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var dialog = new OrderEditDialog(order);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadOrders();
                        MessageBox.Show("تم تحديث الطلب بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error editing order");
                MessageBox.Show($"حدث خطأ أثناء تعديل الطلب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int orderId)
            {
                var result = MessageBox.Show("هل أنت متأكد من حذف هذا الطلب؟", "تأكيد الحذف",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                        {
                            var order = await dbContext.Orders.FindAsync(orderId);
                            if (order != null)
                            {
                                dbContext.Orders.Remove(order);
                                await dbContext.SaveChangesAsync();
                                LoadOrders();
                                MessageBox.Show("تم حذف الطلب بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error deleting order");
                        MessageBox.Show($"حدث خطأ أثناء حذف الطلب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void btnMarkAsPaid_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int orderId)
                {
                    using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                    {
                        var order = await dbContext.Orders.FindAsync(orderId);
                        if (order != null && !order.IsPaid)
                        {
                            order.IsPaid = true;
                            if (!order.DeliveryDate.HasValue)
                            {
                                order.DeliveryDate = DateTime.Now;
                            }

                            dbContext.Orders.Update(order);
                            await dbContext.SaveChangesAsync();
                            LoadOrders();
                            MessageBox.Show("تم تحديث حالة الدفع بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error marking order as paid");
                MessageBox.Show($"حدث خطأ أثناء تحديث حالة الدفع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPrintTable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var document = CreateTablePrintDocument();

                PrintDialog printDialog = new PrintDialog();

                if (printDialog.PrintTicket != null)
                {
                    printDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Landscape;
                }

                if (printDialog.ShowDialog() == true)
                {
                    document.PageHeight = printDialog.PrintableAreaHeight;
                    document.PageWidth = printDialog.PrintableAreaWidth;

                    IDocumentPaginatorSource paginatorSource = document;
                    printDialog.PrintDocument(
                        paginatorSource.DocumentPaginator,
                        "تقرير الطلبات - " + DateTime.Now.ToString("yyyy-MM-dd"));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error printing table");
                MessageBox.Show($"حدث خطأ أثناء طباعة الجدول: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CreateTablePrintDocument()
        {
            var document = new FlowDocument
            {
                FontFamily = new FontFamily("Arial"),
                FontSize = 10,
                PagePadding = new Thickness(20),
                FlowDirection = FlowDirection.RightToLeft,
                ColumnWidth = double.MaxValue,
                PageWidth = 11.69 * 96,
                PageHeight = 8.27 * 96
            };

            var headerPara = new Paragraph(new Run("خدمة اكسبرس - تقرير الطلبات"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            document.Blocks.Add(headerPara);

            var datePara = new Paragraph(new Run($"تاريخ الطباعة: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            document.Blocks.Add(datePara);

            // Add filter information
            var filterPara = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            if (_selectedCustomer != null)
            {
                filterPara.Inlines.Add(new Run($"العميل: {_selectedCustomer.Name} | "));
            }

            if (_startDate.HasValue && _endDate.HasValue)
            {
                filterPara.Inlines.Add(new Run($"الفترة: من {_startDate.Value:yyyy-MM-dd} إلى {_endDate.Value:yyyy-MM-dd}"));
            }

            document.Blocks.Add(filterPara);

            var summaryPara = new Paragraph
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 0, 0, 5),
                Margin = new Thickness(0, 0, 0, 10)
            };

            summaryPara.Inlines.Add(new Run("إجمالي السعر: ") { FontWeight = FontWeights.Bold });
            summaryPara.Inlines.Add(new Run(txtTotalPrice.Text));
            summaryPara.Inlines.Add(new Run("   |   "));
            summaryPara.Inlines.Add(new Run("إجمالي الربح: ") { FontWeight = FontWeights.Bold });
            summaryPara.Inlines.Add(new Run(txtTotalProfit.Text));
            summaryPara.Inlines.Add(new Run("   |   "));
            summaryPara.Inlines.Add(new Run("الإجمالي الكلي: ") { FontWeight = FontWeights.Bold });
            summaryPara.Inlines.Add(new Run(txtGrandTotal.Text));
            summaryPara.Inlines.Add(new Run("   |   "));
            summaryPara.Inlines.Add(new Run("تم التسليم: ") { FontWeight = FontWeights.Bold });
            summaryPara.Inlines.Add(new Run(txtDeliveredCount.Text));
            summaryPara.Inlines.Add(new Run("   |   "));
            summaryPara.Inlines.Add(new Run("قيد التوصيل: ") { FontWeight = FontWeights.Bold });
            summaryPara.Inlines.Add(new Run(txtPendingCount.Text));

            document.Blocks.Add(summaryPara);

            var table = new Table
            {
                CellSpacing = 0,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.5)
            };

            table.Columns.Add(new TableColumn { Width = new GridLength(40) });  // Order #
            table.Columns.Add(new TableColumn { Width = new GridLength(70) });  // Customer
            table.Columns.Add(new TableColumn { Width = new GridLength(70) });  // Recipient Phone
            table.Columns.Add(new TableColumn { Width = new GridLength(110) }); // Address
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });  // Date
            table.Columns.Add(new TableColumn { Width = new GridLength(140) }); // Status
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });  // Amount
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });  // Profit/Subscription
            table.Columns.Add(new TableColumn { Width = new GridLength(60) });  // Total
            table.Columns.Add(new TableColumn { Width = new GridLength(60) });  // Paid
            table.Columns.Add(new TableColumn { Width = new GridLength(70) });  // Driver

            var headerRow = new TableRow();
            headerRow.Background = Brushes.LightGray;

            headerRow.Cells.Add(CreateTableCell("رقم الطلب", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("العميل", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("هاتف المستلم", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("العنوان", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("تاريخ الطلب", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("الحالة", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("السعر", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("الربح", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("الإجمالي", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("مدفوع", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("السائق", FontWeights.Bold));

            var headerRowGroup = new TableRowGroup();
            headerRowGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerRowGroup);

            var dataRowGroup = new TableRowGroup();

            if (_filteredOrders != null)
            {
                int rowIndex = 0;
                foreach (var order in _filteredOrders)
                {
                    var row = new TableRow();

                    if (rowIndex % 2 == 1)
                    {
                        row.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                    }

                    row.Cells.Add(CreateTableCell(order.OrderNumber));
                    row.Cells.Add(CreateTableCell(order.CustomerName));
                    row.Cells.Add(CreateTableCell(order.RecipientPhone));
                    row.Cells.Add(CreateTableCell(order.CustomerAddress));
                    row.Cells.Add(CreateTableCell(order.OrderDate.ToString("yyyy-MM-dd")));
                    row.Cells.Add(CreateTableCell(order.StatusText));
                    row.Cells.Add(CreateTableCell(order.Price.ToString("N2")));
                    row.Cells.Add(CreateTableCell(order.ProfitDisplay));
                    row.Cells.Add(CreateTableCell(order.TotalPrice.ToString("N2")));
                    row.Cells.Add(CreateTableCell(order.IsPaid ? "نعم" : "لا"));
                    row.Cells.Add(CreateTableCell(order.DriverName));

                    dataRowGroup.Rows.Add(row);
                    rowIndex++;
                }
            }

            table.RowGroups.Add(dataRowGroup);
            document.Blocks.Add(table);

            var footerPara = new Paragraph(new Run("جميع الحقوق محفوظة لخدمة اكسبرس © " + DateTime.Now.Year))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0),
                FontStyle = FontStyles.Italic,
                FontSize = 9
            };
            document.Blocks.Add(footerPara);

            return document;
        }

        private TableCell CreateTableCell(string text, FontWeight fontWeight = default)
        {
            var paragraph = new Paragraph(new Run(text ?? "-"))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(2)
            };

            if (fontWeight != default)
            {
                paragraph.FontWeight = fontWeight;
            }

            return new TableCell(paragraph);
        }
    }
}