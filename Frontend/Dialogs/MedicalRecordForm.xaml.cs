using System;
using System.Windows;
using HospitalNet.Backend.Managers;
using HospitalNet.Backend.Models;

namespace HospitalNet.Frontend.Dialogs
{
    /// <summary>
    /// Interaction logic for MedicalRecordForm.xaml
    /// Used to record medical information after completing a patient visit
    /// </summary>
    public partial class MedicalRecordForm : Window
    {
        private MedicalRecordManager _medicalRecordManager;
        private Appointment _appointment;
        private Patient _patient;

        /// <summary>
        /// Constructor - takes appointment and patient as context
        /// </summary>
        public MedicalRecordForm(Appointment appointment, Patient patient)
        {
            InitializeComponent();
            _appointment = appointment;
            _patient = patient;

            // Display patient information
            if (patient != null)
            {
                PatientInfoTextBlock.Text = $"Patient: {patient.FirstName} {patient.LastName} | Age: {patient.Age} | ID: {patient.PatientID}";
            }
        }

        /// <summary>
        /// Save button click - validates and saves medical record
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(ClinicalNotesTextBox.Text))
                {
                    StatusTextBlock.Text = "⚠ Clinical notes are required";
                    ClinicalNotesTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(DiagnosisTextBox.Text))
                {
                    StatusTextBlock.Text = "⚠ Diagnosis is required";
                    DiagnosisTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(PrescriptionTextBox.Text))
                {
                    StatusTextBlock.Text = "⚠ Prescription/treatment plan is required";
                    PrescriptionTextBox.Focus();
                    return;
                }

                _medicalRecordManager = new MedicalRecordManager(App.ConnectionString);

                // Create medical record
                var medicalRecord = new MedicalRecord
                {
                    AppointmentID = _appointment.AppointmentID,
                    PatientID = _patient.PatientID,
                    DoctorID = _appointment.DoctorID,
                    VisitDate = DateTime.Now,
                    ClinicalNotes = ClinicalNotesTextBox.Text,
                    Diagnosis = DiagnosisTextBox.Text,
                    PrescriptionText = PrescriptionTextBox.Text,
                    VitalSigns = VitalSignsTextBox.Text,
                    FollowUpRequired = FollowUpRequiredCheckBox.IsChecked.GetValueOrDefault(false),
                    FollowUpNotes = FollowUpNotesTextBox.Text
                };

                // Save medical record
                _medicalRecordManager.AddMedicalRecord(medicalRecord);
                
                StatusTextBlock.Text = "✓ Medical record saved successfully";
                
                // Update appointment status to Completed
                var appointmentManager = new AppointmentManager(App.ConnectionString);
                appointmentManager.UpdateAppointmentStatus(_appointment.AppointmentID, "Completed");

                // Close dialog with success result
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"✗ Error: {ex.Message}";
                MessageBox.Show(
                    $"Failed to save medical record:\n{ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Cancel button click
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
