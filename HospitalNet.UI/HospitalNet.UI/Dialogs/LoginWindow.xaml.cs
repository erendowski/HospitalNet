using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Principal;
using System.Windows;

namespace HospitalNet.UI.Dialogs
{
    public partial class LoginWindow : Window
    {
        private readonly string _baseConnectionString;
        private readonly bool _isWindowsOnly;

        public LoginWindow(string baseConnectionString, bool defaultWindowsAuth)
        {
            InitializeComponent();

            _baseConnectionString = baseConnectionString ?? throw new ArgumentNullException(nameof(baseConnectionString));

            UseWindowsAuthCheckBox.IsChecked = defaultWindowsAuth;
            ProfileTextBlock.Text = $"DB Profile: {App.ActiveProfile}";

            _isWindowsOnly = TryDetectWindowsOnlyAuthMode(_baseConnectionString);
            if (_isWindowsOnly)
            {
                UseWindowsAuthCheckBox.IsChecked = true;
                UseWindowsAuthCheckBox.IsEnabled = false;
                InfoTextBlock.Text = "This SQL Server is configured for Windows Authentication only. SQL Username/Password logins require Mixed Mode.";
            }

            if (UseWindowsAuthCheckBox.IsChecked == true)
            {
                UsernameTextBox.Text = WindowsIdentity.GetCurrent().Name;
            }

            ApplyAuthModeToInputs();
        }

        public string EffectiveConnectionString { get; private set; } = string.Empty;
        public string SignedInUsername { get; private set; } = string.Empty;

        private void UseWindowsAuthCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ApplyAuthModeToInputs();
        }

        private void ApplyAuthModeToInputs()
        {
            bool useWindowsAuth = UseWindowsAuthCheckBox.IsChecked == true;

            UsernameTextBox.IsEnabled = !useWindowsAuth;
            PasswordBox.IsEnabled = !useWindowsAuth;

            if (useWindowsAuth)
            {
                UsernameTextBox.Text = WindowsIdentity.GetCurrent().Name;
                PasswordBox.Password = string.Empty;
            }
        }

        private static bool TryDetectWindowsOnlyAuthMode(string baseConnectionString)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(baseConnectionString)
                {
                    IntegratedSecurity = true,
                    UserID = string.Empty,
                    Password = string.Empty,
                };

                using var con = new SqlConnection(builder.ConnectionString);
                con.Open();

                using var cmd = con.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT CAST(SERVERPROPERTY('IsIntegratedSecurityOnly') AS int)";

                var value = cmd.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                    return false;

                return Convert.ToInt32(value) == 1;
            }
            catch
            {
                return false;
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = string.Empty;

            try
            {
                bool useWindowsAuth = UseWindowsAuthCheckBox.IsChecked == true;

                if (_isWindowsOnly && !useWindowsAuth)
                {
                    StatusTextBlock.Text = "This SQL Server is Windows-auth only. Please use Windows Authentication, or enable Mixed Mode on the SQL Server.";
                    MessageBox.Show(this,
                        StatusTextBlock.Text,
                        "Login Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                var builder = new SqlConnectionStringBuilder(_baseConnectionString);

                if (useWindowsAuth)
                {
                    builder.IntegratedSecurity = true;
                    builder.UserID = string.Empty;
                    builder.Password = string.Empty;
                    SignedInUsername = WindowsIdentity.GetCurrent().Name ?? string.Empty;
                }
                else
                {
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
                }

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
                    hint = "\n\nHint: If you are using SQL Username/Password, ensure the SQL Server is in Mixed Mode and that the login exists and is mapped to the HospitalNet database.";
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
