using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HospitalNet.Backend.BusinessLogic;
using HospitalNet.Backend.Infrastructure;

namespace HospitalNet.UI.Views
{
    /// <summary>
    /// Dashboard showing hospital overview and today's appointments.
    /// </summary>
    public partial class DashboardView : UserControl
    {
        private DoctorManager _doctorManager;
        private PatientManager _patientManager;
        private AppointmentManager _appointmentManager;
        private DispatcherTimer _refreshTimer;

        public DashboardView()
        {
            InitializeComponent();
            if (App.OfflineMode)
            {
                SetOfflineState("Dashboard offline (no database connection).");
                return;
            }

            InitializeManagers();

            if (App.OfflineMode || _doctorManager == null || _patientManager == null || _appointmentManager == null)
            {
                SetOfflineState("Dashboard offline (no database connection).");
                return;
            }

            LoadDashboardData();
            StartAutoRefresh();
        }

        private void InitializeManagers()
        {
            try
            {
                if (App.OfflineMode)
                {
                    SetOfflineState("Dashboard offline (no database connection).");
                    return;
                }

                var dbHelper = new DatabaseHelper(App.ConnectionString);
                if (!dbHelper.TestConnection())
                {
                    SetOfflineState("Dashboard offline (no database connection).");
                    return;
                }

                _doctorManager = new DoctorManager(App.ConnectionString);
                _patientManager = new PatientManager(App.ConnectionString);
                _appointmentManager = new AppointmentManager(App.ConnectionString);
            }
            catch (Exception ex)
            {
                SetOfflineState($"Dashboard offline (DB unavailable): {ex.Message}");
            }
        }

        private void LoadDashboardData()
        {
            try
            {
                if (App.OfflineMode || _doctorManager == null || _patientManager == null || _appointmentManager == null)
                {
                    SetOfflineState("Dashboard offline (no database connection).");
                    return;
                }

                DateTimeTextBlock.Text = DateTime.Now.ToString("dddd, MMMM d, yyyy - h:mm tt");

                var todayAppointments = _appointmentManager.GetAppointmentsByDate(DateTime.Today);

                int completedToday = 0;
                var displayAppointments = new ObservableCollection<dynamic>();

                foreach (var apt in todayAppointments)
                {
                    if (apt.Status == "Completed")
                        completedToday++;

                    var doctor = _doctorManager.GetDoctorByID(apt.DoctorID);
                    var patient = _patientManager.GetPatientByID(apt.PatientID);

                    string doctorName = doctor != null ? $"Dr. {doctor.FirstName} {doctor.LastName}" : "Unknown";
                    string patientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "Unknown";

                    displayAppointments.Add(new
                    {
                        AppointmentID = apt.AppointmentID,
                        AppointmentDateTime = apt.AppointmentDateTime,
                        DoctorName = doctorName,
                        PatientName = patientName,
                        ReasonForVisit = apt.ReasonForVisit,
                        Status = apt.Status
                    });
                }

                TodayAppointmentsGrid.ItemsSource = displayAppointments;
                TodayAppointmentsMetric.Text = todayAppointments.Count.ToString();
                CompletedTodayMetric.Text = completedToday.ToString();

                try
                {
                    var doctors = _doctorManager.GetAllDoctors();
                    ActiveDoctorsMetric.Text = doctors.Count.ToString();
                }
                catch
                {
                    ActiveDoctorsMetric.Text = "N/A";
                }

                try
                {
                    var patients = _patientManager.GetAllActivePatients();
                    TotalPatientsMetric.Text = patients.Count.ToString();
                }
                catch
                {
                    TotalPatientsMetric.Text = "N/A";
                }

                StatusTextBlock.Text = $"Updated at {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                SetOfflineState($"Dashboard offline (data unavailable): {ex.Message}");
            }
        }

        private void StartAutoRefresh()
        {
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
            _refreshTimer.Tick += (s, e) => LoadDashboardData();
            _refreshTimer.Start();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _refreshTimer?.Stop();
        }

        private void SetOfflineState(string statusMessage)
        {
            _doctorManager = null;
            _patientManager = null;
            _appointmentManager = null;

            TodayAppointmentsGrid.ItemsSource = null;
            TodayAppointmentsMetric.Text = "-";
            CompletedTodayMetric.Text = "-";
            ActiveDoctorsMetric.Text = "-";
            TotalPatientsMetric.Text = "-";
            StatusTextBlock.Text = statusMessage;
        }
    }
}
