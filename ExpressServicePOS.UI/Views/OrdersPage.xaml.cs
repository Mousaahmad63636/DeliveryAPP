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
    public partial class OrdersPage : Page
    {
        private readonly IServiceScope _serviceScope;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<OrdersPage> _logger;
        private readonly PrintService _printService;
        private List<OrderViewModel> _orders;

        public OrdersPage()
        {
            InitializeComponent();

            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();
            _dbContextFactory = _serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<OrdersPage>>();
            _printService = _serviceScope.ServiceProvider.GetRequiredService<PrintService>();

            LoadOrders();
            Unloaded += OrdersPage_Unloaded;
        }

        private void OrdersPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _serviceScope?.Dispose();
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
                        IsReturned = o.IsReturned
                    }).ToList();

                    dgOrders.ItemsSource = _orders;
                    UpdateSummary();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading orders");
                MessageBox.Show($"حدث خطأ أثناء تحميل الطلبات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummary()
        {
            if (_orders == null || !_orders.Any())
            {
                txtTotalPrice.Text = "0.00";
                txtTotalProfit.Text = "0.00";
                txtGrandTotal.Text = "0.00";
                return;
            }

            decimal totalPrice = _orders.Sum(o => o.Price);
            decimal totalProfit = _orders.Sum(o => o.DeliveryFee);
            decimal grandTotal = _orders.Sum(o => o.TotalPrice);

            txtTotalPrice.Text = totalPrice.ToString("N2");
            txtTotalProfit.Text = totalProfit.ToString("N2");
            txtGrandTotal.Text = grandTotal.ToString("N2");
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
            ApplySearch();
        }

        private void txtSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplySearch();
            }
        }

        private void ApplySearch()
        {
            string searchTerm = txtSearch.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchTerm))
            {
                dgOrders.ItemsSource = _orders;
                UpdateSummary();
                return;
            }

            var filteredOrders = _orders.Where(o =>
                o.OrderNumber.ToLower().Contains(searchTerm) ||
                o.CustomerName.ToLower().Contains(searchTerm) ||
                o.CustomerPhone.ToLower().Contains(searchTerm) ||
                o.Id.ToString().Contains(searchTerm) ||
                (o.DriverName != null && o.DriverName.ToLower().Contains(searchTerm)) ||
                (o.DriverVehicleNumber != null && o.DriverVehicleNumber.ToLower().Contains(searchTerm))
            ).ToList();

            dgOrders.ItemsSource = filteredOrders;

            decimal totalPrice = filteredOrders.Sum(o => o.Price);
            decimal totalProfit = filteredOrders.Sum(o => o.DeliveryFee);
            decimal grandTotal = filteredOrders.Sum(o => o.TotalPrice);

            txtTotalPrice.Text = totalPrice.ToString("N2");
            txtTotalProfit.Text = totalProfit.ToString("N2");
            txtGrandTotal.Text = grandTotal.ToString("N2");
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

            document.Blocks.Add(summaryPara);

            var table = new Table
            {
                CellSpacing = 0,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.5)
            };

            table.Columns.Add(new TableColumn { Width = new GridLength(40) });
            table.Columns.Add(new TableColumn { Width = new GridLength(70) });
            table.Columns.Add(new TableColumn { Width = new GridLength(110) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(140) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(60) });
            table.Columns.Add(new TableColumn { Width = new GridLength(60) });
            table.Columns.Add(new TableColumn { Width = new GridLength(60) });
            table.Columns.Add(new TableColumn { Width = new GridLength(70) });

            var headerRow = new TableRow();
            headerRow.Background = Brushes.LightGray;

            headerRow.Cells.Add(CreateTableCell("معرف", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("رقم الطلب", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("اسم العميل", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("رقم الهاتف", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("العنوان", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("تاريخ الطلب", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("تاريخ الدفع", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("السعر", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("الربح", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("الإجمالي", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("الحالة", FontWeights.Bold));

            var headerRowGroup = new TableRowGroup();
            headerRowGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerRowGroup);

            var dataRowGroup = new TableRowGroup();
            IEnumerable<OrderViewModel> itemsSource = dgOrders.ItemsSource as IEnumerable<OrderViewModel>;

            if (itemsSource != null)
            {
                int rowIndex = 0;
                foreach (var order in itemsSource)
                {
                    var row = new TableRow();

                    if (rowIndex % 2 == 1)
                    {
                        row.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                    }

                    row.Cells.Add(CreateTableCell(order.Id.ToString()));
                    row.Cells.Add(CreateTableCell(order.OrderNumber));
                    row.Cells.Add(CreateTableCell(order.CustomerName));
                    row.Cells.Add(CreateTableCell(order.CustomerPhone));
                    row.Cells.Add(CreateTableCell(order.CustomerAddress));
                    row.Cells.Add(CreateTableCell(order.OrderDate.ToString("yyyy-MM-dd")));
                    row.Cells.Add(CreateTableCell(order.DatePaid.HasValue ? order.DatePaid.Value.ToString("yyyy-MM-dd") : "-"));
                    row.Cells.Add(CreateTableCell(order.Price.ToString("N2")));
                    row.Cells.Add(CreateTableCell(order.DeliveryFee.ToString("N2")));
                    row.Cells.Add(CreateTableCell(order.TotalPrice.ToString("N2")));
                    row.Cells.Add(CreateTableCell(order.StatusText));

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
    }
}