// File: ExpressServicePOS.Infrastructure.Services/ReceiptService.cs
using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ExpressServicePOS.Infrastructure.Services
{
    /// <summary>
    /// Service for managing receipt templates and generating receipts.
    /// </summary>
    public class ReceiptService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ReceiptService> _logger;
        private ReceiptTemplate _currentTemplate;

        public ReceiptService(AppDbContext dbContext, ILogger<ReceiptService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the current receipt template.
        /// </summary>
        /// <returns>The current receipt template.</returns>
        public async Task<ReceiptTemplate> GetCurrentTemplateAsync()
        {
            if (_currentTemplate == null)
            {
                _currentTemplate = await _dbContext.ReceiptTemplates.FirstOrDefaultAsync();
                if (_currentTemplate == null)
                {
                    // Create default template if none exists
                    _currentTemplate = new ReceiptTemplate();
                    _dbContext.ReceiptTemplates.Add(_currentTemplate);
                    await _dbContext.SaveChangesAsync();
                }
            }
            return _currentTemplate;
        }

        /// <summary>
        /// Creates a FlowDocument for an order receipt exactly matching the format in the new image.
        /// </summary>
        /// <param name="order">The order to create a receipt for.</param>
        /// <returns>A FlowDocument representing the receipt.</returns>
        public async Task<FlowDocument> CreateExpressServiceReceiptAsync(Order order)
        {
            try
            {
                if (order == null)
                    throw new ArgumentNullException(nameof(order));

                var document = new FlowDocument
                {
                    FontFamily = new FontFamily("Arial"),
                    FontSize = 12,
                    PagePadding = new Thickness(20),
                    Background = Brushes.White,
                    FlowDirection = FlowDirection.RightToLeft,
                    ColumnWidth = 300
                };

                // Create a section with a StackPanel to mimic receipt layout
                Section receiptSection = new Section();
                StackPanel mainPanel = new StackPanel
                {
                    Width = 300,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10)
                };

                // Company Header with more spacing
                mainPanel.Children.Add(CreateTextBlock("EXPRESS SERVICE TEAM", FontWeights.Bold, 14, HorizontalAlignment.Right));
                mainPanel.Children.Add(CreateTextBlock("81 / 616919 - 03 / 616919", FontWeights.Normal, 10, HorizontalAlignment.Right));
                mainPanel.Children.Add(CreateTextBlock("طريق المطار - نزلة عين الدولة", FontWeights.Normal, 10, HorizontalAlignment.Right));

                // Separator
                mainPanel.Children.Add(CreateSeparator());

                // Order Number
                mainPanel.Children.Add(CreateTextBlock(order.OrderNumber, FontWeights.Bold, 18, HorizontalAlignment.Center, Colors.Red));

                // Sender Information
                mainPanel.Children.Add(CreateLabeledLine(":المرسل", order.SenderName ?? ""));
                mainPanel.Children.Add(CreateLabeledLine(":هاتف المرسل", order.SenderPhone ?? ""));

                // Recipient Information
                mainPanel.Children.Add(CreateLabeledLine(":المرسل إليه", order.RecipientName ?? ""));
                mainPanel.Children.Add(CreateLabeledLine(":هاتف المرسل إليه", order.RecipientPhone ?? ""));

                // Address
                mainPanel.Children.Add(CreateLabeledLine(":العنوان", $"{order.PickupLocation ?? ""} / {order.DeliveryLocation ?? ""}"));

                // Checkboxes section
                mainPanel.Children.Add(CreateTextBlock("قابل للكسر", FontWeights.Normal, 12, HorizontalAlignment.Right));
                mainPanel.Children.Add(CreateTextBlock("بدل", FontWeights.Normal, 12, HorizontalAlignment.Right));
                mainPanel.Children.Add(CreateTextBlock("مرتجع", FontWeights.Normal, 12, HorizontalAlignment.Right));

                // Notes section
                mainPanel.Children.Add(CreateTextBlock(":ملاحظات", FontWeights.Normal, 12, HorizontalAlignment.Right));
                mainPanel.Children.Add(CreateEmptyLine());

                // Separator
                mainPanel.Children.Add(CreateSeparator());

                // Total Price
                mainPanel.Children.Add(CreateTextBlock($"قيمة الطلب مع التوصيل: {order.TotalPrice:N2} {order.Currency}",
                    FontWeights.Bold, 12, HorizontalAlignment.Right));

                // Disclaimer
                mainPanel.Children.Add(CreateTextBlock("نحن غير مسؤولين عن محتوى الطرد وعن قانونية البضاعة الموجودة داخله",
                    FontWeights.Normal, 10, HorizontalAlignment.Center));

                // Add panel to section
                receiptSection.Blocks.Add(new BlockUIContainer(mainPanel));

                // Add section to document
                document.Blocks.Add(receiptSection);

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating receipt for order {order?.Id}");
                throw;
            }
        }

        // Helper method to create text blocks
        private TextBlock CreateTextBlock(string text, FontWeight weight, double fontSize,
            HorizontalAlignment alignment, Color? color = null)
        {
            return new TextBlock
            {
                Text = text,
                FontWeight = weight,
                FontSize = fontSize,
                HorizontalAlignment = alignment,
                Foreground = color.HasValue ? new SolidColorBrush(color.Value) : Brushes.Black,
                Margin = new Thickness(0, 5, 0, 5)
            };
        }

        // Helper method to create labeled lines
        private TextBlock CreateLabeledLine(string label, string value)
        {
            return CreateTextBlock($"{label}: {value}", FontWeights.Normal, 12, HorizontalAlignment.Right);
        }

        // Helper method to create separator
        private Border CreateSeparator()
        {
            return new Border
            {
                BorderThickness = new Thickness(0, 1, 0, 0),
                BorderBrush = Brushes.Black,
                Margin = new Thickness(0, 10, 0, 10)
            };
        }

        // Helper method to create empty lines
        private TextBlock CreateEmptyLine()
        {
            return new TextBlock
            {
                Text = " ",
                Height = 20
            };
        }
    }
}