using HospitalNet.Backend.Infrastructure;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;

namespace HospitalNet.UI
{
    /// <summary>
    /// Application entry point for HospitalNet WPF application.
    /// Handles startup configuration, connection string initialization, and application-level resources.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Application-wide connection string for database access.
        /// Shared across all Manager instances.
        /// </summary>
        public static string ConnectionString { get; private set; }

        /// <summary>
        /// True when the app is intentionally running without a live database.
        /// This lets the UI load instantly instead of timing out on every tab.
        /// </summary>
        // Set false to attempt live DB connection by default.
        public static bool OfflineMode { get; private set; } = false;

        /// <summary>
        /// Indicates whether the application initialized successfully.
        /// </summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>
        /// Handles the application startup event.
        /// Initializes connection string and validates database connectivity.
        /// </summary>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                var profile = ActiveProfile;    
                ConnectionString = GetConnectionString();

                using var con = new SqlConnection(ConnectionString);
                con.Open();

                // Try DB; if it fails, exit with an explicit message.
                var dbHelper = new DatabaseHelper(ConnectionString);
                if (!dbHelper.TestConnection())
                {
                    MessageBox.Show(
                        "Failed to connect to the database. Please check your connection settings.",
                        "Database Connection Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    IsInitialized = false;
                    Shutdown(1);
                    return;
                }

                OfflineMode = false;

                IsInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Application initialization failed: {ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                IsInitialized = false;
                Shutdown(1);
            }
        }

        public static string ActiveProfile =>
            ConfigurationManager.AppSettings["DbProfile"] ?? "Azure";
        /// <summary>
        /// Gets the database connection string.
        /// In production, this should be read from app.config or appsettings.json.
        /// </summary>
        public static string GetConnectionString()
        {
            return ActiveProfile.Equals("Local", StringComparison.OrdinalIgnoreCase)
                ? ConfigurationManager.ConnectionStrings["LocalSql"].ConnectionString
                : ConfigurationManager.ConnectionStrings["AzureSql"].ConnectionString;
        }

        /// <summary>
        /// Validates database connectivity using the configured connection string.
        /// Attempts to open a connection to ensure the database is accessible.
        /// </summary>
        private bool ValidateDatabaseConnection()
        {
            try
            {
                var dbHelper = new DatabaseHelper(ConnectionString);
                return dbHelper.TestConnection();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database connection validation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handles the application exit event.
        /// Performs cleanup operations before application shutdown.
        /// </summary>
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Application shutting down.");
        }

        /// <summary>
        /// Handles unhandled exceptions in the application.
        /// Prevents application crash and logs the error.
        /// </summary>
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Unhandled Exception: {e.Exception.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack Trace: {e.Exception.StackTrace}");

            MessageBox.Show(
                $"An unexpected error occurred: {e.Exception.Message}\n\nPlease contact support if the problem persists.",
                "Application Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }
    }
}
