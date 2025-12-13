using System;
using System.Windows;
using HospitalNet.Backend.BusinessLogic;
using HospitalNet.Backend.Models;

namespace HospitalNet.UI.Dialogs
{
    /// <summary>
    /// Add or edit patient records.
    /// </summary>
    public partial class AddPatientDialog : Window
    {
        private PatientManager _patientManager;
        private Patient _editingPatient;
        private readonly bool _isEditMode;

        public AddPatientDialog()
        {
            InitializeComponent();
            _isEditMode = false;
            TitleTextBlock.Text = "Add New Patient";
            DateOfBirthPicker.SelectedDate = DateTime.Today;
        }

        public AddPatientDialog(Patient patientToEdit)
        {
            InitializeComponent();
            _isEditMode = true;
            _editingPatient = patientToEdit;
            TitleTextBlock.Text = "Edit Patient";
            PopulateFields();
        }

        private void PopulateFields()
        {
            if (_editingPatient != null)
            {
                FirstNameTextBox.Text = _editingPatient.FirstName;
                LastNameTextBox.Text = _editingPatient.LastName;
                PhoneTextBox.Text = _editingPatient.Phone;
                EmailTextBox.Text = _editingPatient.Email;
                DateOfBirthPicker.SelectedDate = _editingPatient.DateOfBirth;
                MedicalHistoryTextBox.Text = _editingPatient.MedicalHistory ?? string.Empty;
                AllergiesTextBox.Text = _editingPatient.Allergies ?? string.Empty;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
                {
                    StatusTextBlock.Text = "First name is required";
                    FirstNameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
                {
                    StatusTextBlock.Text = "Last name is required";
                    LastNameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
                {
                    StatusTextBlock.Text = "Phone is required";
                    PhoneTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                {
                    StatusTextBlock.Text = "Email is required";
                    EmailTextBox.Focus();
                    return;
                }

                if (!DateOfBirthPicker.SelectedDate.HasValue)
                {
                    StatusTextBlock.Text = "Date of birth is required";
                    return;
                }

                if (DateOfBirthPicker.SelectedDate.Value > DateTime.Today)
                {
                    StatusTextBlock.Text = "Date of birth cannot be in the future";
                    return;
                }

                _patientManager = new PatientManager(App.ConnectionString);

                if (_isEditMode)
                {
                    _editingPatient.FirstName = FirstNameTextBox.Text;
                    _editingPatient.LastName = LastNameTextBox.Text;
                    _editingPatient.Phone = PhoneTextBox.Text;
                    _editingPatient.Email = EmailTextBox.Text;
                    _editingPatient.DateOfBirth = DateOfBirthPicker.SelectedDate.Value;
                    _editingPatient.MedicalHistory = MedicalHistoryTextBox.Text;
                    _editingPatient.Allergies = AllergiesTextBox.Text;

                    // Ensure required fields have safe defaults for validation
                    _editingPatient.Gender = string.IsNullOrWhiteSpace(_editingPatient.Gender) ? "Unspecified" : _editingPatient.Gender;
                    _editingPatient.Address = string.IsNullOrWhiteSpace(_editingPatient.Address) ? "N/A" : _editingPatient.Address;
                    _editingPatient.City = string.IsNullOrWhiteSpace(_editingPatient.City) ? "N/A" : _editingPatient.City;
                    _editingPatient.PostalCode = string.IsNullOrWhiteSpace(_editingPatient.PostalCode) ? "00000" : _editingPatient.PostalCode;
                    _editingPatient.InsuranceProviderID = _editingPatient.InsuranceProviderID;

                    _patientManager.UpdatePatient(_editingPatient);
                    StatusTextBlock.Text = "Patient updated successfully";
                }
                else
                {
                    var newPatient = new Patient
                    {
                        FirstName = FirstNameTextBox.Text,
                        LastName = LastNameTextBox.Text,
                        Phone = PhoneTextBox.Text,
                        Email = EmailTextBox.Text,
                        DateOfBirth = DateOfBirthPicker.SelectedDate.Value,
                        MedicalHistory = MedicalHistoryTextBox.Text,
                        Allergies = AllergiesTextBox.Text,
                        Gender = "Unspecified",
                        Address = "N/A",
                        City = "N/A",
                        PostalCode = "00000",
                        InsuranceProviderID = 0,
                        RegistrationDate = DateTime.Now,
                        IsActive = true
                    };

                    _patientManager.AddPatient(newPatient);
                    StatusTextBlock.Text = "Patient added successfully";
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
                MessageBox.Show(
                    $"Failed to save patient:\n{ex.Message}",
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
