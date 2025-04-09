// ExpressServicePOS.UI/Views/CustomerDialog.xaml.cs
using ExpressServicePOS.Core.Models;
using System.Windows;

namespace ExpressServicePOS.UI.Views
{
    /// <summary>
    /// Interaction logic for CustomerDialog.xaml
    /// </summary>
    public partial class CustomerDialog : Window
    {
        // Make Customer property publicly settable
        public Customer Customer { get; set; }


        public CustomerDialog()
        {
            InitializeComponent();
            Customer = new Customer
            {
                Name = string.Empty,
                Address = string.Empty,
                Phone = string.Empty,
                Notes = string.Empty,
                Class = "X"  // Default class is "X"
            };

            // Set default value for Class field
            txtClass.Text = "X";
        }

        /// <summary>
        /// Populates the dialog fields with the current customer data
        /// </summary>
        public void PopulateFields()
        {
            if (Customer != null)
            {
                txtName.Text = Customer.Name;
                txtClass.Text = Customer.Class;
                txtAddress.Text = Customer.Address;
                txtPhone.Text = Customer.Phone;
                txtNotes.Text = Customer.Notes;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("الرجاء إدخال اسم العميل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return;
            }

            // Create customer object
            Customer.Name = txtName.Text;
            Customer.Class = txtClass.Text;
            Customer.Address = txtAddress.Text;
            Customer.Phone = txtPhone.Text;
            Customer.Notes = txtNotes.Text;

            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}