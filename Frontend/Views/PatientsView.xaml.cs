using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using HospitalNet.Backend.Managers;
using HospitalNet.Backend.Models;

namespace HospitalNet.Frontend.Views
{
    /// <summary>
    /// Interaction logic for PatientsView.xaml
    /// Manages patient listing, search, add, edit, and delete operations
    /// </summary>
    public partial class PatientsView : UserControl
    {
        private PatientManager _patientManager;
        private ObservableCollection<Patient> _allPatients;
        private ObservableCollection<Patient> _filteredPatients;

        public PatientsView()
        {
            InitializeComponent();
            LoadPatients();
        }

        /// <summary>
        /// Load all active patients from database
        /// </summary>
        private void LoadPatients()
        {
            try
            {
                _patientManager = new PatientManager(App.ConnectionString);
                var patients = _patientManager.GetAllActivePatients();
                
                _allPatients = new ObservableCollection<Patient>(patients);
                _filteredPatients = new ObservableCollection<Patient>(patients);
                
                PatientsDataGrid.ItemsSource = _filteredPatients;
                
                StatusTextBlock.Text = $"✓ Loaded {patients.Count} active patients";
                CountTextBlock.Text = $"Total: {patients.Count}";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"✗ Error loading patients: {ex.Message}";
                MessageBox.Show(
                    $"Failed to load patients:\n{ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handle search text changes - filter patients in real-time
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.ToLower();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredPatients.Clear();
                foreach (var patient in _allPatients)
                {
                    _filteredPatients.Add(patient);
                }
            }
            else
            {
                // Filter by first name, last name, phone, or email
                var filtered = _allPatients.FindAll(p =>
                    p.FirstName.ToLower().Contains(searchText) ||
                    p.LastName.ToLower().Contains(searchText) ||
                    p.Phone.ToLower().Contains(searchText) ||
                    p.Email.ToLower().Contains(searchText));

                _filteredPatients.Clear();
                foreach (var patient in filtered)
                {
                    _filteredPatients.Add(patient);
                }
            }
            
            CountTextBlock.Text = $"Total: {_filteredPatients.Count}";
        }

        /// <summary>
        /// Open dialog to add new patient
        /// </summary>
        private void AddPatientButton_Click(object sender, RoutedEventArgs e)
        {
            var addPatientDialog = new AddPatientDialog();
            bool? result = addPatientDialog.ShowDialog();
            
            if (result == true)
            {
                // Refresh patient list after adding new patient
                LoadPatients();
                StatusTextBlock.Text = "✓ New patient added successfully";
            }
        }

        /// <summary>
        /// Edit selected patient
        /// </summary>
        private void EditPatient_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            int patientID = (int)btn.Tag;
            
            var selectedPatient = _patientManager.GetPatientByID(patientID);
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
                StatusTextBlock.Text = "✓ Patient updated successfully";
            }
        }

        /// <summary>
        /// Delete (deactivate) selected patient
        /// </summary>
        private void DeletePatient_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            int patientID = (int)btn.Tag;
            
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to remove this patient? This action will deactivate the patient record.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _patientManager.DeactivatePatient(patientID);
                    LoadPatients();
                    StatusTextBlock.Text = "✓ Patient removed successfully";
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
