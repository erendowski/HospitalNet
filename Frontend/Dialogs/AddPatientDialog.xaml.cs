using System;
using System.Windows;
using HospitalNet.Backend.Managers;
using HospitalNet.Backend.Models;

namespace HospitalNet.Frontend.Dialogs
{
    /// <summary>
    /// Interaction logic for AddPatientDialog.xaml
    /// Used to add new patients or edit existing patient records
    /// </summary>
    public partial class AddPatientDialog : Window
    {
        private PatientManager _patientManager;
        private Patient _editingPatient;
        private bool _isEditMode;

        /// <summary>
        /// Constructor for adding new patient
        /// </summary>
        public AddPatientDialog()
        {
            InitializeComponent();
            _isEditMode = false;
            TitleTextBlock.Text = "Add New Patient";
            DateOfBirthPicker.SelectedDate = DateTime.Today;
        }

        /// <summary>
        /// Constructor for editing existing patient
        /// </summary>
        public AddPatientDialog(Patient patientToEdit)
        {
            InitializeComponent();
            _isEditMode = true;
            _editingPatient = patientToEdit;
            TitleTextBlock.Text = "Edit Patient";
            
            // Populate fields with existing patient data
            PopulateFields();
        }

        /// <summary>
        /// Populate form fields with existing patient data
        /// </summary>
        private void PopulateFields()
        {
            if (_editingPatient != null)
            {
                FirstNameTextBox.Text = _editingPatient.FirstName;
                LastNameTextBox.Text = _editingPatient.LastName;
                PhoneTextBox.Text = _editingPatient.Phone;
                EmailTextBox.Text = _editingPatient.Email;
                DateOfBirthPicker.SelectedDate = _editingPatient.DateOfBirth;
                MedicalHistoryTextBox.Text = _editingPatient.MedicalHistory ?? "";
                AllergiesTextBox.Text = _editingPatient.Allergies ?? "";
            }
        }

        /// <summary>
        /// Save button click - validates and saves patient
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
                {
                    StatusTextBlock.Text = "⚠ First name is required";
                    FirstNameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
                {
                    StatusTextBlock.Text = "⚠ Last name is required";
                    LastNameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
                {
                    StatusTextBlock.Text = "⚠ Phone is required";
                    PhoneTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                {
                    StatusTextBlock.Text = "⚠ Email is required";
                    EmailTextBox.Focus();
                    return;
                }

                if (!DateOfBirthPicker.SelectedDate.HasValue)
                {
                    StatusTextBlock.Text = "⚠ Date of birth is required";
                    return;
                }

                // Validate date of birth is in the past
                if (DateOfBirthPicker.SelectedDate.Value > DateTime.Today)
                {
                    StatusTextBlock.Text = "⚠ Date of birth cannot be in the future";
                    return;
                }

                _patientManager = new PatientManager(App.ConnectionString);

                if (_isEditMode)
                {
                    // Update existing patient
                    _editingPatient.FirstName = FirstNameTextBox.Text;
                    _editingPatient.LastName = LastNameTextBox.Text;
                    _editingPatient.Phone = PhoneTextBox.Text;
                    _editingPatient.Email = EmailTextBox.Text;
                    _editingPatient.DateOfBirth = DateOfBirthPicker.SelectedDate.Value;
                    _editingPatient.MedicalHistory = MedicalHistoryTextBox.Text;
                    _editingPatient.Allergies = AllergiesTextBox.Text;

                    _patientManager.UpdatePatient(_editingPatient);
                    StatusTextBlock.Text = "✓ Patient updated successfully";
                }
                else
                {
                    // Add new patient
                    var newPatient = new Patient
                    {
                        FirstName = FirstNameTextBox.Text,
                        LastName = LastNameTextBox.Text,
                        Phone = PhoneTextBox.Text,
                        Email = EmailTextBox.Text,
                        DateOfBirth = DateOfBirthPicker.SelectedDate.Value,
                        MedicalHistory = MedicalHistoryTextBox.Text,
                        Allergies = AllergiesTextBox.Text,
                        RegistrationDate = DateTime.Now,
                        IsActive = true
                    };

                    _patientManager.AddPatient(newPatient);
                    StatusTextBlock.Text = "✓ Patient added successfully";
                }

                // Close dialog with success result
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"✗ Error: {ex.Message}";
                MessageBox.Show(
                    $"Failed to save patient:\n{ex.Message}",
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
