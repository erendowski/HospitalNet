using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using HospitalNet.Frontend.Views;

namespace HospitalNet.Frontend
{
    /// <summary>
    /// Main application window.
    /// Provides navigation between different views (Dashboard, Patients, Appointments, Doctors, Analytics).
    /// Hosts the ContentControl that dynamically loads different UserControls based on navigation.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Dictionary mapping view names to their corresponding UserControl instances.
        /// Lazy-loaded to improve startup performance.
        /// </summary>
        private Dictionary<string, UserControl> _viewCache = new Dictionary<string, UserControl>();

        /// <summary>
        /// Timer for updating the timestamp in the top bar.
        /// </summary>
        private System.Windows.Threading.DispatcherTimer _updateTimer;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize the application
            InitializeApplication();
        }

        /// <summary>
        /// Initializes the application window.
        /// Sets up event handlers, timers, and loads the default view.
        /// </summary>
        private void InitializeApplication()
        {
            try
            {
                // Check if application initialized successfully
                if (!App.IsInitialized)
                {
                    MessageBox.Show(
                        "Application failed to initialize. Please check your database connection.",
                        "Initialization Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    this.Close();
                    return;
                }

                // Set up timestamp update timer
                _updateTimer = new System.Windows.Threading.DispatcherTimer();
                _updateTimer.Interval = TimeSpan.FromSeconds(1);
                _updateTimer.Tick += (s, e) => UpdateTimestamp();
                _updateTimer.Start();

                // Initialize view cache
                InitializeViewCache();

                // Load default view (Dashboard)
                NavigateToView("Dashboard");

                System.Diagnostics.Debug.WriteLine("Application initialized successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize application: {ex.Message}",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                this.Close();
            }
        }

        /// <summary>
        /// Pre-initializes view cache with placeholder entries.
        /// Views are loaded on-demand when first accessed.
        /// </summary>
        private void InitializeViewCache()
        {
            _viewCache.Add("Dashboard", null);
            _viewCache.Add("Patients", null);
            _viewCache.Add("Appointments", null);
            _viewCache.Add("Doctors", null);
            _viewCache.Add("Analytics", null);
            _viewCache.Add("Settings", null);
        }

        /// <summary>
        /// Handles navigation button clicks in the sidebar.
        /// Routes to the appropriate view based on the button's Tag property.
        /// </summary>
        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string viewName)
            {
                NavigateToView(viewName);
                HighlightActiveButton(button);
            }
        }

        /// <summary>
        /// Navigates to the specified view.
        /// Loads the view from cache or creates a new instance if not cached.
        /// </summary>
        /// <param name="viewName">The name of the view to navigate to.</param>
        private void NavigateToView(string viewName)
        {
            try
            {
                UserControl view = null;

                // Load view from cache or create new instance
                if (_viewCache.ContainsKey(viewName))
                {
                    view = _viewCache[viewName];
                }

                if (view == null)
                {
                    // Create new view instance based on view name
                    view = CreateView(viewName);
                    
                    // Cache the view for future use
                    if (_viewCache.ContainsKey(viewName))
                    {
                        _viewCache[viewName] = view;
                    }
                }

                // Set the content control to display the view
                ContentFrame.Content = view;

                // Update page title
                PageTitleTextBlock.Text = viewName;

                System.Diagnostics.Debug.WriteLine($"Navigated to: {viewName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load {viewName}: {ex.Message}",
                    "Navigation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Creates a new UserControl instance based on the view name.
        /// This method is a factory for creating different views.
        /// </summary>
        /// <param name="viewName">The name of the view to create.</param>
        /// <returns>A new UserControl instance of the requested view type.</returns>
        private UserControl CreateView(string viewName)
        {
            switch (viewName)
            {
                case "Dashboard":
                    return new DashboardView();

                case "Patients":
                    return new PatientsView();

                case "Appointments":
                    return new AppointmentsView();

                case "Doctors":
                    return new DoctorsView();

                case "Analytics":
                    return new AnalyticsView();

                case "Settings":
                    return new SettingsView();

                default:
                    throw new ArgumentException($"Unknown view: {viewName}");
            }
        }

        /// <summary>
        /// Highlights the active navigation button by changing its border color.
        /// </summary>
        /// <param name="activeButton">The button that was clicked.</param>
        private void HighlightActiveButton(Button activeButton)
        {
            // Remove highlight from all buttons
            foreach (var button in FindVisualChildren<Button>(this))
            {
                if (button.Tag is string)
                {
                    button.BorderBrush = System.Windows.Media.Brushes.Transparent;
                }
            }

            // Highlight the active button
            activeButton.BorderBrush = System.Windows.Media.Brushes.White;
        }

        /// <summary>
        /// Updates the timestamp display in the top bar.
        /// Called by the timer every second.
        /// </summary>
        private void UpdateTimestamp()
        {
            TimestampTextBlock.Text = DateTime.Now.ToString("dddd, MMMM d, yyyy - h:mm:ss tt");
        }

        /// <summary>
        /// Handles the Exit button click.
        /// Prompts user for confirmation before closing the application.
        /// </summary>
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to exit HospitalNet?",
                "Exit Application",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _updateTimer?.Stop();
                this.Close();
            }
        }

        /// <summary>
        /// Finds all visual children of a specific type within the visual tree.
        /// Used to find and manipulate UI elements recursively.
        /// </summary>
        /// <typeparam name="T">The type of visual children to find.</typeparam>
        /// <param name="parent">The parent element to search within.</param>
        /// <returns>An enumerable of all matching visual children.</returns>
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent != null)
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                    if (child is T typedChild)
                    {
                        yield return typedChild;
                    }

                    foreach (T descendant in FindVisualChildren<T>(child))
                    {
                        yield return descendant;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Window Closing event.
        /// Performs cleanup operations before the window closes.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _updateTimer?.Stop();
                System.Diagnostics.Debug.WriteLine("MainWindow closing.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during window closing: {ex.Message}");
            }
        }
    }
}
