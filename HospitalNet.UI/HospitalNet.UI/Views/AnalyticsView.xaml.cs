using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
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
        private PerformanceReport _lastReport;

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
            AiSummaryTextBox.Text = string.Empty;
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
                    _lastReport = report;
                    AiSummaryTextBox.Text = string.Empty;

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
                    _lastReport = null;
                    StatusTextBlock.Text = $"Doctor metrics unavailable: {ex.Message}";
                }

                StatusTextBlock.Text = $"Report generated for {startDate:MM/dd/yyyy} to {endDate:MM/dd/yyyy}";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error generating report: {ex.Message}";
            }
        }

        private async void GenerateAiSummaryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_analyticsManager == null || _appointmentManager == null)
                {
                    StatusTextBlock.Text = "Analytics offline (no database connection).";
                    return;
                }

                if (_lastReport == null)
                {
                    StatusTextBlock.Text = "Generate a report first.";
                    return;
                }

                var baseUrl = ConfigurationManager.AppSettings["AiBaseUrl"] ?? "https://api.openai.com/v1/";
                var model = ConfigurationManager.AppSettings["AiModel"] ?? "gpt-4.1-mini";
                var apiKey = ConfigurationManager.AppSettings["AiApiKey"] ?? string.Empty;

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    StatusTextBlock.Text = "AI API key is not set (AiApiKey).";
                    MessageBox.Show(
                        "Set AiApiKey in HospitalNet.UI/App.config to enable AI summaries.",
                        "Missing API Key",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                GenerateAiSummaryButton.IsEnabled = false;
                StatusTextBlock.Text = "Generating AI summary...";

                var prompt = AiPromptBuilder.BuildAnalyticsPrompt(_lastReport);

                using HttpClient http = AiReportClient.CreateOpenAiCompatibleClient(baseUrl, apiKey);
                var client = new AiReportClient(http, model);
                string summary = await client.GenerateAsync(prompt);

                AiSummaryTextBox.Text = summary;
                StatusTextBlock.Text = "AI summary generated.";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"AI summary error: {ex.Message}";
                MessageBox.Show(
                    $"Failed to generate AI summary:\n{ex.Message}",
                    "AI Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                GenerateAiSummaryButton.IsEnabled = true;
            }
        }
    }
}
