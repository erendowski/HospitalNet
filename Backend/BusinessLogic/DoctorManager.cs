using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using HospitalNet.Backend.Infrastructure;
using HospitalNet.Backend.Models;

namespace HospitalNet.Backend.BusinessLogic
{
    /// <summary>
    /// Manages doctor-related business logic and database operations.
    /// Handles doctor information retrieval, availability checks, and schedule management.
    /// </summary>
    public class DoctorManager
    {
        private readonly DatabaseHelper _dbHelper;

        /// <summary>
        /// Initializes a new instance of the DoctorManager class.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        public DoctorManager(string connectionString)
        {
            _dbHelper = new DatabaseHelper(connectionString);
        }

        /// <summary>
        /// Retrieves all active doctors from the database.
        /// </summary>
        /// <returns>A list of active doctor objects.</returns>
        public List<Doctor> GetAllDoctors()
        {
            try
            {
                // Execute stored procedure that returns all active doctors
                var doctors = _dbHelper.ExecuteReader<Doctor>(
                    "sp_GetAllActiveDoctors",
                    reader => new Doctor
                    {
                        DoctorID = DatabaseHelper.GetIntValue(reader, "DoctorID"),
                        FirstName = DatabaseHelper.GetStringValue(reader, "FirstName"),
                        LastName = DatabaseHelper.GetStringValue(reader, "LastName"),
                        Specialization = DatabaseHelper.GetStringValue(reader, "Specialization"),
                        LicenseNumber = DatabaseHelper.GetStringValue(reader, "LicenseNumber"),
                        PhoneNumber = DatabaseHelper.GetStringValue(reader, "PhoneNumber"),
                        Email = DatabaseHelper.GetStringValue(reader, "Email"),
                        OfficeLocation = DatabaseHelper.GetStringValue(reader, "OfficeLocation"),
                        YearsOfExperience = DatabaseHelper.GetIntValue(reader, "YearsOfExperience"),
                        MaxPatientCapacityPerDay = DatabaseHelper.GetIntValue(reader, "MaxPatientCapacityPerDay"),
                        IsActive = DatabaseHelper.GetBoolValue(reader, "IsActive"),
                        CreatedDate = DatabaseHelper.GetDateTimeValue(reader, "CreatedDate"),
                        UpdatedDate = DatabaseHelper.GetDateTimeValue(reader, "UpdatedDate")
                    });

                return doctors;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving all doctors: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving all doctors: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves a specific doctor by ID.
        /// </summary>
        /// <param name="doctorId">The ID of the doctor.</param>
        /// <returns>The doctor object if found; null otherwise.</returns>
        public Doctor GetDoctorById(int doctorId)
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

                var doctors = _dbHelper.ExecuteReader<Doctor>(
                    "sp_GetDoctorById",
                    reader => new Doctor
                    {
                        DoctorID = DatabaseHelper.GetIntValue(reader, "DoctorID"),
                        FirstName = DatabaseHelper.GetStringValue(reader, "FirstName"),
                        LastName = DatabaseHelper.GetStringValue(reader, "LastName"),
                        Specialization = DatabaseHelper.GetStringValue(reader, "Specialization"),
                        LicenseNumber = DatabaseHelper.GetStringValue(reader, "LicenseNumber"),
                        PhoneNumber = DatabaseHelper.GetStringValue(reader, "PhoneNumber"),
                        Email = DatabaseHelper.GetStringValue(reader, "Email"),
                        OfficeLocation = DatabaseHelper.GetStringValue(reader, "OfficeLocation"),
                        YearsOfExperience = DatabaseHelper.GetIntValue(reader, "YearsOfExperience"),
                        MaxPatientCapacityPerDay = DatabaseHelper.GetIntValue(reader, "MaxPatientCapacityPerDay"),
                        IsActive = DatabaseHelper.GetBoolValue(reader, "IsActive"),
                        CreatedDate = DatabaseHelper.GetDateTimeValue(reader, "CreatedDate"),
                        UpdatedDate = DatabaseHelper.GetDateTimeValue(reader, "UpdatedDate")
                    },
                    parameters);

                return doctors.Count > 0 ? doctors[0] : null;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving doctor: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving doctor: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves doctors by specialization.
        /// </summary>
        /// <param name="specialization">The specialization to search for (e.g., "Cardiology", "Neurology").</param>
        /// <returns>A list of doctors with the specified specialization.</returns>
        public List<Doctor> GetDoctorsBySpecialization(string specialization)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(specialization))
                {
                    throw new ArgumentException("Specialization must be provided.", nameof(specialization));
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@Specialization", specialization)
                };

                var doctors = _dbHelper.ExecuteReader<Doctor>(
                    "sp_GetDoctorsBySpecialization",
                    reader => new Doctor
                    {
                        DoctorID = DatabaseHelper.GetIntValue(reader, "DoctorID"),
                        FirstName = DatabaseHelper.GetStringValue(reader, "FirstName"),
                        LastName = DatabaseHelper.GetStringValue(reader, "LastName"),
                        Specialization = DatabaseHelper.GetStringValue(reader, "Specialization"),
                        LicenseNumber = DatabaseHelper.GetStringValue(reader, "LicenseNumber"),
                        PhoneNumber = DatabaseHelper.GetStringValue(reader, "PhoneNumber"),
                        Email = DatabaseHelper.GetStringValue(reader, "Email"),
                        OfficeLocation = DatabaseHelper.GetStringValue(reader, "OfficeLocation"),
                        YearsOfExperience = DatabaseHelper.GetIntValue(reader, "YearsOfExperience"),
                        MaxPatientCapacityPerDay = DatabaseHelper.GetIntValue(reader, "MaxPatientCapacityPerDay"),
                        IsActive = DatabaseHelper.GetBoolValue(reader, "IsActive"),
                        CreatedDate = DatabaseHelper.GetDateTimeValue(reader, "CreatedDate"),
                        UpdatedDate = DatabaseHelper.GetDateTimeValue(reader, "UpdatedDate")
                    },
                    parameters);

                return doctors;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving doctors by specialization: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving doctors by specialization: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Registers a new doctor in the system.
        /// Calls sp_CreateDoctor stored procedure.
        /// </summary>
        /// <param name="doctor">The doctor object to register.</param>
        /// <returns>The registered doctor with DoctorID populated.</returns>
        public Doctor RegisterDoctor(Doctor doctor)
        {
            try
            {
                // Validate the doctor model first
                if (!doctor.IsValid())
                {
                    throw new Exception("Doctor validation failed. All required fields must be provided.");
                }

                // Check if license number already exists (should be handled by DB constraint, but good practice)
                var existingDoctor = GetDoctorByLicenseNumber(doctor.LicenseNumber);
                if (existingDoctor != null)
                {
                    throw new Exception($"A doctor with license number {doctor.LicenseNumber} already exists.");
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@FirstName", doctor.FirstName),
                    DatabaseHelper.CreateInputParameter("@LastName", doctor.LastName),
                    DatabaseHelper.CreateInputParameter("@Specialization", doctor.Specialization),
                    DatabaseHelper.CreateInputParameter("@LicenseNumber", doctor.LicenseNumber),
                    DatabaseHelper.CreateInputParameter("@PhoneNumber", doctor.PhoneNumber),
                    DatabaseHelper.CreateInputParameter("@Email", doctor.Email),
                    DatabaseHelper.CreateInputParameter("@OfficeLocation", doctor.OfficeLocation),
                    DatabaseHelper.CreateInputParameter("@YearsOfExperience", doctor.YearsOfExperience),
                    DatabaseHelper.CreateInputParameter("@MaxPatientCapacityPerDay", doctor.MaxPatientCapacityPerDay),
                    DatabaseHelper.CreateInputParameter("@IsActive", doctor.IsActive),
                    DatabaseHelper.CreateOutputParameter("@DoctorID", SqlDbType.Int)
                };

                var outputs = _dbHelper.ExecuteNonQueryWithOutputs("sp_CreateDoctor", parameters);

                doctor.DoctorID = (int)outputs["@DoctorID"];
                doctor.CreatedDate = DateTime.Now;
                doctor.UpdatedDate = DateTime.Now;

                return doctor;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while registering doctor: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error registering doctor: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves a doctor by license number.
        /// </summary>
        /// <param name="licenseNumber">The license number to search for.</param>
        /// <returns>The doctor object if found; null otherwise.</returns>
        public Doctor GetDoctorByLicenseNumber(string licenseNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(licenseNumber))
                {
                    throw new ArgumentException("License number must be provided.", nameof(licenseNumber));
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@LicenseNumber", licenseNumber)
                };

                var doctors = _dbHelper.ExecuteReader<Doctor>(
                    "sp_GetDoctorByLicenseNumber",
                    reader => new Doctor
                    {
                        DoctorID = DatabaseHelper.GetIntValue(reader, "DoctorID"),
                        FirstName = DatabaseHelper.GetStringValue(reader, "FirstName"),
                        LastName = DatabaseHelper.GetStringValue(reader, "LastName"),
                        Specialization = DatabaseHelper.GetStringValue(reader, "Specialization"),
                        LicenseNumber = DatabaseHelper.GetStringValue(reader, "LicenseNumber"),
                        PhoneNumber = DatabaseHelper.GetStringValue(reader, "PhoneNumber"),
                        Email = DatabaseHelper.GetStringValue(reader, "Email"),
                        OfficeLocation = DatabaseHelper.GetStringValue(reader, "OfficeLocation"),
                        YearsOfExperience = DatabaseHelper.GetIntValue(reader, "YearsOfExperience"),
                        MaxPatientCapacityPerDay = DatabaseHelper.GetIntValue(reader, "MaxPatientCapacityPerDay"),
                        IsActive = DatabaseHelper.GetBoolValue(reader, "IsActive"),
                        CreatedDate = DatabaseHelper.GetDateTimeValue(reader, "CreatedDate"),
                        UpdatedDate = DatabaseHelper.GetDateTimeValue(reader, "UpdatedDate")
                    },
                    parameters);

                return doctors.Count > 0 ? doctors[0] : null;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving doctor by license: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving doctor by license: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Updates an existing doctor's information.
        /// Calls sp_UpdateDoctor stored procedure.
        /// </summary>
        /// <param name="doctor">The updated doctor object.</param>
        /// <returns>True if update was successful; false otherwise.</returns>
        public bool UpdateDoctor(Doctor doctor)
        {
            try
            {
                if (!doctor.IsValid())
                {
                    throw new Exception("Doctor validation failed. All required fields must be provided.");
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@DoctorID", doctor.DoctorID),
                    DatabaseHelper.CreateInputParameter("@FirstName", doctor.FirstName),
                    DatabaseHelper.CreateInputParameter("@LastName", doctor.LastName),
                    DatabaseHelper.CreateInputParameter("@Specialization", doctor.Specialization),
                    DatabaseHelper.CreateInputParameter("@PhoneNumber", doctor.PhoneNumber),
                    DatabaseHelper.CreateInputParameter("@Email", doctor.Email),
                    DatabaseHelper.CreateInputParameter("@OfficeLocation", doctor.OfficeLocation),
                    DatabaseHelper.CreateInputParameter("@YearsOfExperience", doctor.YearsOfExperience),
                    DatabaseHelper.CreateInputParameter("@MaxPatientCapacityPerDay", doctor.MaxPatientCapacityPerDay),
                    DatabaseHelper.CreateInputParameter("@IsActive", doctor.IsActive)
                };

                int rowsAffected = _dbHelper.ExecuteNonQuery("sp_UpdateDoctor", parameters);
                return rowsAffected > 0;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while updating doctor: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating doctor: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the current number of appointments for a doctor on a specific date.
        /// </summary>
        /// <param name="doctorId">The ID of the doctor.</param>
        /// <param name="appointmentDate">The date to check.</param>
        /// <returns>The number of appointments on that date.</returns>
        public int GetDoctorAppointmentCountForDate(int doctorId, DateTime appointmentDate)
        {
            try
            {
                if (doctorId <= 0)
                {
                    throw new ArgumentException("Doctor ID must be greater than 0.", nameof(doctorId));
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@DoctorID", doctorId),
                    DatabaseHelper.CreateInputParameter("@AppointmentDate", appointmentDate.Date)
                };

                object result = _dbHelper.ExecuteScalar("sp_GetDoctorAppointmentCount", parameters);

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }

                return 0;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while getting doctor appointment count: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting doctor appointment count: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if a doctor has reached their maximum patient capacity for a specific date.
        /// </summary>
        /// <param name="doctorId">The ID of the doctor.</param>
        /// <param name="appointmentDate">The date to check.</param>
        /// <returns>True if the doctor is at or above capacity; false otherwise.</returns>
        public bool IsDoctorAtCapacityForDate(int doctorId, DateTime appointmentDate)
        {
            try
            {
                var doctor = GetDoctorById(doctorId);
                if (doctor == null)
                {
                    throw new Exception($"Doctor with ID {doctorId} not found.");
                }

                int appointmentCount = GetDoctorAppointmentCountForDate(doctorId, appointmentDate);
                return appointmentCount >= doctor.MaxPatientCapacityPerDay;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking doctor capacity: {ex.Message}", ex);
            }
        }
    }
}
