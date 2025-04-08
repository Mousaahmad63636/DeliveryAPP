// File: ExpressServicePOS.UI/Views/SubscriptionReport.xaml.cs
using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Core.ViewModels;
using ExpressServicePOS.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ExpressServicePOS.UI.Views
{
    /// <summary>
    /// Interaction logic for SubscriptionReport.xaml
    /// </summary>
    public partial class SubscriptionReport : Page
    {
        private readonly IServiceScope _serviceScope;
        private readonly SubscriptionService _subscriptionService;
        private readonly PrintService _printService;
        private readonly ILogger<SubscriptionReport> _logger;

        public SubscriptionReport()
        {
            InitializeComponent();

            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();
            _subscriptionService = _serviceScope.ServiceProvider.GetRequiredService<SubscriptionService>();
            _printService = _serviceScope.ServiceProvider.GetRequiredService<PrintService>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<SubscriptionReport>>();

            txtReportDate.Text = $"تاريخ التقرير: {DateTime.Now:yyyy-MM-dd}";

            LoadReportData();

            Unloaded += SubscriptionReport_Unloaded;
        }

        private void SubscriptionReport_Unloaded(object sender, RoutedEventArgs e)
        {
            _serviceScope?.Dispose();
        }

        private async void LoadReportData()
        {
            try
            {
                // Load statistics
                var stats = await _subscriptionService.GetSubscriptionStatisticsAsync();

                txtActiveSubscriptions.Text = stats.ActiveSubscriptions.ToString();
                txtInactiveSubscriptions.Text = stats.InactiveSubscriptions.ToString();
                txtMonthlyRevenueUSD.Text = $"{stats.MonthlyRevenueUSD:N2} $";
                txtMonthlyRevenueLBP.Text = $"{stats.MonthlyRevenueLBP:N0} ل.ل";

                // Load active subscriptions for the table
                var activeSubscriptions = await _subscriptionService.GetSubscriptionsAsync(true);
                var viewModels = activeSubscriptions.Select(s => new SubscriptionViewModel(s)).ToList();
                dgSubscriptions.ItemsSource = viewModels;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading subscription report data");
                MessageBox.Show($"حدث خطأ أثناء تحميل بيانات التقرير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var document = CreateReportDocument();
                _printService.ShowPrintPreview(document);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error printing subscription report");
                MessageBox.Show($"حدث خطأ أثناء طباعة التقرير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CreateReportDocument()
        {
            var document = new FlowDocument
            {
                FontFamily = new FontFamily("Arial"),
                FontSize = 12,
                PagePadding = new Thickness(50),
                FlowDirection = FlowDirection.RightToLeft
            };

            // Add title
            var titlePara = new Paragraph(new Run("تقرير الاشتراكات الشهرية"))
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            document.Blocks.Add(titlePara);

            // Add report date
            var datePara = new Paragraph(new Run($"تاريخ التقرير: {DateTime.Now:yyyy-MM-dd}"))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            document.Blocks.Add(datePara);

            // Add summary section
            var summarySection = new Section();
            var summaryTitle = new Paragraph(new Run("ملخص الاشتراكات"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            summarySection.Blocks.Add(summaryTitle);

            // Create summary table
            var summaryTable = new Table();
            summaryTable.CellSpacing = 0;
            summaryTable.Columns.Add(new TableColumn { Width = new GridLength(200) });
            summaryTable.Columns.Add(new TableColumn { Width = new GridLength(150) });

            var summaryHeader = new TableRowGroup();
            summaryTable.RowGroups.Add(summaryHeader);

            // Add rows for summary data
            AddTableRow(summaryHeader, "الاشتراكات النشطة", txtActiveSubscriptions.Text);
            AddTableRow(summaryHeader, "الاشتراكات الغير نشطة", txtInactiveSubscriptions.Text);
            AddTableRow(summaryHeader, "الإيرادات الشهرية (USD)", txtMonthlyRevenueUSD.Text);
            AddTableRow(summaryHeader, "الإيرادات الشهرية (ل.ل)", txtMonthlyRevenueLBP.Text);

            summarySection.Blocks.Add(summaryTable);
            document.Blocks.Add(summarySection);

            // Add subscriptions list section
            var listSection = new Section();
            var listTitle = new Paragraph(new Run("قائمة الاشتراكات النشطة"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 20, 0, 10)
            };
            listSection.Blocks.Add(listTitle);

            // Create subscriptions table
            var subsTable = new Table();
            subsTable.CellSpacing = 0;
            subsTable.Columns.Add(new TableColumn { Width = new GridLength(150) }); // Customer
            subsTable.Columns.Add(new TableColumn { Width = new GridLength(100) }); // Phone
            subsTable.Columns.Add(new TableColumn { Width = new GridLength(80) });  // Amount
            subsTable.Columns.Add(new TableColumn { Width = new GridLength(100) }); // Start Date
            subsTable.Columns.Add(new TableColumn { Width = new GridLength(100) }); // Next Payment

            // Add header row
            var headerRow = new TableRow();
            headerRow.Background = Brushes.LightGray;
            headerRow.Cells.Add(CreateTableCell("العميل", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("رقم الهاتف", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("المبلغ", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("تاريخ البدء", FontWeights.Bold));
            headerRow.Cells.Add(CreateTableCell("الدفعة القادمة", FontWeights.Bold));

            var tableRowGroup = new TableRowGroup();
            tableRowGroup.Rows.Add(headerRow);
            subsTable.RowGroups.Add(tableRowGroup);

            // Add data rows
            if (dgSubscriptions.ItemsSource is System.Collections.IEnumerable items)
            {
                int rowIndex = 0;
                foreach (SubscriptionViewModel sub in items)
                {
                    var row = new TableRow();
                    if (rowIndex % 2 == 1)
                    {
                        row.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                    }

                    row.Cells.Add(CreateTableCell(sub.Customer?.Name ?? ""));
                    row.Cells.Add(CreateTableCell(sub.Customer?.Phone ?? ""));
                    row.Cells.Add(CreateTableCell(sub.AmountFormatted));
                    row.Cells.Add(CreateTableCell(sub.StartDate.ToString("yyyy-MM-dd")));
                    row.Cells.Add(CreateTableCell(sub.NextPaymentDate?.ToString("yyyy-MM-dd") ?? "-"));

                    tableRowGroup.Rows.Add(row);
                    rowIndex++;
                }
            }

            listSection.Blocks.Add(subsTable);
            document.Blocks.Add(listSection);

            // Add footer
            var footerPara = new Paragraph(new Run("© جميع الحقوق محفوظة لخدمة اكسبرس " + DateTime.Now.Year))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 30, 0, 0),
                FontStyle = FontStyles.Italic,
                FontSize = 10
            };
            document.Blocks.Add(footerPara);

            return document;
        }

        private void AddTableRow(TableRowGroup group, string label, string value)
        {
            var row = new TableRow();
            row.Cells.Add(CreateTableCell(label, FontWeights.Bold));
            row.Cells.Add(CreateTableCell(value));
            group.Rows.Add(row);
        }

        private TableCell CreateTableCell(string text, FontWeight fontWeight = default)
        {
            var paragraph = new Paragraph(new Run(text ?? "-"))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(5)
            };

            if (fontWeight != default)
            {
                paragraph.FontWeight = fontWeight;
            }

            return new TableCell(paragraph);
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
            {
                NavigationService.GoBack();
            }
        }
    }
}