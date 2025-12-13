using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using HospitalNet.Backend.BusinessLogic;
using HospitalNet.Backend.Infrastructure;

namespace HospitalNet.UI.Views
{
    /// <summary>
    /// Displays performance metrics and analytics reports.
    /// </summary>
    public partial class AnalyticsView : UserControl
    {
        private AnalyticsManager _analyticsManager;
        private AppointmentManager _appointmentManager;

        public AnalyticsView()
        {
            InitializeComponent();
            if (App.OfflineMode)
            {
                PerformanceMetricsGrid.ItemsSource = null;
                StatusTextBlock.Text = "Analytics offline (no database connection).";
                return;
            }

            InitializeManagers();
            SetDateRanges();
            LoadInitialMetrics();
        }

        private void InitializeManagers()
        {
            try
            {
                if (App.OfflineMode)
                {
                    _analyticsManager = null;
                    _appointmentManager = null;
                    StatusTextBlock.Text = "Analytics offline (no database connection).";
                    return;
                }

                var dbHelper = new DatabaseHelper(App.ConnectionString);
                if (!dbHelper.TestConnection())
                {
                    _analyticsManager = null;
                    _appointmentManager = null;
                    StatusTextBlock.Text = "Analytics offline (no database connection).";
                    return;
                }

                _analyticsManager = new AnalyticsManager(App.ConnectionString);
                _appointmentManager = new AppointmentManager(App.ConnectionString);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error initializing: {ex.Message}";
                _analyticsManager = null;
                _appointmentManager = null;
            }
        }

        private void SetDateRanges()
        {
            EndDatePicker.SelectedDate = DateTime.Today;
            StartDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
        }

        private void LoadInitialMetrics()
        {
            if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
            {
                GenerateReport();
            }
        }

        private void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateReport();
        }

        private void GenerateReport()
        {
            try
            {
                if (_analyticsManager == null || _appointmentManager == null)
                {
                    StatusTextBlock.Text = "Analytics offline (no database connection).";
                    PerformanceMetricsGrid.ItemsSource = null;
                    TotalAppointmentsMetric.Text = "-";
                    CompletedVisitsMetric.Text = "-";
                    CancellationRateMetric.Text = "-";
                    PatientLoadMetric.Text = "-";
                    return;
                }

                if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
                {
                    StatusTextBlock.Text = "Please select both start and end dates";
                    return;
                }

                DateTime startDate = StartDatePicker.SelectedDate.Value;
                DateTime endDate = EndDatePicker.SelectedDate.Value;

                if (startDate > endDate)
                {
                    StatusTextBlock.Text = "Start date cannot be after end date";
                    return;
                }

                StatusTextBlock.Text = "Generating report...";

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

                TotalAppointmentsMetric.Text = totalAppointments.ToString();
                CompletedVisitsMetric.Text = completedAppointments.ToString();
                CancellationRateMetric.Text = $"{cancellationRate:F1}%";

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

                try
                {
                    var report = _analyticsManager.GeneratePerformanceReport(startDate, endDate);

                    var displayMetrics = new ObservableCollection<dynamic>();
                    foreach (var metric in report.DoctorMetrics)
                    {
                        displayMetrics.Add(new
                        {
                            DoctorName = $"Dr. {metric.FirstName} {metric.LastName}",
                            TotalAppointments = metric.TotalAppointments,
                            CompletedAppointments = metric.CompletedAppointments,
                            CompletionRate = metric.TotalAppointments > 0 ? (double)metric.CompletedAppointments / metric.TotalAppointments : 0,
                            AvgVisitDuration = metric.AverageAppointmentDuration,
                            PatientSatisfaction = 0
                        });
                    }

                    PerformanceMetricsGrid.ItemsSource = displayMetrics;
                }
                catch (Exception ex)
                {
                    PerformanceMetricsGrid.ItemsSource = null;
                    StatusTextBlock.Text = $"Doctor metrics unavailable: {ex.Message}";
                }

                StatusTextBlock.Text = $"Report generated for {startDate:MM/dd/yyyy} to {endDate:MM/dd/yyyy}";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error generating report: {ex.Message}";
            }
        }
    }
}
