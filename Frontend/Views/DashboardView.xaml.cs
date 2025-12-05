using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HospitalNet.Backend.Managers;

namespace HospitalNet.Frontend.Views
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// Main dashboard showing hospital overview and today's appointments
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
            InitializeManagers();
            LoadDashboardData();
            StartAutoRefresh();
        }

        /// <summary>
        /// Initialize database managers
        /// </summary>
        private void InitializeManagers()
        {
            try
            {
                _doctorManager = new DoctorManager(App.ConnectionString);
                _patientManager = new PatientManager(App.ConnectionString);
                _appointmentManager = new AppointmentManager(App.ConnectionString);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"✗ Error initializing: {ex.Message}";
                MessageBox.Show(
                    $"Failed to initialize dashboard:\n{ex.Message}",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Load all dashboard data
        /// </summary>
        private void LoadDashboardData()
        {
            try
            {
                // Update date/time
                DateTimeTextBlock.Text = DateTime.Now.ToString("dddd, MMMM d, yyyy - h:mm tt");

                // Get today's appointments
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

                // Get doctor count
                try
                {
                    var doctors = _doctorManager.GetAllDoctors();
                    ActiveDoctorsMetric.Text = doctors.Count.ToString();
                }
                catch
                {
                    ActiveDoctorsMetric.Text = "N/A";
                }

                // Get patient count
                try
                {
                    var patients = _patientManager.GetAllActivePatients();
                    TotalPatientsMetric.Text = patients.Count.ToString();
                }
                catch
                {
                    TotalPatientsMetric.Text = "N/A";
                }

                StatusTextBlock.Text = $"✓ Dashboard updated at {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"✗ Error loading dashboard: {ex.Message}";
                MessageBox.Show(
                    $"Failed to load dashboard data:\n{ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Start auto-refresh timer (refresh every 60 seconds)
        /// </summary>
        private void StartAutoRefresh()
        {
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(60);
            _refreshTimer.Tick += (s, e) => LoadDashboardData();
            _refreshTimer.Start();
        }

        /// <summary>
        /// Cleanup on unload
        /// </summary>
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
            }
        }
    }
}
