using System;
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
    /// Manages patient listing, search, add, edit, and delete operations.
    /// </summary>
    public partial class PatientsView : UserControl
    {
        private PatientManager _patientManager;
        private ObservableCollection<Patient> _allPatients = new ObservableCollection<Patient>();
        private ObservableCollection<Patient> _filteredPatients = new ObservableCollection<Patient>();
        private Patient _selectedPatient;

        public PatientsView()
        {
            InitializeComponent();
            if (App.OfflineMode)
            {
                PatientsDataGrid.ItemsSource = null;
                StatusTextBlock.Text = "Patients offline (no database connection).";
                CountTextBlock.Text = "Total: -";
                AddPatientButton.IsEnabled = false;
                return;
            }

            LoadPatients();
        }

        private void SetSelectedPatient(Patient patient)
        {
            _selectedPatient = patient;

            if (patient == null)
            {
                SelectedPatientNameText.Text = "-";
                SelectedPatientMetaText.Text = "-";
                SelectedPatientPhoneText.Text = "-";
                SelectedPatientEmailText.Text = "-";
                SelectedPatientDobText.Text = "-";
                SelectedPatientAgeText.Text = "-";
                SelectedPatientGenderText.Text = "-";
                SelectedPatientLastVisitText.Text = "-";
                SelectedPatientAllergiesText.Text = "-";
                SelectedPatientMedicalHistoryText.Text = "-";
                return;
            }

            SelectedPatientNameText.Text = patient.FullName;
            SelectedPatientMetaText.Text = $"ID: {patient.PatientID} • Active: {(patient.IsActive ? "Yes" : "No")}";
            SelectedPatientPhoneText.Text = string.IsNullOrWhiteSpace(patient.Phone) ? "-" : patient.Phone;
            SelectedPatientEmailText.Text = string.IsNullOrWhiteSpace(patient.Email) ? "-" : patient.Email;
            SelectedPatientDobText.Text = patient.DateOfBirth == DateTime.MinValue ? "-" : patient.DateOfBirth.ToString("d");
            SelectedPatientAgeText.Text = patient.DateOfBirth == DateTime.MinValue ? "-" : patient.Age.ToString();
            SelectedPatientGenderText.Text = string.IsNullOrWhiteSpace(patient.Gender) ? "-" : patient.Gender;
            SelectedPatientLastVisitText.Text = patient.LastVisitDate.HasValue ? patient.LastVisitDate.Value.ToString("g") : "-";
            SelectedPatientAllergiesText.Text = string.IsNullOrWhiteSpace(patient.Allergies) ? "-" : patient.Allergies;
            SelectedPatientMedicalHistoryText.Text = string.IsNullOrWhiteSpace(patient.MedicalHistorySummary) ? "-" : patient.MedicalHistorySummary;
        }

        private void LoadPatients()
        {
            try
            {
                if (App.OfflineMode)
                {
                    _patientManager = null;
                    PatientsDataGrid.ItemsSource = null;
                    StatusTextBlock.Text = "Patients offline (no database connection).";
                    CountTextBlock.Text = "Total: -";
                    AddPatientButton.IsEnabled = false;
                    SetSelectedPatient(null);
                    return;
                }

                if (string.IsNullOrWhiteSpace(App.ConnectionString))
                {
                    _patientManager = null;
                    PatientsDataGrid.ItemsSource = null;
                    StatusTextBlock.Text = "Patients offline (no active database connection).";
                    CountTextBlock.Text = "Total: -";
                    AddPatientButton.IsEnabled = false;
                    SetSelectedPatient(null);
                    return;
                }

                var dbHelper = new DatabaseHelper(App.ConnectionString);
                if (!dbHelper.TestConnection())
                {
                    _patientManager = null;
                    PatientsDataGrid.ItemsSource = null;
                    StatusTextBlock.Text = "Patients offline (no database connection).";
                    CountTextBlock.Text = "Total: -";
                    AddPatientButton.IsEnabled = false;
                    SetSelectedPatient(null);
                    return;
                }

                _patientManager = new PatientManager(App.ConnectionString);
                var patients = _patientManager.GetAllActivePatients();

                _allPatients = new ObservableCollection<Patient>(patients);
                _filteredPatients = new ObservableCollection<Patient>(patients);

                PatientsDataGrid.ItemsSource = _filteredPatients;

                StatusTextBlock.Text = $"Loaded {patients.Count} active patients";
                CountTextBlock.Text = $"Total: {patients.Count}";
                AddPatientButton.IsEnabled = true;
                SetSelectedPatient(null);
            }
            catch (Exception ex)
            {
                _patientManager = null;
                PatientsDataGrid.ItemsSource = null;
                StatusTextBlock.Text = $"Patients offline (data unavailable): {ex.Message}";
                CountTextBlock.Text = "Total: -";
                AddPatientButton.IsEnabled = false;
                SetSelectedPatient(null);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.ToLowerInvariant();

            _filteredPatients.Clear();

            var source = string.IsNullOrWhiteSpace(searchText)
                ? _allPatients
                : new ObservableCollection<Patient>(_allPatients.Where(p =>
                    (p.FirstName ?? string.Empty).ToLowerInvariant().Contains(searchText) ||
                    (p.LastName ?? string.Empty).ToLowerInvariant().Contains(searchText) ||
                    (p.Phone ?? string.Empty).ToLowerInvariant().Contains(searchText) ||
                    (p.Email ?? string.Empty).ToLowerInvariant().Contains(searchText)));

            foreach (var patient in source)
            {
                _filteredPatients.Add(patient);
            }

            CountTextBlock.Text = $"Total: {_filteredPatients.Count}";
        }

        private void PatientsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatientsDataGrid.SelectedItem is Patient patient)
            {
                SetSelectedPatient(patient);
                return;
            }

            SetSelectedPatient(null);
        }

        private void AddPatientButton_Click(object sender, RoutedEventArgs e)
        {
            if (_patientManager == null)
            {
                StatusTextBlock.Text = "Cannot add while offline.";
                return;
            }

            var addPatientDialog = new AddPatientDialog();
            addPatientDialog.Owner = Window.GetWindow(this);
            bool? result = addPatientDialog.ShowDialog();

            if (result == true)
            {
                // Update UI immediately from the dialog result (avoids relying on a full reload).
                if (addPatientDialog.SavedPatient != null)
                {
                    _allPatients.Add(addPatientDialog.SavedPatient);

                    // Re-apply filter so the grid updates consistently.
                    SearchTextBox_TextChanged(SearchTextBox, null);

                    StatusTextBlock.Text = "New patient added successfully";
                    SetSelectedPatient(addPatientDialog.SavedPatient);
                }
                else
                {
                    LoadPatients();
                    // Don't overwrite LoadPatients() status in case it failed.
                }
            }
        }

        private void EditPatient_Click(object sender, RoutedEventArgs e)
        {
            if (_patientManager == null)
            {
                StatusTextBlock.Text = "Cannot edit while offline.";
                return;
            }

            if (sender is Button btn && btn.Tag is int patientId)
            {
                var selectedPatient = _patientManager.GetPatientByID(patientId);
                if (selectedPatient == null)
                {
                    MessageBox.Show(
                        "Patient not found.",
                        "Edit Patient",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var editPatientDialog = new AddPatientDialog(selectedPatient);
                editPatientDialog.Owner = Window.GetWindow(this);
                bool? result = editPatientDialog.ShowDialog();

                if (result == true)
                {
                    int editedId = selectedPatient.PatientID;
                    LoadPatients();

                    // Restore selection and details for the edited patient.
                    var refreshed = _allPatients.FirstOrDefault(p => p.PatientID == editedId);
                    if (refreshed != null)
                    {
                        PatientsDataGrid.SelectedItem = _filteredPatients.FirstOrDefault(p => p.PatientID == editedId) ?? refreshed;
                        SetSelectedPatient(refreshed);
                    }
                    // Don't overwrite LoadPatients() status in case it failed.
                }
            }
        }

        private void DeletePatient_Click(object sender, RoutedEventArgs e)
        {
            if (_patientManager == null)
            {
                StatusTextBlock.Text = "Cannot delete while offline.";
                return;
            }

            if (sender is Button btn && btn.Tag is int patientId)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to remove this patient? This action will deactivate the patient record.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _patientManager.DeactivatePatient(patientId);
                        LoadPatients();
                        StatusTextBlock.Text = "Patient removed successfully";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Failed to delete patient:\n{ex.Message}",
                            "Delete Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
