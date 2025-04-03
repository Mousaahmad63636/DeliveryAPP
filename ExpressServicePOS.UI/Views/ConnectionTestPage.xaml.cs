using System.Windows.Controls;

namespace ExpressServicePOS.UI.Views
{
    public partial class ConnectionTestPage : Page
    {
        public ConnectionTestPage(string message, int customerCount, int orderCount, int driverCount = 0)
        {
            InitializeComponent();

            txtConnectionStatus.Text = message;
            txtCustomerCount.Text = customerCount.ToString();
            txtOrderCount.Text = orderCount.ToString();
            txtDriverCount.Text = driverCount.ToString();
        }
    }
}