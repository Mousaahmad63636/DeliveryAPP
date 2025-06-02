using ExpressServicePOS.Core.Models;
using ExpressServicePOS.Core.ViewModels;
using ExpressServicePOS.Infrastructure.Services;
using ExpressServicePOS.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace ExpressServicePOS.UI.Controls
{
    public partial class SubscriptionsDueWidget : UserControl
    {
        private readonly IServiceScope _serviceScope;
        private readonly SubscriptionService _subscriptionService;
        private readonly ILogger<SubscriptionsDueWidget> _logger;

        public SubscriptionsDueWidget()
        {
            InitializeComponent();

            try
            {
                _serviceScope = ((App)Application.Current).ServiceProvider.CreateScope();
                _subscriptionService = _serviceScope.ServiceProvider.GetRequiredService<SubscriptionService>();
                _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<SubscriptionsDueWidget>>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing SubscriptionsDueWidget: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Unloaded += (s, e) => _serviceScope?.Dispose();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSubscriptionsDue();
        }

        private async Task LoadSubscriptionsDue()
        {
            try
            {
                // Show loading state
                LoadingState.Visibility = Visibility.Visible;
                EmptyState.Visibility = Visibility.Collapsed;
                lvSubscriptions.Visibility = Visibility.Collapsed;

                // Get subscriptions due in the next 7 days
                var dueSubscriptions = await _subscriptionService.GetDueSubscriptionsAsync(7);

                // Convert to view models
                var viewModels = dueSubscriptions
                    .Select(s => CustomerSubscriptionViewModel.FromModel(s))
                    .OrderBy(s => s.NextPaymentDueDate)
                    .ToList();

                // Update UI
                if (viewModels.Any())
                {
                    lvSubscriptions.ItemsSource = viewModels;
                    lvSubscriptions.Visibility = Visibility.Visible;
                    EmptyState.Visibility = Visibility.Collapsed;
                }
                else
                {
                    lvSubscriptions.Visibility = Visibility.Collapsed;
                    EmptyState.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading due subscriptions");
            }
            finally
            {
                // Hide loading state
                LoadingState.Visibility = Visibility.Collapsed;
            }
        }

        private void ViewAllSubscriptions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Navigate to subscriptions page
                var mainWindow = Window.GetWindow(this);
                if (mainWindow is MainWindow window)
                {
                    var frame = window.FindName("MainFrame") as Frame;
                    if (frame != null)
                    {
                        frame.Navigate(new SubscriptionsPage());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error navigating to subscriptions page");
            }
        }
    }
}