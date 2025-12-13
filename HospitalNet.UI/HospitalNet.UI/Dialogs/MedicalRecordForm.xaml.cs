using System;
using System.Windows;
using HospitalNet.Backend.BusinessLogic;
using HospitalNet.Backend.Models;

namespace HospitalNet.UI.Dialogs
{
    /// <summary>
    /// Records medical information after a patient visit.
    /// </summary>
    public partial class MedicalRecordForm : Window
    {
        private MedicalRecordManager _medicalRecordManager;
        private Appointment _appointment;
        private Patient _patient;

        public MedicalRecordForm(Appointment appointment, Patient patient)
        {
            InitializeComponent();
            _appointment = appointment;
            _patient = patient;

            if (patient != null)
            {
                PatientInfoTextBlock.Text = $"Patient: {patient.FirstName} {patient.LastName} | Age: {patient.Age} | ID: {patient.PatientID}";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ClinicalNotesTextBox.Text))
                {
                    StatusTextBlock.Text = "Clinical notes are required";
                    ClinicalNotesTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(DiagnosisTextBox.Text))
                {
                    StatusTextBlock.Text = "Diagnosis is required";
                    DiagnosisTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(PrescriptionTextBox.Text))
                {
                    StatusTextBlock.Text = "Prescription/treatment plan is required";
                    PrescriptionTextBox.Focus();
                    return;
                }

                _medicalRecordManager = new MedicalRecordManager(App.ConnectionString);

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

                _medicalRecordManager.AddMedicalRecord(medicalRecord);

                var appointmentManager = new AppointmentManager(App.ConnectionString);
                appointmentManager.UpdateAppointmentStatus(_appointment.AppointmentID, "Completed");

                StatusTextBlock.Text = "Medical record saved successfully";

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
                MessageBox.Show(
                    $"Failed to save medical record:\n{ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
