using ExpressServicePOS.Core.Models;
using System;
using System.Windows;

namespace ExpressServicePOS.UI.Views
{
    /// <summary>
    /// Interaction logic for DriverDialog.xaml
    /// </summary>
    public partial class DriverDialog : Window
    {
        // Make Driver property publicly settable
        public Driver Driver { get; set; }

        public DriverDialog()
        {
            InitializeComponent();

            // Initialize with default values
            Driver = new Driver
            {
                Name = string.Empty,
                Phone = string.Empty,
                Email = string.Empty,
                VehicleType = string.Empty,
                VehiclePlateNumber = string.Empty,
                AssignedZones = string.Empty,
                IsActive = true,
                DateHired = DateTime.Now,
                Notes = string.Empty
            };

            // Set default date
            dpDateHired.SelectedDate = DateTime.Now;
        }

        /// <summary>
        /// Populates the dialog fields with the current driver data
        /// </summary>
        public void PopulateFields()
        {
            if (Driver != null)
            {
                txtName.Text = Driver.Name;
                txtPhone.Text = Driver.Phone;
                txtEmail.Text = Driver.Email;
                txtVehicleType.Text = Driver.VehicleType;
                txtVehiclePlateNumber.Text = Driver.VehiclePlateNumber;
                txtAssignedZones.Text = Driver.AssignedZones;
                chkIsActive.IsChecked = Driver.IsActive;
                dpDateHired.SelectedDate = Driver.DateHired;
                txtNotes.Text = Driver.Notes;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("الرجاء إدخال اسم السائق", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                MessageBox.Show("الرجاء إدخال رقم هاتف السائق", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPhone.Focus();
                return;
            }

            // Update driver object
            Driver.Name = txtName.Text;
            Driver.Phone = txtPhone.Text;
            Driver.Email = txtEmail.Text;
            Driver.VehicleType = txtVehicleType.Text;
            Driver.VehiclePlateNumber = txtVehiclePlateNumber.Text;
            Driver.AssignedZones = txtAssignedZones.Text;
            Driver.IsActive = chkIsActive.IsChecked ?? true;
            Driver.DateHired = dpDateHired.SelectedDate ?? DateTime.Now;
            Driver.Notes = txtNotes.Text;

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