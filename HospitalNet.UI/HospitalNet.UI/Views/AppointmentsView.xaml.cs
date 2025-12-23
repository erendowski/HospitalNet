using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using HospitalNet.Backend.BusinessLogic;
using HospitalNet.Backend.Infrastructure;
using HospitalNet.Backend.Models;

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
        private readonly Dictionary<int, Patient> _patientsById = new Dictionary<int, Patient>();

        public AppointmentsView()
        {
            InitializeComponent();

            if (App.OfflineMode)
            {
                AppointmentsListBox.ItemsSource = null;
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
                UpdateAppointmentActionButtons();
                return;
            }

            SelectedAppointmentText.Text =
                $"#{row.AppointmentID} | {row.AppointmentTime:g} | {row.Status}\n{row.PatientName}\n{row.ReasonForVisit}";
            UpdateAppointmentActionButtons();
        }

        private void UpdateAppointmentActionButtons()
        {
            bool canAct = _selectedAppointmentRow != null &&
                          !string.Equals(_selectedAppointmentRow.Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
                          !string.Equals(_selectedAppointmentRow.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
                          _appointmentManager != null;

            if (CompleteAppointmentButton != null)
                CompleteAppointmentButton.IsEnabled = canAct;
            if (CancelAppointmentButton != null)
                CancelAppointmentButton.IsEnabled = canAct;
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
            catch
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
            catch
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
            catch
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
                    AppointmentsListBox.ItemsSource = null;
                    UpdateAppointmentActionButtons();
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

                AppointmentsListBox.ItemsSource = displayAppointments;
                UpdateAppointmentActionButtons();
            }
            catch
            {
                AppointmentsListBox.ItemsSource = null;
                UpdateAppointmentActionButtons();
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

        private void AppointmentsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppointmentsListBox.SelectedItem is AppointmentDisplayRow row)
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

        private void CompleteAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_appointmentManager == null)
                {
                    MessageBox.Show("Appointments are unavailable (offline or no database connection).", "Offline", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_selectedAppointmentRow == null)
                {
                    MessageBox.Show("Please select an appointment.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Mark appointment #{_selectedAppointmentRow.AppointmentID} as completed?",
                    "Confirm Completion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                _appointmentManager.CompleteAppointment(_selectedAppointmentRow.AppointmentID);

                RefreshAppointmentsList();
                SetSelectedAppointmentRow(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to complete appointment:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_appointmentManager == null)
                {
                    MessageBox.Show("Appointments are unavailable (offline or no database connection).", "Offline", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_selectedAppointmentRow == null)
                {
                    MessageBox.Show("Please select an appointment.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Cancel appointment #{_selectedAppointmentRow.AppointmentID}?",
                    "Confirm Cancellation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                _appointmentManager.CancelAppointment(_selectedAppointmentRow.AppointmentID, "Cancelled");

                RefreshAppointmentsList();
                SetSelectedAppointmentRow(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to cancel appointment:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                if (!TimeSpan.TryParse(TimeTextBox.Text, out var appointmentTime))
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
                            DurationMinutes = 30,
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
