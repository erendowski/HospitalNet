using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using HospitalNet.Backend.BusinessLogic;
using HospitalNet.Backend.Models;
using HospitalNet.UI.Dialogs;
using HospitalNet.Backend.Infrastructure;

namespace HospitalNet.UI.Views
{
    /// <summary>
    /// Doctor dashboard - view appointments for the day and complete visits.
    /// </summary>
    public partial class DoctorsView : UserControl
    {
        private DoctorManager _doctorManager;
        private AppointmentManager _appointmentManager;
        private PatientManager _patientManager;
        private Doctor _currentDoctor;
        private DateTime _selectedDate;

        public DoctorsView()
        {
            InitializeComponent();
            InitializeManagers();
            ScheduleDatePicker.SelectedDate = DateTime.Today;
            LoadAppointmentsForDate(DateTime.Today);
        }

        private void InitializeManagers()
        {
            try
            {
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

                var doctors = _doctorManager.GetAllDoctors();
                if (doctors.Count > 0)
                {
                    _currentDoctor = doctors[0];
                    StatusTextBlock.Text = $"Logged in as: Dr. {_currentDoctor.FirstName} {_currentDoctor.LastName}";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error initializing: {ex.Message}";
                _doctorManager = null;
                _appointmentManager = null;
                _patientManager = null;
            }
        }

        private void ScheduleDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ScheduleDatePicker.SelectedDate.HasValue)
            {
                _selectedDate = ScheduleDatePicker.SelectedDate.Value;
                LoadAppointmentsForDate(_selectedDate);
            }
        }

        private void LoadAppointmentsForDate(DateTime selectedDate)
        {
            try
            {
                if (_doctorManager == null || _appointmentManager == null || _patientManager == null)
                {
                    AppointmentsGrid.ItemsSource = null;
                    StatusTextBlock.Text = "Doctors offline (no database connection).";
                    return;
                }

                if (_currentDoctor == null)
                {
                    StatusTextBlock.Text = "No doctor selected";
                    return;
                }

                var appointments = _appointmentManager.GetAppointmentsByDoctorAndDate(
                    _currentDoctor.DoctorID,
                    selectedDate);

                var displayAppointments = new ObservableCollection<dynamic>();

                foreach (var apt in appointments)
                {
                    if (apt.Status == "Completed")
                        continue;

                    var patient = _patientManager.GetPatientByID(apt.PatientID);
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
                StatusTextBlock.Text = $"Loaded {displayAppointments.Count} appointments for {selectedDate:MM/dd/yyyy}";
            }
            catch (Exception ex)
            {
                AppointmentsGrid.ItemsSource = null;
                StatusTextBlock.Text = $"Error loading appointments: {ex.Message}";
            }
        }

        private void CompleteVisit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button btn || btn.Tag is not int appointmentId)
                {
                    return;
                }

                var appointment = _appointmentManager.GetAppointmentByID(appointmentId);
                if (appointment == null)
                {
                    MessageBox.Show(
                        "Appointment not found.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var patient = _patientManager.GetPatientByID(appointment.PatientID);

                var medicalRecordForm = new MedicalRecordForm(appointment, patient);
                bool? result = medicalRecordForm.ShowDialog();

                if (result == true)
                {
                    LoadAppointmentsForDate(_selectedDate);
                    StatusTextBlock.Text = "Patient visit completed and recorded";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
                MessageBox.Show(
                    $"Failed to complete visit:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
