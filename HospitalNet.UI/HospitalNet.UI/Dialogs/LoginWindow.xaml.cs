using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace HospitalNet.UI.Dialogs
{
    public partial class LoginWindow : Window
    {
        private readonly string _baseConnectionString;

        public LoginWindow(string baseConnectionString)
        {
            InitializeComponent();

            _baseConnectionString = baseConnectionString ?? throw new ArgumentNullException(nameof(baseConnectionString));

            ProfileTextBlock.Text = $"DB Profile: {App.ActiveProfile}";
            InfoTextBlock.Text = "Sign in using a SQL username and password. If your SQL Server is Windows-auth only, enable Mixed Mode authentication on the server.";
        }

        public string EffectiveConnectionString { get; private set; } = string.Empty;
        public string SignedInUsername { get; private set; } = string.Empty;

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = string.Empty;

            try
            {
                var builder = new SqlConnectionStringBuilder(_baseConnectionString);

                var username = (UsernameTextBox.Text ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(username))
                {
                    StatusTextBlock.Text = "Username is required.";
                    MessageBox.Show(this,
                        StatusTextBlock.Text,
                        "Login Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    UsernameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    StatusTextBlock.Text = "Password is required.";
                    MessageBox.Show(this,
                        StatusTextBlock.Text,
                        "Login Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    PasswordBox.Focus();
                    return;
                }

                builder.IntegratedSecurity = false;
                builder.UserID = username;
                builder.Password = PasswordBox.Password;
                SignedInUsername = username;

                using (var con = new SqlConnection(builder.ConnectionString))
                {
                    con.Open();
                }

                EffectiveConnectionString = builder.ConnectionString;
                DialogResult = true;
                Close();
            }
            catch (SqlException ex)
            {
                string hint = string.Empty;

                if (ex.Number == 18456 || ex.Number == 18452 || ex.Number == 233)
                {
                    hint = "\n\nHint: Ensure your SQL Server is in Mixed Mode and that the login exists and is mapped to the HospitalNet database.";
                }

                StatusTextBlock.Text = $"Login failed (SQL error {ex.Number}): {ex.Message}{hint}";
                MessageBox.Show(this,
                    StatusTextBlock.Text,
                    "Login Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Login failed: {ex.Message}";
                MessageBox.Show(this,
                    StatusTextBlock.Text,
                    "Login Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
