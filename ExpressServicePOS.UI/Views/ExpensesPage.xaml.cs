using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ExpressServicePOS.UI.Views
{
    public class ExpenseViewModel
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }
        public string Notes { get; set; }
        public string PaymentMethod { get; set; }
        public string Currency { get; set; }

        // Formatted amount property for display
        public string AmountFormatted => Currency == "USD" ?
            $"{Amount:N2} $" :
            $"{Amount:N0} ل.ل";
    }

    public partial class ExpensesPage : Page
    {
        private readonly IServiceScope _serviceScope;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<ExpensesPage> _logger;
        private List<ExpenseViewModel> _expenses;
        private List<ExpenseViewModel> _filteredExpenses;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private bool _isLoading = false;

        public ExpensesPage()
        {
            InitializeComponent();

            try
            {
                _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();
                _dbContextFactory = _serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<ExpensesPage>>();

                // Set default date to today
                var today = DateTime.Today;
                _startDate = today;
                _endDate = today;

                dpStartDate.SelectedDate = _startDate;
                dpEndDate.SelectedDate = _endDate;

                Unloaded += ExpensesPage_Unloaded;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تهيئة الصفحة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExpensesPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadExpensesAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading page data");
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExpensesPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _serviceScope?.Dispose();
        }

        private async void LoadExpensesAsync()
        {
            if (_isLoading) return;

            _isLoading = true;
            try
            {
                ShowProgressBar(true);

                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    var expenses = await dbContext.Expenses
                        .OrderByDescending(e => e.Date)
                        .ToListAsync();

                    _expenses = expenses.Select(e => new ExpenseViewModel
                    {
                        Id = e.Id,
                        Date = e.Date,
                        Description = e.Description,
                        Amount = e.Amount,
                        Category = e.Category,
                        Notes = e.Notes,
                        PaymentMethod = e.PaymentMethod,
                        Currency = e.Currency
                    }).ToList();

                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading expenses");
                MessageBox.Show($"خطأ أثناء تحميل المصروفات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowProgressBar(false);
                _isLoading = false;
            }
        }

        private void ShowProgressBar(bool show)
        {
            if (loadingOverlay != null)
            {
                loadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ApplyFilters()
        {
            try
            {
                if (_expenses == null)
                {
                    _filteredExpenses = new List<ExpenseViewModel>();
                    dgExpenses.ItemsSource = _filteredExpenses;
                    UpdateSummary();
                    return;
                }

                var filteredExpenses = new List<ExpenseViewModel>(_expenses);

                if (_startDate.HasValue)
                {
                    filteredExpenses = filteredExpenses.Where(e => e.Date.Date >= _startDate.Value.Date).ToList();
                }

                if (_endDate.HasValue)
                {
                    filteredExpenses = filteredExpenses.Where(e => e.Date.Date <= _endDate.Value.Date).ToList();
                }

                _filteredExpenses = filteredExpenses;
                dgExpenses.ItemsSource = _filteredExpenses;

                UpdateSummary();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error applying filters");
                MessageBox.Show($"خطأ في تطبيق الفلتر: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummary()
        {
            try
            {
                if (_filteredExpenses == null || !_filteredExpenses.Any())
                {
                    txtTotalUSD.Text = "0.00";
                    txtTotalLBP.Text = "0";
                    txtExpenseCount.Text = "0";
                    return;
                }

                decimal totalUSD = _filteredExpenses
                    .Where(e => e.Currency == "USD")
                    .Sum(e => e.Amount);

                decimal totalLBP = _filteredExpenses
                    .Where(e => e.Currency == "LBP")
                    .Sum(e => e.Amount);

                int count = _filteredExpenses.Count;

                txtTotalUSD.Text = totalUSD.ToString("N2");
                txtTotalLBP.Text = totalLBP.ToString("N0");
                txtExpenseCount.Text = count.ToString();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating summary");
            }
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in date filter changed");
                MessageBox.Show($"خطأ في تغيير التاريخ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnResetDateFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var today = DateTime.Today;
                dpStartDate.SelectedDate = today;
                dpEndDate.SelectedDate = today;
                _startDate = today;
                _endDate = today;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error resetting date filter");
                MessageBox.Show($"خطأ في إعادة تعيين الفلتر: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddExpense_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new ExpenseDialog();
                if (dialog.ShowDialog() == true)
                {
                    LoadExpensesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding new expense");
                MessageBox.Show($"خطأ في إضافة مصروف جديد: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgExpenses_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (dgExpenses.SelectedItem is ExpenseViewModel selectedExpense)
                {
                    EditExpense(selectedExpense.Id);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in datagrid double click");
                MessageBox.Show($"خطأ في فتح المصروف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int expenseId)
                {
                    EditExpense(expenseId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in edit button click");
                MessageBox.Show($"خطأ في تعديل المصروف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditExpense(int expenseId)
        {
            try
            {
                ShowProgressBar(true);

                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    var expense = await dbContext.Expenses.FindAsync(expenseId);
                    if (expense == null)
                    {
                        MessageBox.Show("المصروف غير موجود", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var dialog = new ExpenseDialog(expense);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadExpensesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error editing expense");
                MessageBox.Show($"خطأ أثناء تعديل المصروف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowProgressBar(false);
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int expenseId)
                {
                    var result = MessageBox.Show("هل أنت متأكد من حذف هذا المصروف؟", "تأكيد الحذف",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        ShowProgressBar(true);

                        using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                        {
                            var expense = await dbContext.Expenses.FindAsync(expenseId);
                            if (expense != null)
                            {
                                dbContext.Expenses.Remove(expense);
                                await dbContext.SaveChangesAsync();
                                LoadExpensesAsync();
                                MessageBox.Show("تم حذف المصروف بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show("المصروف غير موجود أو ربما تم حذفه مسبقاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting expense");
                MessageBox.Show($"خطأ أثناء حذف المصروف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowProgressBar(false);
            }
        }
    }
}