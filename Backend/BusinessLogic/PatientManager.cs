using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using HospitalNet.Backend.Infrastructure;
using HospitalNet.Backend.Models;

namespace HospitalNet.Backend.BusinessLogic
{
    /// <summary>
    /// Manages patient-related business logic and database operations.
    /// Handles patient registration, retrieval, and history tracking.
    /// </summary>
    public class PatientManager
    {
        private readonly DatabaseHelper _dbHelper;

        /// <summary>
        /// Initializes a new instance of the PatientManager class.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        public PatientManager(string connectionString)
        {
            _dbHelper = new DatabaseHelper(connectionString);
        }

        // Legacy compatibility helper used by older UI code.
        public Patient AddPatient(Patient patient) => RegisterPatient(patient);

        // Legacy compatibility helper used by older UI code.
        public Patient GetPatientByID(int patientId) => GetPatientById(patientId);

        /// <summary>
        /// Registers a new patient in the system.
        /// Calls sp_CreatePatient stored procedure.
        /// </summary>
        /// <param name="patient">The patient object to register.</param>
        /// <returns>The registered patient with PatientID populated.</returns>
        public Patient RegisterPatient(Patient patient)
        {
            try
            {
                // Validate the patient model first
                if (!patient.IsValid())
                {
                    throw new Exception("Patient validation failed. All required fields must be provided.");
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@FirstName", patient.FirstName),
                    DatabaseHelper.CreateInputParameter("@LastName", patient.LastName),
                    DatabaseHelper.CreateInputParameter("@DateOfBirth", patient.DateOfBirth),
                    DatabaseHelper.CreateInputParameter("@Gender", patient.Gender),
                    DatabaseHelper.CreateInputParameter("@PhoneNumber", patient.PhoneNumber),
                    DatabaseHelper.CreateInputParameter("@Email", patient.Email),
                    DatabaseHelper.CreateInputParameter("@Address", patient.Address),
                    DatabaseHelper.CreateInputParameter("@City", patient.City),
                    DatabaseHelper.CreateInputParameter("@PostalCode", patient.PostalCode),
                    DatabaseHelper.CreateInputParameter("@InsuranceProviderID", patient.InsuranceProviderID),
                    DatabaseHelper.CreateInputParameter("@MedicalHistorySummary", patient.MedicalHistorySummary ?? ""),
                    DatabaseHelper.CreateInputParameter("@IsActive", patient.IsActive),
                    DatabaseHelper.CreateOutputParameter("@PatientID", SqlDbType.Int)
                };

                var outputs = _dbHelper.ExecuteNonQueryWithOutputs("sp_CreatePatient", parameters);

                patient.PatientID = (int)outputs["@PatientID"];
                patient.CreatedDate = DateTime.Now;
                patient.UpdatedDate = DateTime.Now;

                return patient;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while registering patient: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error registering patient: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves a specific patient by ID.
        /// </summary>
        /// <param name="patientId">The ID of the patient.</param>
        /// <returns>The patient object if found; null otherwise.</returns>
        public Patient GetPatientById(int patientId)
        {
            try
            {
                if (patientId <= 0)
                {
                    throw new ArgumentException("Patient ID must be greater than 0.", nameof(patientId));
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@PatientID", patientId)
                };

                var patients = _dbHelper.ExecuteReader<Patient>(
                    "sp_GetPatientById",
                    reader => new Patient
                    {
                        PatientID = DatabaseHelper.GetIntValue(reader, "PatientID"),
                        FirstName = DatabaseHelper.GetStringValue(reader, "FirstName"),
                        LastName = DatabaseHelper.GetStringValue(reader, "LastName"),
                        DateOfBirth = DatabaseHelper.GetDateTimeValue(reader, "DateOfBirth"),
                        Gender = DatabaseHelper.GetStringValue(reader, "Gender"),
                        PhoneNumber = DatabaseHelper.GetStringValue(reader, "PhoneNumber"),
                        Email = DatabaseHelper.GetStringValue(reader, "Email"),
                        Address = DatabaseHelper.GetStringValue(reader, "Address"),
                        City = DatabaseHelper.GetStringValue(reader, "City"),
                        PostalCode = DatabaseHelper.GetStringValue(reader, "PostalCode"),
                        InsuranceProviderID = DatabaseHelper.GetIntValue(reader, "InsuranceProviderID"),
                        MedicalHistorySummary = DatabaseHelper.GetStringValue(reader, "MedicalHistorySummary"),
                        IsActive = DatabaseHelper.GetBoolValue(reader, "IsActive"),
                        CreatedDate = DatabaseHelper.GetDateTimeValue(reader, "CreatedDate"),
                        UpdatedDate = DatabaseHelper.GetDateTimeValue(reader, "UpdatedDate"),
                        LastVisitDate = DatabaseHelper.GetDateTimeValue(reader, "LastVisitDate")
                    },
                    parameters);

                return patients.Count > 0 ? patients[0] : null;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving patient: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving patient: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves all active patients from the system.
        /// </summary>
        /// <returns>A list of active patient objects.</returns>
        public List<Patient> GetAllActivePatients()
        {
            try
            {
                var patients = _dbHelper.ExecuteReader<Patient>(
                    "sp_GetAllActivePatients",
                    reader => new Patient
                    {
                        PatientID = DatabaseHelper.GetIntValue(reader, "PatientID"),
                        FirstName = DatabaseHelper.GetStringValue(reader, "FirstName"),
                        LastName = DatabaseHelper.GetStringValue(reader, "LastName"),
                        DateOfBirth = DatabaseHelper.GetDateTimeValue(reader, "DateOfBirth"),
                        Gender = DatabaseHelper.GetStringValue(reader, "Gender"),
                        PhoneNumber = DatabaseHelper.GetStringValue(reader, "PhoneNumber"),
                        Email = DatabaseHelper.GetStringValue(reader, "Email"),
                        Address = DatabaseHelper.GetStringValue(reader, "Address"),
                        City = DatabaseHelper.GetStringValue(reader, "City"),
                        PostalCode = DatabaseHelper.GetStringValue(reader, "PostalCode"),
                        InsuranceProviderID = DatabaseHelper.GetIntValue(reader, "InsuranceProviderID"),
                        MedicalHistorySummary = DatabaseHelper.GetStringValue(reader, "MedicalHistorySummary"),
                        IsActive = DatabaseHelper.GetBoolValue(reader, "IsActive"),
                        CreatedDate = DatabaseHelper.GetDateTimeValue(reader, "CreatedDate"),
                        UpdatedDate = DatabaseHelper.GetDateTimeValue(reader, "UpdatedDate"),
                        LastVisitDate = DatabaseHelper.GetDateTimeValue(reader, "LastVisitDate")
                    });

                return patients;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving all active patients: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving all active patients: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Searches for patients by name (first name or last name).
        /// </summary>
        /// <param name="searchName">The name or part of the name to search for.</param>
        /// <returns>A list of patients matching the search criteria.</returns>
        public List<Patient> SearchPatientsByName(string searchName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchName))
                {
                    throw new ArgumentException("Search name must be provided.", nameof(searchName));
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@SearchName", searchName)
                };

                var patients = _dbHelper.ExecuteReader<Patient>(
                    "sp_SearchPatientsByName",
                    reader => new Patient
                    {
                        PatientID = DatabaseHelper.GetIntValue(reader, "PatientID"),
                        FirstName = DatabaseHelper.GetStringValue(reader, "FirstName"),
                        LastName = DatabaseHelper.GetStringValue(reader, "LastName"),
                        DateOfBirth = DatabaseHelper.GetDateTimeValue(reader, "DateOfBirth"),
                        Gender = DatabaseHelper.GetStringValue(reader, "Gender"),
                        PhoneNumber = DatabaseHelper.GetStringValue(reader, "PhoneNumber"),
                        Email = DatabaseHelper.GetStringValue(reader, "Email"),
                        Address = DatabaseHelper.GetStringValue(reader, "Address"),
                        City = DatabaseHelper.GetStringValue(reader, "City"),
                        PostalCode = DatabaseHelper.GetStringValue(reader, "PostalCode"),
                        InsuranceProviderID = DatabaseHelper.GetIntValue(reader, "InsuranceProviderID"),
                        MedicalHistorySummary = DatabaseHelper.GetStringValue(reader, "MedicalHistorySummary"),
                        IsActive = DatabaseHelper.GetBoolValue(reader, "IsActive"),
                        CreatedDate = DatabaseHelper.GetDateTimeValue(reader, "CreatedDate"),
                        UpdatedDate = DatabaseHelper.GetDateTimeValue(reader, "UpdatedDate"),
                        LastVisitDate = DatabaseHelper.GetDateTimeValue(reader, "LastVisitDate")
                    },
                    parameters);

                return patients;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while searching patients: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching patients: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Searches for a patient by phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number to search for.</param>
        /// <returns>The patient if found; null otherwise.</returns>
        public Patient SearchPatientByPhoneNumber(string phoneNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    throw new ArgumentException("Phone number must be provided.", nameof(phoneNumber));
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@PhoneNumber", phoneNumber)
                };

                var patients = _dbHelper.ExecuteReader<Patient>(
                    "sp_GetPatientByPhoneNumber",
                    reader => new Patient
                    {
                        PatientID = DatabaseHelper.GetIntValue(reader, "PatientID"),
                        FirstName = DatabaseHelper.GetStringValue(reader, "FirstName"),
                        LastName = DatabaseHelper.GetStringValue(reader, "LastName"),
                        DateOfBirth = DatabaseHelper.GetDateTimeValue(reader, "DateOfBirth"),
                        Gender = DatabaseHelper.GetStringValue(reader, "Gender"),
                        PhoneNumber = DatabaseHelper.GetStringValue(reader, "PhoneNumber"),
                        Email = DatabaseHelper.GetStringValue(reader, "Email"),
                        Address = DatabaseHelper.GetStringValue(reader, "Address"),
                        City = DatabaseHelper.GetStringValue(reader, "City"),
                        PostalCode = DatabaseHelper.GetStringValue(reader, "PostalCode"),
                        InsuranceProviderID = DatabaseHelper.GetIntValue(reader, "InsuranceProviderID"),
                        MedicalHistorySummary = DatabaseHelper.GetStringValue(reader, "MedicalHistorySummary"),
                        IsActive = DatabaseHelper.GetBoolValue(reader, "IsActive"),
                        CreatedDate = DatabaseHelper.GetDateTimeValue(reader, "CreatedDate"),
                        UpdatedDate = DatabaseHelper.GetDateTimeValue(reader, "UpdatedDate"),
                        LastVisitDate = DatabaseHelper.GetDateTimeValue(reader, "LastVisitDate")
                    },
                    parameters);

                return patients.Count > 0 ? patients[0] : null;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while searching patient by phone: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching patient by phone: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves a patient's visit history including appointments and medical records.
        /// Calls sp_GetPatientVisitHistory stored procedure.
        /// </summary>
        /// <param name="patientId">The ID of the patient.</param>
        /// <returns>A complex result object containing appointments and medical records.</returns>
        public PatientVisitHistory GetPatientVisitHistory(int patientId)
        {
            try
            {
                if (patientId <= 0)
                {
                    throw new ArgumentException("Patient ID must be greater than 0.", nameof(patientId));
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@PatientID", patientId)
                };

                // This procedure should return visit history data
                var visitHistory = new PatientVisitHistory
                {
                    PatientID = patientId,
                    Appointments = new List<Appointment>(),
                    MedicalRecords = new List<MedicalRecord>()
                };

                // Execute the stored procedure and populate the visit history
                var results = _dbHelper.ExecuteReader(
                    "sp_GetPatientVisitHistory",
                    parameters);

                // Process the results into appointment and medical record objects
                // This assumes the stored procedure returns both appointments and medical records
                foreach (DataRow row in results.Rows)
                {
                    // Check if this row contains appointment data
                    if (row["AppointmentID"] != DBNull.Value && row["AppointmentDateTime"] != DBNull.Value)
                    {
                        var appointment = new Appointment
                        {
                            AppointmentID = DatabaseHelper.GetIntValue(row, "AppointmentID"),
                            PatientID = DatabaseHelper.GetIntValue(row, "PatientID"),
                            DoctorID = DatabaseHelper.GetIntValue(row, "DoctorID"),
                            AppointmentDateTime = DatabaseHelper.GetDateTimeValue(row, "AppointmentDateTime"),
                            DurationMinutes = DatabaseHelper.GetIntValue(row, "DurationMinutes"),
                            ReasonForVisit = DatabaseHelper.GetStringValue(row, "ReasonForVisit"),
                            Status = DatabaseHelper.GetStringValue(row, "Status"),
                            Notes = DatabaseHelper.GetStringValue(row, "Notes"),
                            CreatedDate = DatabaseHelper.GetDateTimeValue(row, "CreatedDate"),
                            UpdatedDate = DatabaseHelper.GetDateTimeValue(row, "UpdatedDate")
                        };

                        visitHistory.Appointments.Add(appointment);
                    }

                    // Check if this row contains medical record data
                    if (row["RecordID"] != DBNull.Value && row["VisitDate"] != DBNull.Value)
                    {
                        var medicalRecord = new MedicalRecord
                        {
                            RecordID = DatabaseHelper.GetIntValue(row, "RecordID"),
                            AppointmentID = DatabaseHelper.GetIntValue(row, "AppointmentID"),
                            PatientID = DatabaseHelper.GetIntValue(row, "PatientID"),
                            DoctorID = DatabaseHelper.GetIntValue(row, "DoctorID"),
                            VisitDate = DatabaseHelper.GetDateTimeValue(row, "VisitDate"),
                            ClinicalNotes = DatabaseHelper.GetStringValue(row, "ClinicalNotes"),
                            Diagnosis = DatabaseHelper.GetStringValue(row, "Diagnosis"),
                            PrescriptionText = DatabaseHelper.GetStringValue(row, "PrescriptionText"),
                            AllergiesNotedDuringVisit = DatabaseHelper.GetStringValue(row, "AllergiesNotedDuringVisit"),
                            VitalSigns = DatabaseHelper.GetStringValue(row, "VitalSigns"),
                            FollowUpRequired = DatabaseHelper.GetBoolValue(row, "FollowUpRequired"),
                            FollowUpDate = DatabaseHelper.GetDateTimeValue(row, "FollowUpDate"),
                            CreatedDate = DatabaseHelper.GetDateTimeValue(row, "CreatedDate"),
                            UpdatedDate = DatabaseHelper.GetDateTimeValue(row, "UpdatedDate")
                        };

                        visitHistory.MedicalRecords.Add(medicalRecord);
                    }
                }

                return visitHistory;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving patient visit history: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving patient visit history: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Updates an existing patient's information.
        /// Calls sp_UpdatePatient stored procedure.
        /// </summary>
        /// <param name="patient">The updated patient object.</param>
        /// <returns>True if update was successful; false otherwise.</returns>
        public bool UpdatePatient(Patient patient)
        {
            try
            {
                if (!patient.IsValid())
                {
                    throw new Exception("Patient validation failed. All required fields must be provided.");
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@PatientID", patient.PatientID),
                    DatabaseHelper.CreateInputParameter("@FirstName", patient.FirstName),
                    DatabaseHelper.CreateInputParameter("@LastName", patient.LastName),
                    DatabaseHelper.CreateInputParameter("@PhoneNumber", patient.PhoneNumber),
                    DatabaseHelper.CreateInputParameter("@Email", patient.Email),
                    DatabaseHelper.CreateInputParameter("@Address", patient.Address),
                    DatabaseHelper.CreateInputParameter("@City", patient.City),
                    DatabaseHelper.CreateInputParameter("@PostalCode", patient.PostalCode),
                    DatabaseHelper.CreateInputParameter("@MedicalHistorySummary", patient.MedicalHistorySummary ?? ""),
                    DatabaseHelper.CreateInputParameter("@IsActive", patient.IsActive)
                };

                int rowsAffected = _dbHelper.ExecuteNonQuery("sp_UpdatePatient", parameters);
                return rowsAffected > 0;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while updating patient: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating patient: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deactivates a patient (soft delete).
        /// </summary>
        /// <param name="patientId">The ID of the patient to deactivate.</param>
        /// <returns>True if deactivation was successful; false otherwise.</returns>
        public bool DeactivatePatient(int patientId)
        {
            try
            {
                if (patientId <= 0)
                {
                    throw new ArgumentException("Patient ID must be greater than 0.", nameof(patientId));
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@PatientID", patientId)
                };

                int rowsAffected = _dbHelper.ExecuteNonQuery("sp_DeactivatePatient", parameters);
                return rowsAffected > 0;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while deactivating patient: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deactivating patient: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Complex result object containing patient visit history.
    /// Combines appointments and medical records in a single query result.
    /// </summary>
    public class PatientVisitHistory
    {
        /// <summary>
        /// Gets or sets the patient ID.
        /// </summary>
        public int PatientID { get; set; }

        /// <summary>
        /// Gets or sets the list of appointments for the patient.
        /// </summary>
        public List<Appointment> Appointments { get; set; }

        /// <summary>
        /// Gets or sets the list of medical records for the patient.
        /// </summary>
        public List<MedicalRecord> MedicalRecords { get; set; }

        /// <summary>
        /// Gets the total number of visits (appointments + medical records).
        /// </summary>
        public int TotalVisits => (Appointments?.Count ?? 0) + (MedicalRecords?.Count ?? 0);

        /// <summary>
        /// Gets the most recent visit date.
        /// </summary>
        public DateTime? MostRecentVisitDate
        {
            get
            {
                DateTime? mostRecentAppointment = null;
                DateTime? mostRecentMedicalRecord = null;

                if (Appointments?.Count > 0)
                {
                    mostRecentAppointment = Appointments[Appointments.Count - 1].AppointmentDateTime;
                }

                if (MedicalRecords?.Count > 0)
                {
                    mostRecentMedicalRecord = MedicalRecords[MedicalRecords.Count - 1].VisitDate;
                }

                if (mostRecentAppointment.HasValue && mostRecentMedicalRecord.HasValue)
                {
                    return mostRecentAppointment > mostRecentMedicalRecord ? mostRecentAppointment : mostRecentMedicalRecord;
                }

                return mostRecentAppointment ?? mostRecentMedicalRecord;
            }
        }
    }
}
