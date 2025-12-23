using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HospitalNet.Backend.BusinessLogic;
using HospitalNet.Backend.Infrastructure;
using HospitalNet.Backend.Models;

namespace HospitalNet.UI.Views
{
    /// <summary>
    /// Dashboard showing hospital overview and today's appointments.
    /// </summary>
    public partial class DashboardView : UserControl
    {
        private sealed class DashboardBarItem
        {
            public string Label { get; set; }
            public string CountText { get; set; }
            public double Fraction { get; set; }
        }

        private sealed class AppointmentCardItem
        {
            public int AppointmentID { get; set; }
            public DateTime AppointmentDateTime { get; set; }
            public string DoctorName { get; set; }
            public string PatientName { get; set; }
            public string ReasonForVisit { get; set; }
            public string Status { get; set; }

            public string TimeText => AppointmentDateTime.ToString("HH:mm");
        }

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

                var doctorsById = new System.Collections.Generic.Dictionary<int, Doctor>();
                var patientsById = new System.Collections.Generic.Dictionary<int, Patient>();

                // Always compute metrics for today
                var todayAppointments = _appointmentManager.GetAppointmentsByDate(DateTime.Today);
                int completedToday = 0;
                foreach (var apt in todayAppointments)
                {
                    if (apt.Status == "Completed")
                        completedToday++;
                }

                TodayAppointmentsMetric.Text = todayAppointments.Count.ToString();
                CompletedTodayMetric.Text = completedToday.ToString();

                try
                {
                    var doctors = _doctorManager.GetAllDoctors();
                    ActiveDoctorsMetric.Text = doctors.Count.ToString();
                    foreach (var doctor in doctors)
                    {
                        doctorsById[doctor.DoctorID] = doctor;
                    }

                    // Doctors by specialization (top 6)
                    var specializationGroups = doctors
                        .GroupBy(d => string.IsNullOrWhiteSpace(d.Specialization) ? "Unspecified" : d.Specialization.Trim())
                        .Select(g => new { Label = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(6)
                        .ToList();

                    int specTotal = specializationGroups.Sum(x => x.Count);
                    DoctorsBySpecializationItems.ItemsSource = specializationGroups.Select(x => new DashboardBarItem
                    {
                        Label = x.Label,
                        CountText = x.Count.ToString(),
                        Fraction = specTotal == 0 ? 0 : (double)x.Count / specTotal
                    }).ToList();
                }
                catch
                {
                    ActiveDoctorsMetric.Text = "N/A";
                    DoctorsBySpecializationItems.ItemsSource = null;
                }

                try
                {
                    var patients = _patientManager.GetAllActivePatients();
                    TotalPatientsMetric.Text = patients.Count.ToString();
                    foreach (var patient in patients)
                    {
                        patientsById[patient.PatientID] = patient;
                    }

                    // Patients by gender
                    var genderGroups = patients
                        .GroupBy(p => string.IsNullOrWhiteSpace(p.Gender) ? "Unspecified" : p.Gender.Trim())
                        .Select(g => new { Label = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .ToList();

                    int genderTotal = genderGroups.Sum(x => x.Count);
                    PatientsByGenderItems.ItemsSource = genderGroups.Select(x => new DashboardBarItem
                    {
                        Label = x.Label,
                        CountText = x.Count.ToString(),
                        Fraction = genderTotal == 0 ? 0 : (double)x.Count / genderTotal
                    }).ToList();
                }
                catch
                {
                    TotalPatientsMetric.Text = "N/A";
                    PatientsByGenderItems.ItemsSource = null;
                }

                var displayAppointments = new ObservableCollection<AppointmentCardItem>();

                foreach (var apt in todayAppointments)
                {
                    if (!doctorsById.TryGetValue(apt.DoctorID, out var doctor))
                    {
                        doctor = _doctorManager.GetDoctorByID(apt.DoctorID);
                        if (doctor != null)
                        {
                            doctorsById[doctor.DoctorID] = doctor;
                        }
                    }

                    if (!patientsById.TryGetValue(apt.PatientID, out var patient))
                    {
                        patient = _patientManager.GetPatientByID(apt.PatientID);
                        if (patient != null)
                        {
                            patientsById[patient.PatientID] = patient;
                        }
                    }

                    string doctorName = doctor != null ? $"Dr. {doctor.FirstName} {doctor.LastName}" : "Unknown";
                    string patientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "Unknown";

                    displayAppointments.Add(new AppointmentCardItem
                    {
                        AppointmentID = apt.AppointmentID,
                        AppointmentDateTime = apt.AppointmentDateTime,
                        DoctorName = doctorName,
                        PatientName = patientName,
                        ReasonForVisit = apt.ReasonForVisit,
                        Status = apt.Status
                    });
                }

                TodayAppointmentsItems.ItemsSource = displayAppointments;

                // Appointments by status (today)
                var statusGroups = todayAppointments
                    .GroupBy(a => string.IsNullOrWhiteSpace(a.Status) ? "Unknown" : a.Status.Trim())
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                int statusTotal = statusGroups.Sum(x => x.Count);
                AppointmentsByStatusItems.ItemsSource = statusGroups.Select(x => new DashboardBarItem
                {
                    Label = x.Label,
                    CountText = x.Count.ToString(),
                    Fraction = statusTotal == 0 ? 0 : (double)x.Count / statusTotal
                }).ToList();

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

            TodayAppointmentsItems.ItemsSource = null;
            PatientsByGenderItems.ItemsSource = null;
            DoctorsBySpecializationItems.ItemsSource = null;
            AppointmentsByStatusItems.ItemsSource = null;
            TodayAppointmentsMetric.Text = "-";
            CompletedTodayMetric.Text = "-";
            ActiveDoctorsMetric.Text = "-";
            TotalPatientsMetric.Text = "-";
            StatusTextBlock.Text = statusMessage;
        }
    }
}
