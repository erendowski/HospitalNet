using System;
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
                    TotalAppointmentsMetric.Text = "-";
                    CompletedVisitsMetric.Text = "-";
                    CancellationRateMetric.Text = "-";
                    PatientLoadMetric.Text = "-";
                    _lastReport = null;
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
                int scheduledAppointments = 0;

                foreach (var apt in appointments)
                {
                    if (apt.Status == "Completed")
                        completedAppointments++;
                    else if (apt.Status == "Cancelled")
                        cancelledAppointments++;
                    else if (apt.Status == "Scheduled")
                        scheduledAppointments++;
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
                    _lastReport = _analyticsManager.GeneratePerformanceReport(startDate, endDate);
                }
                catch (Exception ex)
                {
                    // Fallback report so AI summary can still be generated (doctor metrics may be unavailable due to permissions).
                    int totalActivePatients = 0;
                    int patientsWithAppointments = 0;
                    int averagePatientsPerDoctor = 0;

                    try
                    {
                        var patientManager = new PatientManager(App.ConnectionString);
                        totalActivePatients = patientManager.GetAllActivePatients().Count;
                        patientsWithAppointments = new System.Collections.Generic.HashSet<int>(appointments.ConvertAll(a => a.PatientID)).Count;
                    }
                    catch
                    {
                        // ignore
                    }

                    try
                    {
                        var doctorManager = new DoctorManager(App.ConnectionString);
                        int doctorCount = doctorManager.GetAllDoctors().Count;
                        averagePatientsPerDoctor = doctorCount > 0 ? (int)Math.Round((double)patientsWithAppointments / doctorCount) : 0;
                    }
                    catch
                    {
                        // ignore
                    }

                    _lastReport = new PerformanceReport
                    {
                        ReportDate = DateTime.Now,
                        StartDate = startDate,
                        EndDate = endDate,
                        AppointmentStats = new AppointmentStatistics
                        {
                            StartDate = startDate,
                            EndDate = endDate,
                            TotalAppointments = totalAppointments,
                            ScheduledAppointments = scheduledAppointments,
                            CompletedAppointments = completedAppointments,
                            CancelledAppointments = cancelledAppointments,
                            NoShowAppointments = 0,
                            CancellationRate = totalAppointments > 0 ? (int)Math.Round((double)cancelledAppointments * 100 / totalAppointments) : 0,
                            CompletionRate = totalAppointments > 0 ? (int)Math.Round((double)completedAppointments * 100 / totalAppointments) : 0
                        },
                        PatientLoadStats = new PatientLoadStatistics
                        {
                            StartDate = startDate,
                            EndDate = endDate,
                            TotalActivePatients = totalActivePatients,
                            NewPatientsRegistered = 0,
                            PatientsWithAppointments = patientsWithAppointments,
                            AveragePatientsPerDoctor = averagePatientsPerDoctor,
                            TotalUniqueVisitors = patientsWithAppointments
                        },
                        DoctorMetrics = new System.Collections.Generic.List<DoctorPerformanceMetrics>(),
                        SpecializationStats = new System.Collections.Generic.List<SpecializationStatistics>(),
                        PeakTimes = new System.Collections.Generic.List<HourlyAppointmentStatistics>()
                    };

                    StatusTextBlock.Text = $"Limited report generated (DB metrics unavailable): {ex.Message}";
                }

                AiSummaryTextBox.Text = string.Empty;
                if (_lastReport != null && !StatusTextBlock.Text.StartsWith("Limited report generated", StringComparison.OrdinalIgnoreCase))
                {
                    StatusTextBlock.Text = $"Report generated for {startDate:MM/dd/yyyy} to {endDate:MM/dd/yyyy}";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error generating report: {ex.Message}";
                _lastReport = null;
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
