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

        private void InitializeManagers()
        {
            try
            {
                var dbHelper = new DatabaseHelper(App.ConnectionString);
                if (!dbHelper.TestConnection())
                {
                    _doctorManager = null;
                    _patientManager = null;
                    _appointmentManager = null;
                    return;
                }

                _doctorManager = new DoctorManager(App.ConnectionString);
                _patientManager = new PatientManager(App.ConnectionString);
                _appointmentManager = new AppointmentManager(App.ConnectionString);
            }
            catch (Exception ex)
            {
                _doctorManager = null;
                _patientManager = null;
                _appointmentManager = null;
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
            catch (Exception ex)
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
                    return;
                }

                var patients = _patientManager.GetAllActivePatients();
                PatientComboBox.ItemsSource = new ObservableCollection<Patient>(patients);
                if (patients.Count > 0)
                {
                    PatientComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                PatientComboBox.ItemsSource = null;
            }
        }

        private void DoctorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DoctorComboBox.SelectedItem is Doctor doctor)
            {
                _selectedDoctor = doctor;
                RefreshAppointmentsList();
            }
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePicker.SelectedDate.HasValue)
            {
                _selectedDate = DatePicker.SelectedDate.Value;
                RefreshAppointmentsList();
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
                AppointmentsDataGrid.ItemsSource = null;
            }
        }

        private void BookButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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
