using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using HospitalNet.Backend.BusinessLogic;
using HospitalNet.Backend.Models;
using HospitalNet.Backend.Infrastructure;

namespace HospitalNet.UI.Views
{
    /// <summary>
    /// Appointment scheduling with double-booking prevention.
    /// </summary>
    public partial class AppointmentsView : UserControl
    {
        private sealed class AppointmentDisplayRow
        {
            public int AppointmentID { get; set; }
            public int PatientID { get; set; }
            public int DoctorID { get; set; }
            public DateTime AppointmentTime { get; set; }
            public string PatientName { get; set; }
            public string ReasonForVisit { get; set; }
            public string Status { get; set; }
        }

        private DoctorManager _doctorManager;
        private PatientManager _patientManager;
        private AppointmentManager _appointmentManager;
        private Doctor _selectedDoctor;
        private Patient _selectedPatient;
        private AppointmentDisplayRow _selectedAppointmentRow;
        private DateTime _selectedDate;
        private readonly System.Collections.Generic.Dictionary<int, Patient> _patientsById = new System.Collections.Generic.Dictionary<int, Patient>();

        public AppointmentsView()
        {
            InitializeComponent();
            if (App.OfflineMode)
            {
                AppointmentsDataGrid.ItemsSource = null;
                BookButton.IsEnabled = false;
                SetSelectedDoctor(null);
                SetSelectedPatient(null);
                SetSelectedAppointmentRow(null);
                return;
            }

            InitializeManagers();
            LoadDoctors();
            LoadPatients();
            DatePicker.SelectedDate = DateTime.Today;
        }

        private void SetSelectedDoctor(Doctor doctor)
        {
            _selectedDoctor = doctor;
            SelectedDoctorText.Text = doctor != null ? $"Dr. {doctor.FullName} ({doctor.Specialization})" : "-";
        }

        private void SetSelectedPatient(Patient patient)
        {
            _selectedPatient = patient;
            SelectedPatientText.Text = patient != null ? $"{patient.FullName} (ID: {patient.PatientID})" : "-";
        }

        private void SetSelectedAppointmentRow(AppointmentDisplayRow row)
        {
            _selectedAppointmentRow = row;
            if (row == null)
            {
                SelectedAppointmentText.Text = "-";
                return;
            }

            SelectedAppointmentText.Text = $"#{row.AppointmentID} • {row.AppointmentTime:g} • {row.Status}\n{row.PatientName}\n{row.ReasonForVisit}";
        }

        private void InitializeManagers()
        {
            try
            {
                if (App.OfflineMode)
                {
                    _doctorManager = null;
                    _patientManager = null;
                    _appointmentManager = null;
                    return;
                }

                var dbHelper = new DatabaseHelper(App.ConnectionString);
                if (!dbHelper.TestConnection())
                {
                    _doctorManager = null;
                    _patientManager = null;
                    _appointmentManager = null;
                    BookButton.IsEnabled = false;
                    return;
                }

                _doctorManager = new DoctorManager(App.ConnectionString);
                _patientManager = new PatientManager(App.ConnectionString);
                _appointmentManager = new AppointmentManager(App.ConnectionString);
                BookButton.IsEnabled = true;
            }
            catch (Exception)
            {
                _doctorManager = null;
                _patientManager = null;
                _appointmentManager = null;
                BookButton.IsEnabled = false;
            }
        }

        private void LoadDoctors()
        {
            try
            {
                if (_doctorManager == null)
                {
                    DoctorComboBox.ItemsSource = null;
                    return;
                }

                var doctors = _doctorManager.GetAllDoctors();
                DoctorComboBox.ItemsSource = new ObservableCollection<Doctor>(doctors);
                if (doctors.Count > 0)
                {
                    DoctorComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception)
            {
                DoctorComboBox.ItemsSource = null;
            }
        }

        private void LoadPatients()
        {
            try
            {
                if (_patientManager == null)
                {
                    PatientComboBox.ItemsSource = null;
                    _patientsById.Clear();
                    return;
                }

                var patients = _patientManager.GetAllActivePatients();
                PatientComboBox.ItemsSource = new ObservableCollection<Patient>(patients);

                _patientsById.Clear();
                foreach (var patient in patients)
                {
                    _patientsById[patient.PatientID] = patient;
                }
                if (patients.Count > 0)
                {
                    PatientComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception)
            {
                PatientComboBox.ItemsSource = null;
                _patientsById.Clear();
            }
        }

        private void DoctorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DoctorComboBox.SelectedItem is Doctor doctor)
            {
                SetSelectedDoctor(doctor);
                RefreshAppointmentsList();
                SetSelectedAppointmentRow(null);
            }
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePicker.SelectedDate.HasValue)
            {
                _selectedDate = DatePicker.SelectedDate.Value;
                RefreshAppointmentsList();
                SetSelectedAppointmentRow(null);
            }
        }

