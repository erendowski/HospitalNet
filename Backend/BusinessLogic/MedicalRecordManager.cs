using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using HospitalNet.Backend.Infrastructure;
using HospitalNet.Backend.Models;

namespace HospitalNet.Backend.BusinessLogic
{
    /// <summary>
    /// Manages medical record-related business logic and database operations.
    /// Handles clinical data entry, prescription recording, and medical history.
    /// </summary>
    public class MedicalRecordManager
    {
        private readonly DatabaseHelper _dbHelper;

        /// <summary>
        /// Initializes a new instance of the MedicalRecordManager class.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        public MedicalRecordManager(string connectionString)
        {
            _dbHelper = new DatabaseHelper(connectionString);
        }

        /// <summary>
        /// Records a new medical visit with clinical data.
        /// Calls sp_RecordMedicalVisit stored procedure.
        /// This is where doctors enter diagnosis, prescription, vital signs, and other clinical information.
        /// </summary>
        /// <param name="medicalRecord">The medical record object containing clinical data.</param>
        /// <returns>The recorded medical record with RecordID populated.</returns>
        public MedicalRecord AddMedicalRecord(MedicalRecord medicalRecord)
        {
            try
            {
                // Validate the medical record model first
                if (!medicalRecord.IsValid())
                {
                    throw new Exception("Medical record validation failed. All required fields must be provided.");
                }

                // Validate follow-up information if follow-up is required
                if (medicalRecord.FollowUpRequired && !medicalRecord.IsFollowUpValid())
                {
                    throw new Exception("If follow-up is required, a valid follow-up date must be provided.");
                }

                // Create parameters for stored procedure
                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@AppointmentID", medicalRecord.AppointmentID),
                    DatabaseHelper.CreateInputParameter("@PatientID", medicalRecord.PatientID),
                    DatabaseHelper.CreateInputParameter("@DoctorID", medicalRecord.DoctorID),
                    DatabaseHelper.CreateInputParameter("@VisitDate", medicalRecord.VisitDate),
                    DatabaseHelper.CreateInputParameter("@ClinicalNotes", medicalRecord.ClinicalNotes),
                    DatabaseHelper.CreateInputParameter("@Diagnosis", medicalRecord.Diagnosis),
                    // PrescriptionText can be unlimited - stored as NVARCHAR(MAX)
                    DatabaseHelper.CreateInputParameter("@PrescriptionText", medicalRecord.PrescriptionText ?? ""),
                    DatabaseHelper.CreateInputParameter("@AllergiesNotedDuringVisit", medicalRecord.AllergiesNotedDuringVisit ?? ""),
                    DatabaseHelper.CreateInputParameter("@VitalSigns", medicalRecord.VitalSigns ?? ""),
                    DatabaseHelper.CreateInputParameter("@FollowUpRequired", medicalRecord.FollowUpRequired),
                    DatabaseHelper.CreateInputParameter("@FollowUpDate", 
                        medicalRecord.FollowUpRequired && medicalRecord.FollowUpDate != DateTime.MinValue 
                            ? (object)medicalRecord.FollowUpDate 
                            : DBNull.Value),
                    DatabaseHelper.CreateOutputParameter("@RecordID", SqlDbType.Int)
                };

                // Execute stored procedure
                var outputs = _dbHelper.ExecuteNonQueryWithOutputs("sp_RecordMedicalVisit", parameters);

                // Extract the newly created RecordID
                medicalRecord.RecordID = (int)outputs["@RecordID"];
                medicalRecord.CreatedDate = DateTime.Now;
                medicalRecord.UpdatedDate = DateTime.Now;

                return medicalRecord;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while recording medical visit: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error recording medical visit: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves a specific medical record by ID.
        /// </summary>
        /// <param name="recordId">The ID of the medical record.</param>
        /// <returns>The medical record if found; null otherwise.</returns>
        public MedicalRecord GetMedicalRecordById(int recordId)
        {
            try
            {
                if (recordId <= 0)
                {
                    throw new ArgumentException("Record ID must be greater than 0.", nameof(recordId));
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@RecordID", recordId)
                };

                var records = _dbHelper.ExecuteReader<MedicalRecord>(
                    "sp_GetMedicalRecordById",
                    reader => new MedicalRecord
                    {
                        RecordID = DatabaseHelper.GetIntValue(reader, "RecordID"),
                        AppointmentID = DatabaseHelper.GetIntValue(reader, "AppointmentID"),
                        PatientID = DatabaseHelper.GetIntValue(reader, "PatientID"),
                        DoctorID = DatabaseHelper.GetIntValue(reader, "DoctorID"),
                        VisitDate = DatabaseHelper.GetDateTimeValue(reader, "VisitDate"),
                        ClinicalNotes = DatabaseHelper.GetStringValue(reader, "ClinicalNotes"),
                        Diagnosis = DatabaseHelper.GetStringValue(reader, "Diagnosis"),
                        PrescriptionText = DatabaseHelper.GetStringValue(reader, "PrescriptionText"),
                        AllergiesNotedDuringVisit = DatabaseHelper.GetStringValue(reader, "AllergiesNotedDuringVisit"),
                        VitalSigns = DatabaseHelper.GetStringValue(reader, "VitalSigns"),
                        FollowUpRequired = DatabaseHelper.GetBoolValue(reader, "FollowUpRequired"),
                        FollowUpDate = DatabaseHelper.GetDateTimeValue(reader, "FollowUpDate"),
                        CreatedDate = DatabaseHelper.GetDateTimeValue(reader, "CreatedDate"),
                        UpdatedDate = DatabaseHelper.GetDateTimeValue(reader, "UpdatedDate")
                    },
                    parameters);

                return records.Count > 0 ? records[0] : null;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving medical record: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving medical record: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves all medical records for a specific patient.
        /// Calls sp_GetPatientMedicalRecords stored procedure.
        /// </summary>
        /// <param name="patientId">The ID of the patient.</param>
        /// <returns>A list of medical records for the patient.</returns>
        public List<MedicalRecord> GetPatientMedicalRecords(int patientId)
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

                var records = _dbHelper.ExecuteReader<MedicalRecord>(
                    "sp_GetPatientMedicalRecords",
                    reader => new MedicalRecord
                    {
                        RecordID = DatabaseHelper.GetIntValue(reader, "RecordID"),
                        AppointmentID = DatabaseHelper.GetIntValue(reader, "AppointmentID"),
                        PatientID = DatabaseHelper.GetIntValue(reader, "PatientID"),
                        DoctorID = DatabaseHelper.GetIntValue(reader, "DoctorID"),
                        VisitDate = DatabaseHelper.GetDateTimeValue(reader, "VisitDate"),
                        ClinicalNotes = DatabaseHelper.GetStringValue(reader, "ClinicalNotes"),
                        Diagnosis = DatabaseHelper.GetStringValue(reader, "Diagnosis"),
                        PrescriptionText = DatabaseHelper.GetStringValue(reader, "PrescriptionText"),
                        AllergiesNotedDuringVisit = DatabaseHelper.GetStringValue(reader, "AllergiesNotedDuringVisit"),
                        VitalSigns = DatabaseHelper.GetStringValue(reader, "VitalSigns"),
                        FollowUpRequired = DatabaseHelper.GetBoolValue(reader, "FollowUpRequired"),
                        FollowUpDate = DatabaseHelper.GetDateTimeValue(reader, "FollowUpDate"),
                        CreatedDate = DatabaseHelper.GetDateTimeValue(reader, "CreatedDate"),
                        UpdatedDate = DatabaseHelper.GetDateTimeValue(reader, "UpdatedDate")
                    },
                    parameters);

                return records;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving patient medical records: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving patient medical records: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves medical records for a specific doctor.
        /// </summary>
        /// <param name="doctorId">The ID of the doctor.</param>
        /// <returns>A list of medical records created by the doctor.</returns>
        public List<MedicalRecord> GetDoctorMedicalRecords(int doctorId)
        {
            try
            {
                if (doctorId <= 0)
                {
                    throw new ArgumentException("Doctor ID must be greater than 0.", nameof(doctorId));
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@DoctorID", doctorId)
                };

                var records = _dbHelper.ExecuteReader<MedicalRecord>(
                    "sp_GetDoctorMedicalRecords",
                    reader => new MedicalRecord
                    {
                        RecordID = DatabaseHelper.GetIntValue(reader, "RecordID"),
                        AppointmentID = DatabaseHelper.GetIntValue(reader, "AppointmentID"),
                        PatientID = DatabaseHelper.GetIntValue(reader, "PatientID"),
                        DoctorID = DatabaseHelper.GetIntValue(reader, "DoctorID"),
                        VisitDate = DatabaseHelper.GetDateTimeValue(reader, "VisitDate"),
                        ClinicalNotes = DatabaseHelper.GetStringValue(reader, "ClinicalNotes"),
                        Diagnosis = DatabaseHelper.GetStringValue(reader, "Diagnosis"),
                        PrescriptionText = DatabaseHelper.GetStringValue(reader, "PrescriptionText"),
                        AllergiesNotedDuringVisit = DatabaseHelper.GetStringValue(reader, "AllergiesNotedDuringVisit"),
                        VitalSigns = DatabaseHelper.GetStringValue(reader, "VitalSigns"),
                        FollowUpRequired = DatabaseHelper.GetBoolValue(reader, "FollowUpRequired"),
                        FollowUpDate = DatabaseHelper.GetDateTimeValue(reader, "FollowUpDate"),
                        CreatedDate = DatabaseHelper.GetDateTimeValue(reader, "CreatedDate"),
                        UpdatedDate = DatabaseHelper.GetDateTimeValue(reader, "UpdatedDate")
                    },
                    parameters);

                return records;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving doctor medical records: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving doctor medical records: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves medical records for a specific appointment.
        /// </summary>
        /// <param name="appointmentId">The ID of the appointment.</param>
        /// <returns>The medical record associated with the appointment if found; null otherwise.</returns>
        public MedicalRecord GetMedicalRecordByAppointmentId(int appointmentId)
        {
            try
            {
                if (appointmentId <= 0)
                {
                    throw new ArgumentException("Appointment ID must be greater than 0.", nameof(appointmentId));
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@AppointmentID", appointmentId)
                };

                var records = _dbHelper.ExecuteReader<MedicalRecord>(
                    "sp_GetMedicalRecordByAppointmentId",
                    reader => new MedicalRecord
                    {
                        RecordID = DatabaseHelper.GetIntValue(reader, "RecordID"),
                        AppointmentID = DatabaseHelper.GetIntValue(reader, "AppointmentID"),
                        PatientID = DatabaseHelper.GetIntValue(reader, "PatientID"),
                        DoctorID = DatabaseHelper.GetIntValue(reader, "DoctorID"),
                        VisitDate = DatabaseHelper.GetDateTimeValue(reader, "VisitDate"),
                        ClinicalNotes = DatabaseHelper.GetStringValue(reader, "ClinicalNotes"),
                        Diagnosis = DatabaseHelper.GetStringValue(reader, "Diagnosis"),
                        PrescriptionText = DatabaseHelper.GetStringValue(reader, "PrescriptionText"),
                        AllergiesNotedDuringVisit = DatabaseHelper.GetStringValue(reader, "AllergiesNotedDuringVisit"),
                        VitalSigns = DatabaseHelper.GetStringValue(reader, "VitalSigns"),
                        FollowUpRequired = DatabaseHelper.GetBoolValue(reader, "FollowUpRequired"),
                        FollowUpDate = DatabaseHelper.GetDateTimeValue(reader, "FollowUpDate"),
                        CreatedDate = DatabaseHelper.GetDateTimeValue(reader, "CreatedDate"),
                        UpdatedDate = DatabaseHelper.GetDateTimeValue(reader, "UpdatedDate")
                    },
                    parameters);

                return records.Count > 0 ? records[0] : null;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving medical record by appointment: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving medical record by appointment: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves all medical records that require follow-up appointments.
        /// </summary>
        /// <returns>A list of medical records with pending follow-ups.</returns>
        public List<MedicalRecord> GetFollowUpRequiredRecords()
        {
            try
            {
                var records = _dbHelper.ExecuteReader<MedicalRecord>(
                    "sp_GetFollowUpRequiredRecords",
                    reader => new MedicalRecord
                    {
                        RecordID = DatabaseHelper.GetIntValue(reader, "RecordID"),
                        AppointmentID = DatabaseHelper.GetIntValue(reader, "AppointmentID"),
                        PatientID = DatabaseHelper.GetIntValue(reader, "PatientID"),
                        DoctorID = DatabaseHelper.GetIntValue(reader, "DoctorID"),
                        VisitDate = DatabaseHelper.GetDateTimeValue(reader, "VisitDate"),
                        ClinicalNotes = DatabaseHelper.GetStringValue(reader, "ClinicalNotes"),
                        Diagnosis = DatabaseHelper.GetStringValue(reader, "Diagnosis"),
                        PrescriptionText = DatabaseHelper.GetStringValue(reader, "PrescriptionText"),
                        AllergiesNotedDuringVisit = DatabaseHelper.GetStringValue(reader, "AllergiesNotedDuringVisit"),
                        VitalSigns = DatabaseHelper.GetStringValue(reader, "VitalSigns"),
                        FollowUpRequired = DatabaseHelper.GetBoolValue(reader, "FollowUpRequired"),
                        FollowUpDate = DatabaseHelper.GetDateTimeValue(reader, "FollowUpDate"),
                        CreatedDate = DatabaseHelper.GetDateTimeValue(reader, "CreatedDate"),
                        UpdatedDate = DatabaseHelper.GetDateTimeValue(reader, "UpdatedDate")
                    });

                return records;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving follow-up required records: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving follow-up required records: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves overdue follow-up appointments.
        /// </summary>
        /// <returns>A list of medical records with overdue follow-ups.</returns>
        public List<MedicalRecord> GetOverdueFollowUps()
        {
            try
            {
                var records = _dbHelper.ExecuteReader<MedicalRecord>(
                    "sp_GetOverdueFollowUps",
                    reader => new MedicalRecord
                    {
                        RecordID = DatabaseHelper.GetIntValue(reader, "RecordID"),
                        AppointmentID = DatabaseHelper.GetIntValue(reader, "AppointmentID"),
                        PatientID = DatabaseHelper.GetIntValue(reader, "PatientID"),
                        DoctorID = DatabaseHelper.GetIntValue(reader, "DoctorID"),
                        VisitDate = DatabaseHelper.GetDateTimeValue(reader, "VisitDate"),
                        ClinicalNotes = DatabaseHelper.GetStringValue(reader, "ClinicalNotes"),
                        Diagnosis = DatabaseHelper.GetStringValue(reader, "Diagnosis"),
                        PrescriptionText = DatabaseHelper.GetStringValue(reader, "PrescriptionText"),
                        AllergiesNotedDuringVisit = DatabaseHelper.GetStringValue(reader, "AllergiesNotedDuringVisit"),
                        VitalSigns = DatabaseHelper.GetStringValue(reader, "VitalSigns"),
                        FollowUpRequired = DatabaseHelper.GetBoolValue(reader, "FollowUpRequired"),
                        FollowUpDate = DatabaseHelper.GetDateTimeValue(reader, "FollowUpDate"),
                        CreatedDate = DatabaseHelper.GetDateTimeValue(reader, "CreatedDate"),
                        UpdatedDate = DatabaseHelper.GetDateTimeValue(reader, "UpdatedDate")
                    });

                return records;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving overdue follow-ups: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving overdue follow-ups: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Updates an existing medical record.
        /// Calls sp_UpdateMedicalRecord stored procedure.
        /// </summary>
        /// <param name="medicalRecord">The updated medical record object.</param>
        /// <returns>True if update was successful; false otherwise.</returns>
        public bool UpdateMedicalRecord(MedicalRecord medicalRecord)
        {
            try
            {
                if (!medicalRecord.IsValid())
                {
                    throw new Exception("Medical record validation failed. All required fields must be provided.");
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@RecordID", medicalRecord.RecordID),
                    DatabaseHelper.CreateInputParameter("@ClinicalNotes", medicalRecord.ClinicalNotes),
                    DatabaseHelper.CreateInputParameter("@Diagnosis", medicalRecord.Diagnosis),
                    DatabaseHelper.CreateInputParameter("@PrescriptionText", medicalRecord.PrescriptionText ?? ""),
                    DatabaseHelper.CreateInputParameter("@AllergiesNotedDuringVisit", medicalRecord.AllergiesNotedDuringVisit ?? ""),
                    DatabaseHelper.CreateInputParameter("@VitalSigns", medicalRecord.VitalSigns ?? ""),
                    DatabaseHelper.CreateInputParameter("@FollowUpRequired", medicalRecord.FollowUpRequired),
                    DatabaseHelper.CreateInputParameter("@FollowUpDate", 
                        medicalRecord.FollowUpRequired && medicalRecord.FollowUpDate != DateTime.MinValue 
                            ? (object)medicalRecord.FollowUpDate 
                            : DBNull.Value)
                };

                int rowsAffected = _dbHelper.ExecuteNonQuery("sp_UpdateMedicalRecord", parameters);
                return rowsAffected > 0;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while updating medical record: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating medical record: {ex.Message}", ex);
            }
        }
    }
}
