using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HospitalNet.Backend.BusinessLogic;
using HospitalNet.Backend.Models;
using HospitalNet.UI.Dialogs;
using HospitalNet.Backend.Infrastructure;

namespace HospitalNet.UI.Views
{
    /// <summary>
    /// Doctors management - add, edit, delete doctors and view their appointments.
    /// </summary>
    public partial class DoctorsView : UserControl
    {
        private DoctorManager _doctorManager;
        private AppointmentManager _appointmentManager;
        private PatientManager _patientManager;
        private List<Doctor> _allDoctors;
        private Doctor _selectedDoctor;
        private DateTime _selectedDate;

        public DoctorsView()
        {
            InitializeComponent();

            ApplyAuthorization();
            if (App.OfflineMode)
            {
                DoctorsDataGrid.ItemsSource = null;
                AppointmentsDataGrid.ItemsSource = null;
                StatusTextBlock.Text = "Doctors offline (no database connection).";
                return;
            }

            InitializeManagers();
            AppointmentDatePicker.SelectedDate = DateTime.Today;
            _selectedDate = DateTime.Today;
            LoadDoctors();
        }

        private void InitializeManagers()
        {
            try
            {
                if (App.OfflineMode)
                {
                    _doctorManager = null;
                    _appointmentManager = null;
                    _patientManager = null;
                    StatusTextBlock.Text = "Doctors offline (no database connection).";
                    return;
                }

                var dbHelper = new DatabaseHelper(App.ConnectionString);
                if (!dbHelper.TestConnection())
                {
                    _doctorManager = null;
                    _appointmentManager = null;
                    _patientManager = null;
                    StatusTextBlock.Text = "Doctors offline (no database connection).";
                    return;
                }

                _doctorManager = new DoctorManager(App.ConnectionString);
                _appointmentManager = new AppointmentManager(App.ConnectionString);
                _patientManager = new PatientManager(App.ConnectionString);
                StatusTextBlock.Text = "Ready";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error initializing: {ex.Message}";
                _doctorManager = null;
                _appointmentManager = null;
                _patientManager = null;
            }
        }

        private void LoadDoctors()
        {
            try
            {
                if (_doctorManager == null)
                {
                    DoctorsDataGrid.ItemsSource = null;
                    StatusTextBlock.Text = "Doctors offline (no database connection).";
                    return;
                }

                _allDoctors = _doctorManager.GetAllDoctors();
                DoctorsDataGrid.ItemsSource = new ObservableCollection<Doctor>(_allDoctors);
                StatusTextBlock.Text = $"Loaded {_allDoctors.Count} doctors";
            }
            catch (Exception ex)
            {
                DoctorsDataGrid.ItemsSource = null;
                StatusTextBlock.Text = $"Error loading doctors: {ex.Message}";
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_allDoctors == null) return;

            var searchTerm = SearchTextBox.Text?.ToLower() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                DoctorsDataGrid.ItemsSource = new ObservableCollection<Doctor>(_allDoctors);
            }
            else
            {
                var filtered = _allDoctors.Where(d =>
                    d.FullName.ToLower().Contains(searchTerm) ||
                    d.Specialization.ToLower().Contains(searchTerm) ||
                    d.LicenseNumber.ToLower().Contains(searchTerm) ||
                    d.Email.ToLower().Contains(searchTerm)).ToList();
                DoctorsDataGrid.ItemsSource = new ObservableCollection<Doctor>(filtered);
            }
        }

        private void AddDoctorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (App.CurrentUser != null && !App.CurrentUser.IsAdmin)
                {
                    MessageBox.Show(
                        "You do not have permission to add doctors.",
                        "Permission Denied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var dialog = new AddDoctorDialog();
                dialog.Owner = Window.GetWindow(this);
                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    LoadDoctors();
                    StatusTextBlock.Text = "Doctor added successfully";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
            }
        }

