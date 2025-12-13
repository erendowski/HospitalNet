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

        public PatientsView()
        {
            InitializeComponent();
            LoadPatients();
        }

        private void LoadPatients()
        {
            try
        {
                var dbHelper = new DatabaseHelper(App.ConnectionString);
                if (!dbHelper.TestConnection())
                {
                    _patientManager = null;
                    PatientsDataGrid.ItemsSource = null;
                    StatusTextBlock.Text = "Patients offline (no database connection).";
                    CountTextBlock.Text = "Total: -";
                    return;
                }

                _patientManager = new PatientManager(App.ConnectionString);
                var patients = _patientManager.GetAllActivePatients();

                _allPatients = new ObservableCollection<Patient>(patients);
                _filteredPatients = new ObservableCollection<Patient>(patients);

                PatientsDataGrid.ItemsSource = _filteredPatients;

                StatusTextBlock.Text = $"Loaded {patients.Count} active patients";
                CountTextBlock.Text = $"Total: {patients.Count}";
            }
            catch (Exception ex)
            {
                _patientManager = null;
                PatientsDataGrid.ItemsSource = null;
                StatusTextBlock.Text = $"Patients offline (data unavailable): {ex.Message}";
                CountTextBlock.Text = "Total: -";
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

        private void AddPatientButton_Click(object sender, RoutedEventArgs e)
        {
            var addPatientDialog = new AddPatientDialog();
            bool? result = addPatientDialog.ShowDialog();

            if (result == true)
            {
                LoadPatients();
                StatusTextBlock.Text = "New patient added successfully";
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
                bool? result = editPatientDialog.ShowDialog();

                if (result == true)
                {
                    LoadPatients();
                    StatusTextBlock.Text = "Patient updated successfully";
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
