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
        private readonly AppDbContext _dbContext;
        private readonly ILogger<OrdersPage> _logger;
        private readonly PrintService _printService;
        private List<OrderViewModel> _orders;

        public OrdersPage()
        {
            InitializeComponent();

            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();
            _dbContext = _serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
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
                var orders = await _dbContext.Orders
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
                    DriverVehicleNumber = o.Driver?.VehiclePlateNumber ?? ""
                }).ToList();

                dgOrders.ItemsSource = _orders;
                UpdateSummary();
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
                MessageBox.Show($"عرض تفاصيل الطلب رقم {selectedOrder.OrderNumber}", "تفاصيل الطلب", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int orderId)
            {
                MessageBox.Show($"تعديل الطلب رقم {orderId}", "تعديل الطلب", MessageBoxButton.OK, MessageBoxImage.Information);
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
                        var order = await _dbContext.Orders.FindAsync(orderId);
                        if (order != null)
                        {
                            _dbContext.Orders.Remove(order);
                            await _dbContext.SaveChangesAsync();
                            LoadOrders();
                            MessageBox.Show("تم حذف الطلب بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private async void btnPrintReceipt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int orderId)
                {
                    var receiptDocument = await _printService.CreateExpressServiceReceiptAsync(orderId);
                    _printService.ShowPrintPreview(receiptDocument);
                    _logger?.LogInformation($"Print Express Service receipt requested for order ID: {orderId}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error printing receipt");
                MessageBox.Show($"حدث خطأ أثناء طباعة الإيصال: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnMarkAsPaid_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int orderId)
                {
                    var order = await _dbContext.Orders.FindAsync(orderId);
                    if (order != null && !order.IsPaid)
                    {
                        order.IsPaid = true;
                        if (!order.DeliveryDate.HasValue)
                        {
                            order.DeliveryDate = DateTime.Now;
                        }

                        _dbContext.Orders.Update(order);
                        await _dbContext.SaveChangesAsync();
                        LoadOrders();
                        MessageBox.Show("تم تحديث حالة الدفع بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
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
                if (printDialog.ShowDialog() == true)
                {
                    IDocumentPaginatorSource paginatorSource = document;
                    printDialog.PrintDocument(paginatorSource.DocumentPaginator, "Orders Table Print");
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
                FontSize = 12,
                PagePadding = new Thickness(50),
                FlowDirection = FlowDirection.RightToLeft
            };

            var headerPara = new Paragraph(new Run("تقرير الطلبات"))
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            document.Blocks.Add(headerPara);

            var datePara = new Paragraph(new Run($"تاريخ الطباعة: {DateTime.Now:yyyy-MM-dd HH:mm}"))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            document.Blocks.Add(datePara);

            var summaryPara = new Paragraph();
            summaryPara.Inlines.Add(new Run("إجمالي السعر: ") { FontWeight = FontWeights.Bold });
            summaryPara.Inlines.Add(new Run(txtTotalPrice.Text));
            summaryPara.Inlines.Add(new LineBreak());
            summaryPara.Inlines.Add(new Run("إجمالي الربح: ") { FontWeight = FontWeights.Bold });
            summaryPara.Inlines.Add(new Run(txtTotalProfit.Text));
            summaryPara.Inlines.Add(new LineBreak());
            summaryPara.Inlines.Add(new Run("الإجمالي الكلي: ") { FontWeight = FontWeights.Bold });
            summaryPara.Inlines.Add(new Run(txtGrandTotal.Text));
            summaryPara.Margin = new Thickness(0, 0, 0, 20);
            document.Blocks.Add(summaryPara);

            var table = new Table();

            for (int i = 0; i < 10; i++)
            {
                table.Columns.Add(new TableColumn());
            }

            table.BorderBrush = Brushes.Black;
            table.BorderThickness = new Thickness(1);
            table.CellSpacing = 0;

            var headerRow = new TableRow();
            headerRow.Background = Brushes.LightGray;

            headerRow.Cells.Add(CreateTableCell("رقم الطلب", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("اسم العميل", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("العنوان", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("تاريخ الطلب", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("تاريخ الدفع", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("المبلغ", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("الربح", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("الإجمالي", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("السائق", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("رقم السيارة", FontWeights.Bold));

            var headerRowGroup = new TableRowGroup();
            headerRowGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerRowGroup);

            var dataRowGroup = new TableRowGroup();

            IEnumerable<OrderViewModel> itemsSource = dgOrders.ItemsSource as IEnumerable<OrderViewModel>;
            if (itemsSource != null)
            {
                foreach (var order in itemsSource)
                {
                    var row = new TableRow();

                    row.Cells.Add(CreateTableCell(order.OrderNumber));
                    row.Cells.Add(CreateTableCell(order.CustomerName));
                    row.Cells.Add(CreateTableCell(order.CustomerAddress));
                    row.Cells.Add(CreateTableCell(order.OrderDate.ToString("yyyy-MM-dd")));
                    row.Cells.Add(CreateTableCell(order.DatePaid.HasValue ? order.DatePaid.Value.ToString("yyyy-MM-dd") : "-"));
                    row.Cells.Add(CreateTableCell(order.Price.ToString("N2")));
                    row.Cells.Add(CreateTableCell(order.DeliveryFee.ToString("N2")));
                    row.Cells.Add(CreateTableCell(order.TotalPrice.ToString("N2")));
                    row.Cells.Add(CreateTableCell(order.DriverName));
                    row.Cells.Add(CreateTableCell(order.DriverVehicleNumber));

                    dataRowGroup.Rows.Add(row);
                }
            }

            table.RowGroups.Add(dataRowGroup);

            var summaryRow = new TableRow();
            summaryRow.Background = Brushes.LightGray;

            var totalLabelCell = new TableCell();
            totalLabelCell.ColumnSpan = 5;
            var totalLabelPara = new Paragraph(new Run("الإجمالي:"));
            totalLabelPara.FontWeight = FontWeights.Bold;
            totalLabelPara.TextAlignment = TextAlignment.Right;
            totalLabelCell.Blocks.Add(totalLabelPara);
            summaryRow.Cells.Add(totalLabelCell);

            summaryRow.Cells.Add(CreateTableCell(txtTotalPrice.Text, FontWeights.Bold));
            summaryRow.Cells.Add(CreateTableCell(txtTotalProfit.Text, FontWeights.Bold));
            summaryRow.Cells.Add(CreateTableCell(txtGrandTotal.Text, FontWeights.Bold));

            summaryRow.Cells.Add(new TableCell(new Paragraph(new Run(""))));
            summaryRow.Cells.Add(new TableCell(new Paragraph(new Run(""))));

            var summaryRowGroup = new TableRowGroup();
            summaryRowGroup.Rows.Add(summaryRow);
            table.RowGroups.Add(summaryRowGroup);

            document.Blocks.Add(table);

            return document;
        }

        private TableCell CreateTableCell(string text, FontWeight fontWeight = default)
        {
            var paragraph = new Paragraph(new Run(text));
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
    }
}