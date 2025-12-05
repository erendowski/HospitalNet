using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using HospitalNet.Backend.Managers;
using HospitalNet.Backend.Models;

namespace HospitalNet.Frontend.Views
{
    /// <summary>
    /// Interaction logic for AppointmentsView.xaml
    /// Handles appointment scheduling with double-booking prevention
    /// CRITICAL: Catches double-booking exceptions and displays user-friendly error messages
    /// </summary>
    public partial class AppointmentsView : UserControl
    {
        private DoctorManager _doctorManager;
        private PatientManager _patientManager;
        private AppointmentManager _appointmentManager;
        private Doctor _selectedDoctor;
        private DateTime _selectedDate;

        public AppointmentsView()
        {
            InitializeComponent();
            InitializeManagers();
            LoadDoctors();
            LoadPatients();
            DatePicker.SelectedDate = DateTime.Today;
        }

        /// <summary>
        /// Initialize all database managers
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
                MessageBox.Show(
                    $"Failed to initialize managers:\n{ex.Message}",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Load all doctors into ComboBox
        /// </summary>
        private void LoadDoctors()
        {
            try
            {
                var doctors = _doctorManager.GetAllDoctors();
                DoctorComboBox.ItemsSource = new ObservableCollection<Doctor>(doctors);
                if (doctors.Count > 0)
                {
                    DoctorComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load doctors:\n{ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Load all patients into ComboBox
        /// </summary>
        private void LoadPatients()
        {
            try
            {
                var patients = _patientManager.GetAllActivePatients();
                PatientComboBox.ItemsSource = new ObservableCollection<Patient>(patients);
                if (patients.Count > 0)
                {
                    PatientComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load patients:\n{ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handle doctor selection change - refresh appointments list
        /// </summary>
        private void DoctorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DoctorComboBox.SelectedItem is Doctor doctor)
            {
                _selectedDoctor = doctor;
                RefreshAppointmentsList();
            }
        }

        /// <summary>
        /// Handle date selection change - refresh appointments list
        /// </summary>
        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePicker.SelectedDate.HasValue)
            {
                _selectedDate = DatePicker.SelectedDate.Value;
                RefreshAppointmentsList();
            }
        }

        /// <summary>
        /// Refresh appointments list for selected doctor and date
        /// </summary>
        private void RefreshAppointmentsList()
        {
            try
            {
                if (_selectedDoctor == null || !DatePicker.SelectedDate.HasValue)
                    return;

                var appointments = _appointmentManager.GetAppointmentsByDoctorAndDate(
                    _selectedDoctor.DoctorID,
                    _selectedDate);

                // Create display objects with doctor name
                var displayAppointments = new ObservableCollection<dynamic>();
                foreach (var apt in appointments)
                {
                    var patient = _patientManager.GetPatientByID(apt.PatientID);
                    var patientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "Unknown";
                    
                    displayAppointments.Add(new
                    {
                        AppointmentID = apt.AppointmentID,
                        AppointmentTime = apt.AppointmentDateTime,
                        PatientName = patientName,
                        ReasonForVisit = apt.ReasonForVisit,
                        Status = apt.Status
                    });
                }

                AppointmentsDataGrid.ItemsSource = displayAppointments;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to refresh appointments:\n{ex.Message}",
                    "Refresh Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// CRITICAL: Book appointment with double-booking prevention
        /// Must wrap AppointmentManager.ScheduleAppointment in try-catch
        /// If exception occurs (e.g., "Doctor is not available"), show error MessageBox
        /// </summary>
        private void BookButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation
                if (_selectedDoctor == null)
                {
                    MessageBox.Show(
                        "Please select a doctor.",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (PatientComboBox.SelectedItem == null)
                {
                    MessageBox.Show(
                        "Please select a patient.",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (!DatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show(
                        "Please select an appointment date.",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(TimeTextBox.Text))
                {
                    MessageBox.Show(
                        "Please enter an appointment time (format: HH:mm).",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Parse time
                if (!TimeSpan.TryParse(TimeTextBox.Text, out TimeSpan appointmentTime))
                {
                    MessageBox.Show(
                        "Invalid time format. Please use HH:mm format (e.g., 14:30).",
                        "Time Format Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Combine date and time
                DateTime appointmentDateTime = DatePicker.SelectedDate.Value.Add(appointmentTime);

                int patientID = (int)PatientComboBox.SelectedValue;
                string reason = ReasonTextBox.Text ?? "General Visit";

                // =========================================================================
                // CRITICAL LOGIC: Try-catch block for double-booking prevention
                // =========================================================================
                try
                {
                    // Call AppointmentManager.ScheduleAppointment - this will throw exception if doctor is not available
                    int appointmentID = _appointmentManager.ScheduleAppointment(
                        _selectedDoctor.DoctorID,
                        patientID,
                        appointmentDateTime,
                        reason);

                    MessageBox.Show(
                        $"✓ Appointment booked successfully!\nAppointment ID: {appointmentID}",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Clear form
                    TimeTextBox.Text = "09:00";
                    ReasonTextBox.Text = "";

                    // Refresh appointments list
                    RefreshAppointmentsList();
                }
                catch (Exception bookingException)
                {
                    // CRITICAL: This exception message comes from AppointmentManager
                    // If double-booking is detected, the message will be something like:
                    // "Doctor is not available at this time" or "Double booking not allowed"
                    MessageBox.Show(
                        $"✗ Cannot book appointment:\n{bookingException.Message}",
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
