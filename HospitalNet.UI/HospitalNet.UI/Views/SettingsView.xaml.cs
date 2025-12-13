using System;
using System.Windows;
using System.Windows.Controls;
using HospitalNet.Backend.Infrastructure;

namespace HospitalNet.UI.Views
{
    /// <summary>
    /// Application settings and configuration options.
    /// </summary>
    public partial class SettingsView : UserControl
    {
        private DatabaseHelper _databaseHelper;

        public SettingsView()
        {
            InitializeComponent();
            if (App.OfflineMode)
            {
                ConnectionStringTextBox.Text = TrimConnectionString(App.ConnectionString);
                ConnectionStatusTextBlock.Text = "Status: Offline";
                ConnectionStatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush");
                DatabaseVersionTextBlock.Text = "Offline";
                StatusTextBlock.Text = "Settings offline (no database connection).";
                return;
            }

            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                string connectionString = App.ConnectionString;
                ConnectionStringTextBox.Text = TrimConnectionString(connectionString);

                TestConnection();

                try
                {
                    _databaseHelper = new DatabaseHelper(App.ConnectionString);
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
                StatusTextBlock.Text = $"Error loading settings: {ex.Message}";
            }
        }

        private void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.OfflineMode)
            {
                ConnectionStatusTextBlock.Text = "Status: Offline";
                ConnectionStatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush");
                StatusTextBlock.Text = "Cannot test while offline.";
                return;
            }

            TestConnection();
        }

        private void TestConnection()
        {
            try
            {
                if (App.OfflineMode)
                {
                    ConnectionStatusTextBlock.Text = "Status: Offline";
                    ConnectionStatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush");
                    StatusTextBlock.Text = "Cannot test while offline.";
                    return;
                }

                _databaseHelper = new DatabaseHelper(App.ConnectionString);
                bool isConnected = _databaseHelper.TestConnection();

                if (isConnected)
                {
                    ConnectionStatusTextBlock.Text = "Status: Connected";
                    ConnectionStatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush");
                    StatusTextBlock.Text = "Database connection successful";
                }
                else
                {
                    ConnectionStatusTextBlock.Text = "Status: Failed";
                    ConnectionStatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush");
                    StatusTextBlock.Text = "Database connection failed";
                }
            }
            catch (Exception ex)
            {
                ConnectionStatusTextBlock.Text = "Status: Error";
                ConnectionStatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush");
                StatusTextBlock.Text = $"Connection error: {ex.Message}";

                MessageBox.Show(
                    $"Failed to test connection:\n{ex.Message}",
                    "Connection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string TrimConnectionString(string connectionString)
        {
            return connectionString.Length > 50 ? connectionString[..50] + "..." : connectionString;
        }
    }
}
