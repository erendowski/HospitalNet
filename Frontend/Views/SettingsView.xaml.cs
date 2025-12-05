using System;
using System.Windows;
using System.Windows.Controls;
using HospitalNet.Backend.Managers;

namespace HospitalNet.Frontend.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// Application settings and configuration options
    /// </summary>
    public partial class SettingsView : UserControl
    {
        private DatabaseHelper _databaseHelper;

        public SettingsView()
        {
            InitializeComponent();
            LoadSettings();
        }

        /// <summary>
        /// Load application settings
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                // Display connection string (masked for security)
                string connectionString = App.ConnectionString;
                if (connectionString.Length > 50)
                {
                    ConnectionStringTextBox.Text = connectionString.Substring(0, 50) + "...";
                }
                else
                {
                    ConnectionStringTextBox.Text = connectionString;
                }

                // Test connection on load
                TestConnection();

                // Get database version
                try
                {
                    _databaseHelper = new DatabaseHelper(App.ConnectionString);
                    // In production, you would query the database version
                    DatabaseVersionTextBlock.Text = "MSSQL Server 2019+";
                }
                catch
                {
                    DatabaseVersionTextBlock.Text = "Unable to determine";
                }

                StatusTextBlock.Text = "Settings loaded successfully";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"✗ Error loading settings: {ex.Message}";
            }
        }

        /// <summary>
        /// Test database connection
        /// </summary>
        private void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            TestConnection();
        }

        /// <summary>
        /// Internal method to test connection
        /// </summary>
        private void TestConnection()
        {
            try
            {
                _databaseHelper = new DatabaseHelper(App.ConnectionString);
                bool isConnected = _databaseHelper.TestConnection();

                if (isConnected)
                {
                    ConnectionStatusTextBlock.Text = "Status: ✓ Connected";
                    ConnectionStatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush");
                    StatusTextBlock.Text = "Database connection successful";
                }
                else
                {
                    ConnectionStatusTextBlock.Text = "Status: ✗ Failed";
                    ConnectionStatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush");
                    StatusTextBlock.Text = "Database connection failed";
                }
            }
            catch (Exception ex)
            {
                ConnectionStatusTextBlock.Text = "Status: ✗ Error";
                ConnectionStatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush");
                StatusTextBlock.Text = $"Connection error: {ex.Message}";

                MessageBox.Show(
                    $"Failed to test connection:\n{ex.Message}",
                    "Connection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