        private void ApplyAuthorization()
        {
            bool canAddDoctor = App.CurrentUser == null || App.CurrentUser.IsAdmin;

            if (AddDoctorButton != null)
            {
                AddDoctorButton.IsEnabled = canAddDoctor;
                AddDoctorButton.ToolTip = canAddDoctor
                    ? "Add Doctor"
                    : "Admin permission required";
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDoctors();
            if (_selectedDoctor != null)
            {
                LoadAppointmentsForDoctor(_selectedDoctor.DoctorID);
            }
        }

        private void DoctorsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DoctorsDataGrid.SelectedItem is Doctor doctor)
            {
                _selectedDoctor = doctor;
                UpdateDoctorDetails(doctor);
                LoadAppointmentsForDoctor(doctor.DoctorID);
            }
        }

        private void UpdateDoctorDetails(Doctor doctor)
        {
            DoctorNameTextBlock.Text = $"Dr. {doctor.FullName}";
            DoctorSpecTextBlock.Text = doctor.Specialization;
            DoctorEmailTextBlock.Text = doctor.Email;
            DoctorPhoneTextBlock.Text = doctor.PhoneNumber;
            DoctorOfficeTextBlock.Text = doctor.OfficeLocation;
            DoctorSalaryTextBlock.Text = $"₺{doctor.Salary:N2}";
            DoctorExperienceTextBlock.Text = $"{doctor.YearsOfExperience} years";
            DoctorMaxPatientsTextBlock.Text = doctor.MaxPatientCapacityPerDay.ToString();
        }

        private void ClearDoctorDetails()
        {
            DoctorNameTextBlock.Text = "Select a doctor";
            DoctorSpecTextBlock.Text = "";
            DoctorEmailTextBlock.Text = "-";
            DoctorPhoneTextBlock.Text = "-";
            DoctorOfficeTextBlock.Text = "-";
            DoctorSalaryTextBlock.Text = "-";
            DoctorExperienceTextBlock.Text = "-";
            DoctorMaxPatientsTextBlock.Text = "-";
            AppointmentsDataGrid.ItemsSource = null;
        }

        private void LoadAppointmentsForDoctor(int doctorId)
        {
            try
            {
                if (_appointmentManager == null || _patientManager == null)
                {
                    AppointmentsDataGrid.ItemsSource = null;
                    return;
                }

                var appointments = _appointmentManager.GetAppointmentsByDoctorAndDate(doctorId, _selectedDate);
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
                StatusTextBlock.Text = $"Loaded {displayAppointments.Count} appointments for {_selectedDate:MM/dd/yyyy}";
            }
            catch (Exception ex)
            {
                AppointmentsDataGrid.ItemsSource = null;
                StatusTextBlock.Text = $"Error loading appointments: {ex.Message}";
            }
        }

        private void EditDoctor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button btn || btn.Tag is not int doctorId)
                    return;

                var doctor = _allDoctors?.FirstOrDefault(d => d.DoctorID == doctorId);
                if (doctor == null)
                {
                    MessageBox.Show("Doctor not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dialog = new AddDoctorDialog(doctor);
                dialog.Owner = Window.GetWindow(this);
                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    LoadDoctors();
                    StatusTextBlock.Text = "Doctor updated successfully";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
            }
        }

        private void DeleteDoctor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button btn || btn.Tag is not int doctorId)
                    return;

                var doctor = _allDoctors?.FirstOrDefault(d => d.DoctorID == doctorId);
                if (doctor == null)
                {
                    MessageBox.Show("Doctor not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Are you sure you want to deactivate Dr. {doctor.FullName}?\n\nThis will set the doctor as inactive but keep their records.",
                    "Confirm Deactivation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    doctor.IsActive = false;
                    _doctorManager.UpdateDoctor(doctor);
                    LoadDoctors();
                    ClearDoctorDetails();
                    StatusTextBlock.Text = $"Doctor {doctor.FullName} deactivated";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Failed to deactivate doctor:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDoctor == null)
            {
                MessageBox.Show("Please select a doctor first.", "No Doctor Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Load patients into combo box
            LoadPatientsForComboBox();

            // Show the appointment form with today's date
            NewAppointmentPanel.Visibility = Visibility.Visible;
            AppointmentDatePicker.SelectedDate = DateTime.Today;
            AppointmentTimeTextBox.Text = "09:00";
            DurationTextBox.Text = "30";
            ReasonTextBox.Text = "";
            PatientComboBox.SelectedIndex = -1;
        }

        private void LoadPatientsForComboBox()
        {
            try
            {
                if (_patientManager == null)
                {
                    PatientComboBox.ItemsSource = null;
                    return;
                }

                var patients = _patientManager.GetAllActivePatients();
                PatientComboBox.ItemsSource = new ObservableCollection<Patient>(patients);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error loading patients: {ex.Message}";
            }
        }

        private void CancelAppointmentForm_Click(object sender, RoutedEventArgs e)
        {
            NewAppointmentPanel.Visibility = Visibility.Collapsed;
        }

        private void BookAppointment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (_selectedDoctor == null)
                {
                    MessageBox.Show("Please select a doctor.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PatientComboBox.SelectedItem is not Patient selectedPatient)
                {
                    MessageBox.Show("Please select a patient.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(AppointmentTimeTextBox.Text))
                {
                    MessageBox.Show("Please enter an appointment time (HH:mm).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!TimeSpan.TryParse(AppointmentTimeTextBox.Text, out TimeSpan appointmentTime))
                {
                    MessageBox.Show("Invalid time format. Please use HH:mm (e.g., 09:00, 14:30).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(DurationTextBox.Text, out int durationMinutes) || durationMinutes <= 0 || durationMinutes > 480)
                {
                    MessageBox.Show("Duration must be between 1 and 480 minutes.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(ReasonTextBox.Text))
                {
                    MessageBox.Show("Please enter a reason for the visit.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!AppointmentDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Please select a date for the appointment.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Create appointment date/time
                DateTime selectedDate = AppointmentDatePicker.SelectedDate.Value;
                DateTime appointmentDateTime = selectedDate.Date.Add(appointmentTime);

                // Check if appointment is in the past
                if (appointmentDateTime < DateTime.Now)
                {
                    MessageBox.Show("Cannot schedule appointments in the past.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check doctor availability (conflict detection)
                bool isAvailable = _appointmentManager.CheckDoctorAvailability(
                    _selectedDoctor.DoctorID,
                    appointmentDateTime,
                    durationMinutes);

                if (!isAvailable)
                {
                    MessageBox.Show(
                        $"Dr. {_selectedDoctor.FullName} already has an appointment at this time.\n\nPlease select a different time slot.",
                        "Time Conflict",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Create appointment
                var appointment = new Appointment
                {
                    PatientID = selectedPatient.PatientID,
                    DoctorID = _selectedDoctor.DoctorID,
                    AppointmentDateTime = appointmentDateTime,
                    DurationMinutes = durationMinutes,
                    ReasonForVisit = ReasonTextBox.Text.Trim(),
                    Status = "Scheduled"
                };

                _appointmentManager.ScheduleAppointment(appointment);

                // Update selected date to the newly created appointment's date and refresh list
                _selectedDate = selectedDate;
                NewAppointmentPanel.Visibility = Visibility.Collapsed;
                LoadAppointmentsForDoctor(_selectedDoctor.DoctorID);

                StatusTextBlock.Text = $"Appointment scheduled for {selectedPatient.FullName} at {appointmentDateTime:HH:mm}";
                MessageBox.Show(
                    $"Appointment successfully scheduled!\n\nPatient: {selectedPatient.FullName}\nDoctor: Dr. {_selectedDoctor.FullName}\nDate: {appointmentDateTime:dd/MM/yyyy HH:mm}\nDuration: {durationMinutes} minutes",
                    "Appointment Booked",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Failed to schedule appointment:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
