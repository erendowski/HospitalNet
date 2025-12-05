using System;
using System.Windows;
using HospitalNet.Frontend.Utilities;

namespace HospitalNet.Frontend
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
                // Initialize connection string
                // TODO: Replace with actual connection string from configuration file or app.config
                ConnectionString = GetConnectionString();

                // Validate database connectivity
                if (!ValidateDatabaseConnection())
                {
                    MessageBox.Show(
                        "Failed to connect to the database. Please check your connection settings.",
                        "Database Connection Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    IsInitialized = false;
                    this.Shutdown(1);
                    return;
                }

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
                this.Shutdown(1);
            }
        }

        /// <summary>
        /// Gets the database connection string.
        /// In production, this should be read from app.config or appsettings.json.
        /// </summary>
        /// <returns>The database connection string.</returns>
        private string GetConnectionString()
        {
            // TODO: Replace with actual configuration source
            // Options:
            // 1. Read from app.config: ConfigurationManager.ConnectionStrings["HospitalNet"].ConnectionString
            // 2. Read from appsettings.json (if using .NET Core config)
            // 3. Use environment variables for sensitive data
            // 4. Use Azure Key Vault for production

            // For development/testing:
            string connectionString = "Server=.;Database=HospitalNet;Integrated Security=true;Connection Timeout=30;";

            // Alternative with SQL Authentication:
            // string connectionString = "Server=SERVER_NAME;Database=HospitalNet;User Id=sa;Password=your_password;Connection Timeout=30;";

            return connectionString;
        }

        /// <summary>
        /// Validates database connectivity using the configured connection string.
        /// Attempts to open a connection to ensure the database is accessible.
        /// </summary>
        /// <returns>True if database connection is successful; false otherwise.</returns>
        private bool ValidateDatabaseConnection()
        {
            try
            {
                // Create a DatabaseHelper instance and test the connection
                var dbHelper = new HospitalNet.Backend.Infrastructure.DatabaseHelper(ConnectionString);
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
            // Cleanup operations (if needed)
            // Example: Close any open connections, save user preferences, etc.
            System.Diagnostics.Debug.WriteLine("Application shutting down.");
        }

        /// <summary>
        /// Handles unhandled exceptions in the application.
        /// Prevents application crash and logs the error.
        /// </summary>
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // Log the exception
            System.Diagnostics.Debug.WriteLine($"Unhandled Exception: {e.Exception.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack Trace: {e.Exception.StackTrace}");

            // Show error message to user
            MessageBox.Show(
                $"An unexpected error occurred: {e.Exception.Message}\n\nPlease contact support if the problem persists.",
                "Application Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Mark the exception as handled (prevent application crash)
            e.Handled = true;
        }
    }
}