        private void RefreshAppointmentsList()
        {
            try
            {
                if (_appointmentManager == null || _patientManager == null)
                {
                    AppointmentsDataGrid.ItemsSource = null;
                    return;
                }

                if (_selectedDoctor == null || !DatePicker.SelectedDate.HasValue)
                    return;

                var appointments = _appointmentManager.GetAppointmentsByDoctorAndDate(
                    _selectedDoctor.DoctorID,
                    _selectedDate);

                var displayAppointments = new ObservableCollection<AppointmentDisplayRow>();
                foreach (var apt in appointments)
                {
                    if (!_patientsById.TryGetValue(apt.PatientID, out var patient))
                    {
                        patient = _patientManager.GetPatientByID(apt.PatientID);
                        if (patient != null)
                        {
                            _patientsById[patient.PatientID] = patient;
                        }
                    }
                    var patientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "Unknown";

                    displayAppointments.Add(new AppointmentDisplayRow
                    {
                        AppointmentID = apt.AppointmentID,
                        DoctorID = apt.DoctorID,
                        PatientID = apt.PatientID,
                        AppointmentTime = apt.AppointmentDateTime,
                        PatientName = patientName,
                        ReasonForVisit = apt.ReasonForVisit,
                        Status = apt.Status
                    });
                }

                AppointmentsDataGrid.ItemsSource = displayAppointments;
            }
            catch (Exception)
            {
                AppointmentsDataGrid.ItemsSource = null;
            }
        }

        private void PatientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatientComboBox.SelectedItem is Patient patient)
            {
                SetSelectedPatient(patient);
                return;
            }

            SetSelectedPatient(null);
        }

        private void AppointmentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppointmentsDataGrid.SelectedItem is AppointmentDisplayRow row)
            {
                SetSelectedAppointmentRow(row);

                if (_patientsById.TryGetValue(row.PatientID, out var patient))
                {
                    SetSelectedPatient(patient);
                }
                return;
            }

            SetSelectedAppointmentRow(null);
        }

        private void BookButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_appointmentManager == null || _patientManager == null)
                {
                    MessageBox.Show(
                        "Appointments are unavailable (offline or no database connection).",
                        "Offline",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (_selectedDoctor == null)
                {
                    MessageBox.Show("Please select a doctor.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PatientComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select a patient.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!DatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Please select an appointment date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(TimeTextBox.Text))
                {
                    MessageBox.Show("Please enter an appointment time (format: HH:mm).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!TimeSpan.TryParse(TimeTextBox.Text, out TimeSpan appointmentTime))
                {
                    MessageBox.Show("Invalid time format. Please use HH:mm (e.g., 14:30).", "Time Format Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime appointmentDateTime = DatePicker.SelectedDate.Value.Add(appointmentTime);

                int patientId = (int)PatientComboBox.SelectedValue;
                string reason = string.IsNullOrWhiteSpace(ReasonTextBox.Text) ? "General Visit" : ReasonTextBox.Text;

                try
                {
                    var appointment = _appointmentManager.ScheduleAppointment(
                        new Appointment
                        {
                            DoctorID = _selectedDoctor.DoctorID,
                            PatientID = patientId,
                            AppointmentDateTime = appointmentDateTime,
                            DurationMinutes = 30, // Default 30 minutes
                            ReasonForVisit = reason,
                            Status = "Scheduled"
                        });

                    MessageBox.Show(
                        $"Appointment booked successfully!\nAppointment ID: {appointment.AppointmentID}",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    TimeTextBox.Text = "09:00";
                    ReasonTextBox.Text = string.Empty;

                    RefreshAppointmentsList();
                    SetSelectedAppointmentRow(null);
                }
                catch (Exception bookingException)
                {
                    MessageBox.Show(
                        $"Cannot book appointment:\n{bookingException.Message}",
                        "Booking Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unexpected error:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
