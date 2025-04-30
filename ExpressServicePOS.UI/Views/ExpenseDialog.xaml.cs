using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ExpressServicePOS.UI.Views
{
    public partial class ExpenseDialog : Window
    {
        private readonly Expense _expense;
        private readonly bool _isEdit;
        private readonly IServiceScope _serviceScope;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<ExpenseDialog> _logger;

        public ExpenseDialog()
        {
            InitializeComponent();

            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();
            _dbContextFactory = _serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<ExpenseDialog>>();

            _expense = new Expense
            {
                Date = DateTime.Today,
                Description = string.Empty,
                Category = string.Empty,
                Amount = 0,
                Currency = "USD",
                PaymentMethod = "نقدي",
                Notes = string.Empty
            };

            _isEdit = false;
            txtDialogTitle.Text = "إضافة مصروف جديد";
            dpExpenseDate.SelectedDate = DateTime.Today;

            Closed += (s, e) => _serviceScope.Dispose();
        }

        public ExpenseDialog(Expense expense)
        {
            InitializeComponent();

            _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();
            _dbContextFactory = _serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<ExpenseDialog>>();

            _expense = expense ?? throw new ArgumentNullException(nameof(expense));
            _isEdit = true;
            txtDialogTitle.Text = "تعديل مصروف";

            // Fill in the form with expense data
            dpExpenseDate.SelectedDate = _expense.Date;
            txtDescription.Text = _expense.Description;
            txtAmount.Text = _expense.Amount.ToString("N2");
            txtNotes.Text = _expense.Notes;

            // Set Category
            if (!string.IsNullOrEmpty(_expense.Category))
            {
                bool categoryFound = false;
                foreach (ComboBoxItem item in cmbCategory.Items)
                {
                    if (item.Content.ToString() == _expense.Category)
                    {
                        cmbCategory.SelectedItem = item;
                        categoryFound = true;
                        break;
                    }
                }

                if (!categoryFound)
                {
                    cmbCategory.Text = _expense.Category;
                }
            }

            // Set Currency
            foreach (ComboBoxItem item in cmbCurrency.Items)
            {
                if (item.Content.ToString() == _expense.Currency)
                {
                    cmbCurrency.SelectedItem = item;
                    break;
                }
            }

            // Set Payment Method
            foreach (ComboBoxItem item in cmbPaymentMethod.Items)
            {
                if (item.Content.ToString() == _expense.PaymentMethod)
                {
                    cmbPaymentMethod.SelectedItem = item;
                    break;
                }
            }

            Closed += (s, e) => _serviceScope.Dispose();
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(txtDescription.Text))
                {
                    MessageBox.Show("الرجاء إدخال وصف المصروف", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtDescription.Focus();
                    return;
                }

                if (!decimal.TryParse(txtAmount.Text.Replace(",", ""), out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("الرجاء إدخال مبلغ صحيح وموجب", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtAmount.Focus();
                    return;
                }

                if (!dpExpenseDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("الرجاء اختيار تاريخ المصروف", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    dpExpenseDate.Focus();
                    return;
                }

                // Update expense object
                _expense.Date = dpExpenseDate.SelectedDate.Value;
                _expense.Description = txtDescription.Text;
                _expense.Amount = amount;
                _expense.Notes = txtNotes.Text;

                // Get category
                if (cmbCategory.SelectedItem is ComboBoxItem selectedCategory)
                {
                    _expense.Category = selectedCategory.Content.ToString();
                }
                else
                {
                    _expense.Category = cmbCategory.Text;
                }

                // Get currency
                if (cmbCurrency.SelectedItem is ComboBoxItem selectedCurrency)
                {
                    _expense.Currency = selectedCurrency.Content.ToString();
                }

                // Get payment method
                if (cmbPaymentMethod.SelectedItem is ComboBoxItem selectedPaymentMethod)
                {
                    _expense.PaymentMethod = selectedPaymentMethod.Content.ToString();
                }

                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    if (_isEdit)
                    {
                        dbContext.Expenses.Update(_expense);
                    }
                    else
                    {
                        dbContext.Expenses.Add(_expense);
                    }

                    await dbContext.SaveChangesAsync();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving expense");
                MessageBox.Show($"حدث خطأ أثناء حفظ المصروف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}