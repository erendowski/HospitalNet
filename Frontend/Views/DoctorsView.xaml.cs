using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using HospitalNet.Backend.Managers;
using HospitalNet.Backend.Models;
using HospitalNet.Frontend.Dialogs;

namespace HospitalNet.Frontend.Views
{
    /// <summary>
    /// Interaction logic for DoctorsView.xaml
    /// Doctor dashboard - view appointments for the day and complete visits
    /// </summary>
    public partial class DoctorsView : UserControl
    {
        private DoctorManager _doctorManager;
        private AppointmentManager _appointmentManager;
        private Doctor _currentDoctor;
        private DateTime _selectedDate;

        public DoctorsView()
        {
            InitializeComponent();
            InitializeManagers();
            ScheduleDatePicker.SelectedDate = DateTime.Today;
            LoadAppointmentsForDate(DateTime.Today);
        }

        /// <summary>
        /// Initialize database managers
        /// </summary>
        private void InitializeManagers()
        {
            try
            {
                _doctorManager = new DoctorManager(App.ConnectionString);
                _appointmentManager = new AppointmentManager(App.ConnectionString);
                
                // TODO: In production, get current logged-in doctor
                // For now, get first doctor from database
                var doctors = _doctorManager.GetAllDoctors();
                if (doctors.Count > 0)
                {
                    _currentDoctor = doctors[0];
                    StatusTextBlock.Text = $"Logged in as: Dr. {_currentDoctor.FirstName} {_currentDoctor.LastName}";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"✗ Error initializing: {ex.Message}";
                MessageBox.Show(
                    $"Failed to initialize doctor dashboard:\n{ex.Message}",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handle date picker selection change
        /// </summary>
        private void ScheduleDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ScheduleDatePicker.SelectedDate.HasValue)
            {
                _selectedDate = ScheduleDatePicker.SelectedDate.Value;
                LoadAppointmentsForDate(_selectedDate);
            }
        }

        /// <summary>
        /// Load appointments for the selected date
        /// </summary>
        private void LoadAppointmentsForDate(DateTime selectedDate)
        {
            try
            {
                if (_currentDoctor == null)
                {
                    StatusTextBlock.Text = "✗ No doctor selected";
                    return;
                }

                var appointments = _appointmentManager.GetAppointmentsByDoctorAndDate(
                    _currentDoctor.DoctorID,
                    selectedDate);

                var displayAppointments = new ObservableCollection<dynamic>();
                
                foreach (var apt in appointments)
                {
                    // Skip completed appointments
                    if (apt.Status == "Completed")
                        continue;

                    var patient = new PatientManager(App.ConnectionString).GetPatientByID(apt.PatientID);
                    var patientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "Unknown";

                    displayAppointments.Add(new
                    {
                        AppointmentID = apt.AppointmentID,
                        AppointmentDateTime = apt.AppointmentDateTime,
                        PatientName = patientName,
                        PatientID = apt.PatientID,
                        ReasonForVisit = apt.ReasonForVisit,
                        Status = apt.Status
                    });
                }

                AppointmentsGrid.ItemsSource = displayAppointments;
                
                DateInfoTextBlock.Text = selectedDate.ToString("dddd, MMMM d, yyyy");
                StatusTextBlock.Text = $"✓ Loaded {displayAppointments.Count} appointments for {selectedDate:MM/dd/yyyy}";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"✗ Error loading appointments: {ex.Message}";
                MessageBox.Show(
                    $"Failed to load appointments:\n{ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Complete Visit button click - opens MedicalRecordForm
        /// </summary>
        private void CompleteVisit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;
                int appointmentID = (int)btn.Tag;

                // Get appointment details
                var appointment = _appointmentManager.GetAppointmentByID(appointmentID);
                if (appointment == null)
                {
                    MessageBox.Show(
                        "Appointment not found.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Get patient details
                var patientManager = new PatientManager(App.ConnectionString);
                var patient = patientManager.GetPatientByID(appointment.PatientID);

                // Open medical record form dialog
                var medicalRecordForm = new MedicalRecordForm(appointment, patient);
                bool? result = medicalRecordForm.ShowDialog();

                if (result == true)
                {
                    // Refresh grid after medical record saved
                    LoadAppointmentsForDate(_selectedDate);
                    StatusTextBlock.Text = "✓ Patient visit completed and recorded";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"✗ Error: {ex.Message}";
                MessageBox.Show(
                    $"Failed to complete visit:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
