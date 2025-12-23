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
        private AppointmentManager _appointmentManager;
        private PatientManager _patientManager;
        private List<Doctor> _allDoctors;
        private readonly Dictionary<int, Patient> _patientsById = new Dictionary<int, Patient>();
        private Doctor _selectedDoctor;
        private DateTime _selectedDate;
        private AppointmentDisplayRow _selectedAppointmentRow;

        public DoctorsView()
        {
            InitializeComponent();

            ApplyAuthorization();
            if (App.OfflineMode)
            {
                DoctorsDataGrid.ItemsSource = null;
                AppointmentsDataGrid.ItemsSource = null;
                StatusTextBlock.Text = "Doctors offline (no database connection).";
                AddDoctorButton.IsEnabled = false;
                RefreshButton.IsEnabled = false;
                AddDoctorPanel.Visibility = Visibility.Collapsed;
                SelectedAppointmentSummaryText.Text = "-";
                return;
            }

            InitializeManagers();
            _selectedDate = DateTime.Today;
            AppointmentsDateFilterPicker.SelectedDate = _selectedDate;
            LoadDoctors();
            SelectedAppointmentSummaryText.Text = "-";
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
       (($"{d.FirstName} {d.LastName}").ToLower().Contains(searchTerm)) ||
       ((d.Specialization ?? "").ToLower().Contains(searchTerm)) ||
       ((d.LicenseNumber ?? "").ToLower().Contains(searchTerm)) ||
       ((d.Email ?? "").ToLower().Contains(searchTerm))
   ).ToList();

                DoctorsDataGrid.ItemsSource = new ObservableCollection<Doctor>(filtered);
                StatusTextBlock.Text = $"Showing {filtered.Count} doctors";
            }
        }

        private void AddDoctorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AddDoctorPanel.Visibility != Visibility.Visible)
                {
                    AddDoctorPanel.Visibility = Visibility.Visible;
                    DetailsScrollViewer?.ScrollToTop();

                    NewDoctorFirstNameTextBox.Text = string.Empty;
                    NewDoctorLastNameTextBox.Text = string.Empty;
                    NewDoctorSpecializationTextBox.Text = "General Practice";
                    NewDoctorLicenseTextBox.Text = string.Empty;
                    NewDoctorPhoneTextBox.Text = string.Empty;
                    NewDoctorEmailTextBox.Text = string.Empty;
                    NewDoctorOfficeTextBox.Text = string.Empty;
                    NewDoctorYearsTextBox.Text = "0";
                    NewDoctorMaxPatientsTextBox.Text = "20";
                    NewDoctorSalaryTextBox.Text = "0";
                    NewDoctorIsActiveCheckBox.IsChecked = true;

                    StatusTextBlock.Text = "Enter doctor details and click Save.";
                    NewDoctorFirstNameTextBox.Focus();
                }
                else
                {
                    AddDoctorPanel.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
            }
        }

        private void ApplyAuthorization()
        {
            bool isAdmin = App.CurrentUser != null && App.CurrentUser.IsAdmin;

            if (AddDoctorButton != null)
            {
                AddDoctorButton.IsEnabled = isAdmin;
                AddDoctorButton.ToolTip = isAdmin ? "Add Doctor" : "Admin required";
            }

            if (EditSelectedDoctorButton != null)
                EditSelectedDoctorButton.IsEnabled = false;
            if (DeactivateSelectedDoctorButton != null)
                DeactivateSelectedDoctorButton.IsEnabled = false;
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
                SetSelectedAppointmentRow(null);
                UpdateDoctorActionButtons();
                return;
            }

            _selectedDoctor = null;
            ClearDoctorDetails();
            UpdateDoctorActionButtons();
        }

        private void UpdateDoctorActionButtons()
        {
            bool isAdmin = App.CurrentUser != null && App.CurrentUser.IsAdmin;
            bool hasDoctor = _selectedDoctor != null;
            bool hasDb = _doctorManager != null && !App.OfflineMode;

            if (EditSelectedDoctorButton != null)
                EditSelectedDoctorButton.IsEnabled = isAdmin && hasDoctor && hasDb;
            if (DeactivateSelectedDoctorButton != null)
                DeactivateSelectedDoctorButton.IsEnabled = isAdmin && hasDoctor && hasDb;
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
            SetSelectedAppointmentRow(null);
        }

        private void SetSelectedAppointmentRow(AppointmentDisplayRow row)
        {
            _selectedAppointmentRow = row;
            if (row == null)
            {
                SelectedAppointmentSummaryText.Text = "-";
                return;
            }

            SelectedAppointmentSummaryText.Text =
                $"#{row.AppointmentID} • {row.AppointmentTime:g} • {row.Status}\n{row.PatientName}\n{row.ReasonForVisit}";
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
                var displayAppointments = new ObservableCollection<AppointmentDisplayRow>();

                foreach (var apt in appointments)
                {
                    if (!_patientsById.TryGetValue(apt.PatientID, out var patient))
                    {
                        patient = _patientManager.GetPatientByID(apt.PatientID);
                        if (patient != null)
                        {
                            _patientsById[apt.PatientID] = patient;
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
                StatusTextBlock.Text = $"Loaded {displayAppointments.Count} appointments for {_selectedDate:MM/dd/yyyy}";
            }
            catch (Exception ex)
            {
                AppointmentsDataGrid.ItemsSource = null;
                StatusTextBlock.Text = $"Error loading appointments: {ex.Message}";
            }
        }

        private void AppointmentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppointmentsDataGrid.SelectedItem is AppointmentDisplayRow row)
            {
                SetSelectedAppointmentRow(row);
                return;
            }

            SetSelectedAppointmentRow(null);
        }

        private void CancelAddDoctor_Click(object sender, RoutedEventArgs e)
        {
            AddDoctorPanel.Visibility = Visibility.Collapsed;
        }

        private void SaveAddDoctor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_doctorManager == null)
                {
                    StatusTextBlock.Text = "Doctors offline (no database connection).";
                    return;
                }

                var firstName = (NewDoctorFirstNameTextBox.Text ?? string.Empty).Trim();
                var lastName = (NewDoctorLastNameTextBox.Text ?? string.Empty).Trim();
                var specialization = (NewDoctorSpecializationTextBox.Text ?? string.Empty).Trim();
                var license = (NewDoctorLicenseTextBox.Text ?? string.Empty).Trim();
                var phone = (NewDoctorPhoneTextBox.Text ?? string.Empty).Trim();
                var email = (NewDoctorEmailTextBox.Text ?? string.Empty).Trim();
                var office = (NewDoctorOfficeTextBox.Text ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(firstName) ||
                    string.IsNullOrWhiteSpace(lastName) ||
                    string.IsNullOrWhiteSpace(specialization) ||
                    string.IsNullOrWhiteSpace(license) ||
                    string.IsNullOrWhiteSpace(phone) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(office))
                {
                    StatusTextBlock.Text = "Please fill all required fields.";
                    return;
                }

                if (!int.TryParse(NewDoctorYearsTextBox.Text, out int years) || years < 0)
                {
                    StatusTextBlock.Text = "Years of experience must be a non-negative number.";
                    return;
                }

                if (!int.TryParse(NewDoctorMaxPatientsTextBox.Text, out int maxPatients) || maxPatients <= 0)
                {
                    StatusTextBlock.Text = "Max patients/day must be greater than 0.";
                    return;
                }

                if (!decimal.TryParse(NewDoctorSalaryTextBox.Text, out decimal salary) || salary < 0)
                {
                    StatusTextBlock.Text = "Salary must be a non-negative number.";
                    return;
                }

                var newDoctor = new Doctor
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Specialization = specialization,
                    LicenseNumber = license,
                    PhoneNumber = phone,
                    Email = email,
                    OfficeLocation = office,
                    YearsOfExperience = years,
                    MaxPatientCapacityPerDay = maxPatients,
                    Salary = salary,
                    IsActive = NewDoctorIsActiveCheckBox.IsChecked ?? true
                };

                _doctorManager.RegisterDoctor(newDoctor);
                LoadDoctors();
                AddDoctorPanel.Visibility = Visibility.Collapsed;
                StatusTextBlock.Text = $"Doctor added: Dr. {newDoctor.FullName}";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Failed to add doctor: {ex.Message}";
                MessageBox.Show(
                    $"Failed to add doctor:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void EditSelectedDoctorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (App.CurrentUser != null && !App.CurrentUser.IsAdmin)
                {
                    MessageBox.Show(
                        "You do not have permission to edit doctors.",
                        "Permission Denied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (_selectedDoctor == null)
                {
                    MessageBox.Show("Please select a doctor.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int selectedId = _selectedDoctor.DoctorID;
                var dialog = new AddDoctorDialog(_selectedDoctor);
                dialog.Owner = Window.GetWindow(this);
                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    LoadDoctors();
                    DoctorsDataGrid.SelectedItem = _allDoctors?.FirstOrDefault(d => d.DoctorID == selectedId);
                    StatusTextBlock.Text = "Doctor updated successfully";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Failed to update doctor:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeactivateSelectedDoctorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (App.CurrentUser != null && !App.CurrentUser.IsAdmin)
                {
                    MessageBox.Show(
                        "You do not have permission to deactivate doctors.",
                        "Permission Denied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (_doctorManager == null)
                {
                    MessageBox.Show("Doctors are unavailable (offline or no database connection).", "Offline", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_selectedDoctor == null)
                {
                    MessageBox.Show("Please select a doctor.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Are you sure you want to deactivate Dr. {_selectedDoctor.FullName}?\n\nThis will set the doctor as inactive but keep their records.",
                    "Confirm Deactivation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                _selectedDoctor.IsActive = false;
                _doctorManager.UpdateDoctor(_selectedDoctor);
                LoadDoctors();
                ClearDoctorDetails();
                _selectedDoctor = null;
                UpdateDoctorActionButtons();
                StatusTextBlock.Text = "Doctor deactivated";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Failed to deactivate doctor:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditDoctor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (App.CurrentUser != null && !App.CurrentUser.IsAdmin)
                {
                    MessageBox.Show(
                        "You do not have permission to edit doctors.",
                        "Permission Denied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

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
                if (App.CurrentUser != null && !App.CurrentUser.IsAdmin)
                {
                    MessageBox.Show(
                        "You do not have permission to deactivate doctors.",
                        "Permission Denied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

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

#if false
        // Appointment creation is intentionally restricted to the Appointments tab.
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
            SetSelectedPatientForAppointment(null);
        }

        private void LoadPatientsForComboBox()
        {
            try
            {
                if (_patientManager == null)
                {
                    PatientComboBox.ItemsSource = null;
                    SetSelectedPatientForAppointment(null);
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
                    SetSelectedPatientForAppointment(PatientComboBox.SelectedItem as Patient);
                }
                else
                {
                    SetSelectedPatientForAppointment(null);
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error loading patients: {ex.Message}";
                SetSelectedPatientForAppointment(null);
            }
        }

        private void SetSelectedPatientForAppointment(Patient patient)
        {
            if (patient == null)
            {
                SelectedPatientForAppointmentText.Text = "-";
                SelectedPatientForAppointmentMetaText.Text = "-";
                return;
            }

            SelectedPatientForAppointmentText.Text = $"{patient.FullName} (ID: {patient.PatientID})";
            SelectedPatientForAppointmentMetaText.Text = $"{patient.PhoneNumber ?? "-"} • {patient.Email ?? "-"}";
        }

        private void PatientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatientComboBox.SelectedItem is Patient patient)
            {
                SetSelectedPatientForAppointment(patient);
                return;
            }

            SetSelectedPatientForAppointment(null);
        }

#endif

        private void AppointmentsDateFilterPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppointmentsDateFilterPicker.SelectedDate.HasValue)
            {
                _selectedDate = AppointmentsDateFilterPicker.SelectedDate.Value.Date;
                if (_selectedDoctor != null)
                {
                    LoadAppointmentsForDoctor(_selectedDoctor.DoctorID);
                }
            }
        }

#if false
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
#endif
    }
}
