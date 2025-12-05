using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using HospitalNet.Backend.Managers;

namespace HospitalNet.Frontend.Views
{
    /// <summary>
    /// Interaction logic for AnalyticsView.xaml
    /// Displays performance metrics and analytics reports
    /// </summary>
    public partial class AnalyticsView : UserControl
    {
        private AnalyticsManager _analyticsManager;
        private AppointmentManager _appointmentManager;

        public AnalyticsView()
        {
            InitializeComponent();
            InitializeManagers();
            SetDateRanges();
            LoadInitialMetrics();
        }

        /// <summary>
        /// Initialize database managers
        /// </summary>
        private void InitializeManagers()
        {
            try
            {
                _analyticsManager = new AnalyticsManager(App.ConnectionString);
                _appointmentManager = new AppointmentManager(App.ConnectionString);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"✗ Error initializing: {ex.Message}";
                MessageBox.Show(
                    $"Failed to initialize analytics:\n{ex.Message}",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Set default date ranges (last 30 days)
        /// </summary>
        private void SetDateRanges()
        {
            EndDatePicker.SelectedDate = DateTime.Today;
            StartDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
        }

        /// <summary>
        /// Load metrics on view initialization
        /// </summary>
        private void LoadInitialMetrics()
        {
            if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
            {
                GenerateReport();
            }
        }

        /// <summary>
        /// Generate Report button click
        /// </summary>
        private void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateReport();
        }

        /// <summary>
        /// Generate analytics report for selected date range
        /// </summary>
        private void GenerateReport()
        {
            try
            {
                if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
                {
                    StatusTextBlock.Text = "⚠ Please select both start and end dates";
                    return;
                }

                DateTime startDate = StartDatePicker.SelectedDate.Value;
                DateTime endDate = EndDatePicker.SelectedDate.Value;

                if (startDate > endDate)
                {
                    StatusTextBlock.Text = "⚠ Start date cannot be after end date";
                    return;
                }

                StatusTextBlock.Text = "⏳ Generating report...";

                // Get appointment statistics
                var appointments = _appointmentManager.GetAppointmentsByDateRange(startDate, endDate);

                int totalAppointments = appointments.Count;
                int completedAppointments = 0;
                int cancelledAppointments = 0;

                foreach (var apt in appointments)
                {
                    if (apt.Status == "Completed")
                        completedAppointments++;
                    else if (apt.Status == "Cancelled")
                        cancelledAppointments++;
                }

                double cancellationRate = totalAppointments > 0 ? (double)cancelledAppointments / totalAppointments * 100 : 0;

                // Update metric cards
                TotalAppointmentsMetric.Text = totalAppointments.ToString();
                CompletedVisitsMetric.Text = completedAppointments.ToString();
                CancellationRateMetric.Text = $"{cancellationRate:F1}%";

                // Get patient count
                try
                {
                    var patientManager = new PatientManager(App.ConnectionString);
                    var patients = patientManager.GetAllActivePatients();
                    PatientLoadMetric.Text = patients.Count.ToString();
                }
                catch
                {
                    PatientLoadMetric.Text = "N/A";
                }

                // Get doctor performance metrics
                try
                {
                    var doctorReport = _analyticsManager.GeneratePerformanceReport(startDate, endDate);
                    
                    var displayMetrics = new ObservableCollection<dynamic>();
                    foreach (var metric in doctorReport)
                    {
                        displayMetrics.Add(new
                        {
                            DoctorName = $"Dr. {metric.FirstName} {metric.LastName}",
                            TotalAppointments = metric.TotalAppointments,
                            CompletedAppointments = metric.CompletedAppointments,
                            CompletionRate = metric.TotalAppointments > 0 ? (double)metric.CompletedAppointments / metric.TotalAppointments : 0,
                            AvgVisitDuration = metric.AvgVisitDuration,
                            PatientSatisfaction = metric.PatientSatisfaction ?? 0
                        });
                    }

                    PerformanceMetricsGrid.ItemsSource = displayMetrics;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to load doctor performance metrics:\n{ex.Message}",
                        "Metrics Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                StatusTextBlock.Text = $"✓ Report generated for {startDate:MM/dd/yyyy} to {endDate:MM/dd/yyyy}";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"✗ Error generating report: {ex.Message}";
                MessageBox.Show(
                    $"Failed to generate report:\n{ex.Message}",
                    "Report Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
