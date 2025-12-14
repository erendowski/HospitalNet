using System;
using System.Windows;
using System.Windows.Controls;
using HospitalNet.Backend.BusinessLogic;
using HospitalNet.Backend.Models;

namespace HospitalNet.UI.Dialogs
{
    /// <summary>
    /// Add or edit doctor records.
    /// </summary>
    public partial class AddDoctorDialog : Window
    {
        private DoctorManager _doctorManager;
        private Doctor _editingDoctor;
        private readonly bool _isEditMode;

        public AddDoctorDialog()
        {
            InitializeComponent();
            _isEditMode = false;
            TitleTextBlock.Text = "Add New Doctor";
            SpecializationComboBox.SelectedIndex = 0;
        }

        public AddDoctorDialog(Doctor doctorToEdit)
        {
            InitializeComponent();
            _isEditMode = true;
            _editingDoctor = doctorToEdit;
            TitleTextBlock.Text = "Edit Doctor";
            PopulateFields();
        }

        private void PopulateFields()
        {
            if (_editingDoctor != null)
            {
                FirstNameTextBox.Text = _editingDoctor.FirstName;
                LastNameTextBox.Text = _editingDoctor.LastName;
                LicenseNumberTextBox.Text = _editingDoctor.LicenseNumber;
                PhoneTextBox.Text = _editingDoctor.PhoneNumber;
                EmailTextBox.Text = _editingDoctor.Email;
                OfficeLocationTextBox.Text = _editingDoctor.OfficeLocation;
                YearsOfExperienceTextBox.Text = _editingDoctor.YearsOfExperience.ToString();
                MaxPatientsTextBox.Text = _editingDoctor.MaxPatientCapacityPerDay.ToString();
                SalaryTextBox.Text = _editingDoctor.Salary.ToString("F2");
                IsActiveCheckBox.IsChecked = _editingDoctor.IsActive;

                // Select specialization in combo
                foreach (ComboBoxItem item in SpecializationComboBox.Items)
                {
                    if (item.Content.ToString() == _editingDoctor.Specialization)
                    {
                        SpecializationComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required fields
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

                if (SpecializationComboBox.SelectedItem == null)
                {
                    StatusTextBlock.Text = "Specialization is required";
                    return;
                }

                if (string.IsNullOrWhiteSpace(LicenseNumberTextBox.Text))
                {
                    StatusTextBlock.Text = "License number is required";
                    LicenseNumberTextBox.Focus();
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

                if (string.IsNullOrWhiteSpace(OfficeLocationTextBox.Text))
                {
                    StatusTextBlock.Text = "Office location is required";
                    OfficeLocationTextBox.Focus();
                    return;
                }

                if (!int.TryParse(YearsOfExperienceTextBox.Text, out int yearsOfExperience) || yearsOfExperience < 0)
                {
                    StatusTextBlock.Text = "Years of experience must be a valid non-negative number";
                    YearsOfExperienceTextBox.Focus();
                    return;
                }

                if (!int.TryParse(MaxPatientsTextBox.Text, out int maxPatients) || maxPatients <= 0)
                {
                    StatusTextBlock.Text = "Max patients per day must be greater than 0";
                    MaxPatientsTextBox.Focus();
                    return;
                }

                if (!decimal.TryParse(SalaryTextBox.Text, out decimal salary) || salary < 0)
                {
                    StatusTextBlock.Text = "Salary must be a valid non-negative number";
                    SalaryTextBox.Focus();
                    return;
                }

                var specialization = (SpecializationComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "General Practice";

                _doctorManager = new DoctorManager(App.ConnectionString);

                if (_isEditMode)
                {
                    _editingDoctor.FirstName = FirstNameTextBox.Text.Trim();
                    _editingDoctor.LastName = LastNameTextBox.Text.Trim();
                    _editingDoctor.Specialization = specialization;
                    _editingDoctor.LicenseNumber = LicenseNumberTextBox.Text.Trim();
                    _editingDoctor.PhoneNumber = PhoneTextBox.Text.Trim();
                    _editingDoctor.Email = EmailTextBox.Text.Trim();
                    _editingDoctor.OfficeLocation = OfficeLocationTextBox.Text.Trim();
                    _editingDoctor.YearsOfExperience = yearsOfExperience;
                    _editingDoctor.MaxPatientCapacityPerDay = maxPatients;
                    _editingDoctor.Salary = salary;
                    _editingDoctor.IsActive = IsActiveCheckBox.IsChecked ?? true;

                    _doctorManager.UpdateDoctor(_editingDoctor);
                    StatusTextBlock.Text = "Doctor updated successfully";
                }
                else
                {
                    var newDoctor = new Doctor
                    {
                        FirstName = FirstNameTextBox.Text.Trim(),
                        LastName = LastNameTextBox.Text.Trim(),
                        Specialization = specialization,
                        LicenseNumber = LicenseNumberTextBox.Text.Trim(),
                        PhoneNumber = PhoneTextBox.Text.Trim(),
                        Email = EmailTextBox.Text.Trim(),
                        OfficeLocation = OfficeLocationTextBox.Text.Trim(),
                        YearsOfExperience = yearsOfExperience,
                        MaxPatientCapacityPerDay = maxPatients,
                        Salary = salary,
                        IsActive = IsActiveCheckBox.IsChecked ?? true,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    };

                    _doctorManager.RegisterDoctor(newDoctor);
                    StatusTextBlock.Text = "Doctor added successfully";
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
                MessageBox.Show(
                    $"Failed to save doctor:\n{ex.Message}",
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
