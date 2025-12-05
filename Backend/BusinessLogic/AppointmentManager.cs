using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using HospitalNet.Backend.Infrastructure;
using HospitalNet.Backend.Models;

namespace HospitalNet.Backend.BusinessLogic
{
    /// <summary>
    /// Manages appointment-related business logic and database operations.
    /// Handles scheduling, cancellation, and availability checks.
    /// </summary>
    public class AppointmentManager
    {
        private readonly DatabaseHelper _dbHelper;

        /// <summary>
        /// Initializes a new instance of the AppointmentManager class.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        public AppointmentManager(string connectionString)
        {
            _dbHelper = new DatabaseHelper(connectionString);
        }

        /// <summary>
        /// Schedules a new appointment with double-booking prevention.
        /// Calls sp_CreateAppointment and captures OUTPUT parameters.
        /// </summary>
        /// <param name="appointment">The appointment to schedule.</param>
        /// <returns>The scheduled appointment with AppointmentID populated.</returns>
        /// <exception cref="Exception">Thrown if appointment cannot be scheduled due to double-booking.</exception>
        public Appointment ScheduleAppointment(Appointment appointment)
        {
            try
            {
                // Validate the appointment model first
                if (!appointment.IsValid())
                {
                    throw new Exception("Appointment validation failed. All required fields must be provided.");
                }

                // Check if doctor is available for this time slot
                bool isAvailable = CheckDoctorAvailability(
                    appointment.DoctorID,
                    appointment.AppointmentDateTime,
                    appointment.DurationMinutes);

                if (!isAvailable)
                {
                    throw new Exception("Doctor is not available for the requested time slot.");
                }

                // Create parameters for stored procedure
                var parameters = new List<SqlParameter>
                {
                    DatabaseHelper.CreateInputParameter("@PatientID", appointment.PatientID),
                    DatabaseHelper.CreateInputParameter("@DoctorID", appointment.DoctorID),
                    DatabaseHelper.CreateInputParameter("@AppointmentDateTime", appointment.AppointmentDateTime),
                    DatabaseHelper.CreateInputParameter("@DurationMinutes", appointment.DurationMinutes),
                    DatabaseHelper.CreateInputParameter("@ReasonForVisit", appointment.ReasonForVisit ?? ""),
                    DatabaseHelper.CreateInputParameter("@Status", appointment.Status ?? Appointment.AppointmentStatus.Scheduled),
                    // OUTPUT parameters to capture results
                    DatabaseHelper.CreateOutputParameter("@AppointmentID", SqlDbType.Int),
                    DatabaseHelper.CreateOutputParameter("@ErrorMessage", SqlDbType.NVarChar)
                };

                // Execute stored procedure and capture outputs
                var outputs = _dbHelper.ExecuteNonQueryWithOutputs("sp_CreateAppointment", parameters.ToArray());

                // Check if there was an error (double-booking or other constraint violation)
                string errorMessage = (string)outputs["@ErrorMessage"];
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    throw new Exception($"Failed to schedule appointment: {errorMessage}");
                }

                // Extract the newly created AppointmentID
                appointment.AppointmentID = (int)outputs["@AppointmentID"];
                appointment.CreatedDate = DateTime.Now;
                appointment.UpdatedDate = DateTime.Now;

                return appointment;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while scheduling appointment: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error scheduling appointment: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cancels an existing appointment.
        /// Calls sp_CancelAppointment with the cancellation reason.
        /// </summary>
        /// <param name="appointmentId">The ID of the appointment to cancel.</param>
        /// <param name="cancellationReason">The reason for cancellation.</param>
        /// <returns>True if cancellation was successful; false otherwise.</returns>
        public bool CancelAppointment(int appointmentId, string cancellationReason)
        {
            try
            {
                if (appointmentId <= 0)
                {
                    throw new ArgumentException("Appointment ID must be greater than 0.", nameof(appointmentId));
                }

                if (string.IsNullOrWhiteSpace(cancellationReason))
                {
                    throw new ArgumentException("Cancellation reason must be provided.", nameof(cancellationReason));
                }

                // Create parameters for stored procedure
                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@AppointmentID", appointmentId),
                    DatabaseHelper.CreateInputParameter("@CancellationReason", cancellationReason),
                    DatabaseHelper.CreateInputParameter("@CancellationDateTime", DateTime.Now)
                };

                // Execute stored procedure
                int rowsAffected = _dbHelper.ExecuteNonQuery("sp_CancelAppointment", parameters);

                // Check if the cancellation was successful
                return rowsAffected > 0;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while canceling appointment: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error canceling appointment: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if a doctor is available for a specific time slot.
        /// Calls sp_CheckDoctorAvailability stored procedure.
        /// </summary>
        /// <param name="doctorId">The ID of the doctor.</param>
        /// <param name="appointmentDateTime">The requested appointment date and time.</param>
        /// <param name="durationMinutes">The duration of the appointment in minutes.</param>
        /// <returns>True if the doctor is available; false otherwise.</returns>
        public bool CheckDoctorAvailability(int doctorId, DateTime appointmentDateTime, int durationMinutes)
        {
            try
            {
                if (doctorId <= 0)
                {
                    throw new ArgumentException("Doctor ID must be greater than 0.", nameof(doctorId));
                }

                if (durationMinutes <= 0 || durationMinutes > 480) // Max 8 hours
                {
                    throw new ArgumentException("Duration must be between 1 and 480 minutes.", nameof(durationMinutes));
                }

                // Create parameters for stored procedure
                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@DoctorID", doctorId),
                    DatabaseHelper.CreateInputParameter("@AppointmentDateTime", appointmentDateTime),
                    DatabaseHelper.CreateInputParameter("@DurationMinutes", durationMinutes)
                };

                // Execute stored procedure - returns 1 if available, 0 if not
                object result = _dbHelper.ExecuteScalar("sp_CheckDoctorAvailability", parameters);

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result) == 1;
                }

                return false;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while checking availability: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking doctor availability: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves all appointments for a specific doctor within a date range.
        /// Calls sp_GetDoctorSchedule stored procedure.
        /// </summary>
        /// <param name="doctorId">The ID of the doctor.</param>
        /// <param name="startDate">The start date for the schedule.</param>
        /// <param name="endDate">The end date for the schedule.</param>
        /// <returns>A list of appointments for the doctor within the date range.</returns>
        public List<Appointment> GetDoctorSchedule(int doctorId, DateTime startDate, DateTime endDate)
        {
            try
            {
                if (doctorId <= 0)
                {
                    throw new ArgumentException("Doctor ID must be greater than 0.", nameof(doctorId));
                }

                if (startDate > endDate)
                {
                    throw new ArgumentException("Start date cannot be after end date.");
                }

                // Create parameters for stored procedure
                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@DoctorID", doctorId),
                    DatabaseHelper.CreateInputParameter("@StartDate", startDate),
                    DatabaseHelper.CreateInputParameter("@EndDate", endDate)
                };

                // Execute stored procedure and map results to Appointment objects
                var appointments = _dbHelper.ExecuteReader<Appointment>(
                    "sp_GetDoctorSchedule",
                    reader => new Appointment
                    {
                        AppointmentID = DatabaseHelper.GetIntValue(reader, "AppointmentID"),
                        PatientID = DatabaseHelper.GetIntValue(reader, "PatientID"),
                        DoctorID = DatabaseHelper.GetIntValue(reader, "DoctorID"),
                        AppointmentDateTime = DatabaseHelper.GetDateTimeValue(reader, "AppointmentDateTime"),
                        DurationMinutes = DatabaseHelper.GetIntValue(reader, "DurationMinutes"),
                        ReasonForVisit = DatabaseHelper.GetStringValue(reader, "ReasonForVisit"),
                        Status = DatabaseHelper.GetStringValue(reader, "Status"),
                        Notes = DatabaseHelper.GetStringValue(reader, "Notes"),
                        CreatedDate = DatabaseHelper.GetDateTimeValue(reader, "CreatedDate"),
                        UpdatedDate = DatabaseHelper.GetDateTimeValue(reader, "UpdatedDate")
                    },
                    parameters);

                return appointments;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving doctor schedule: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving doctor schedule: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves a specific appointment by its ID.
        /// </summary>
        /// <param name="appointmentId">The ID of the appointment.</param>
        /// <returns>The appointment if found; null otherwise.</returns>
        public Appointment GetAppointmentById(int appointmentId)
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

                var appointments = _dbHelper.ExecuteReader<Appointment>(
                    "sp_GetAppointmentById",
                    reader => new Appointment
                    {
                        AppointmentID = DatabaseHelper.GetIntValue(reader, "AppointmentID"),
                        PatientID = DatabaseHelper.GetIntValue(reader, "PatientID"),
                        DoctorID = DatabaseHelper.GetIntValue(reader, "DoctorID"),
                        AppointmentDateTime = DatabaseHelper.GetDateTimeValue(reader, "AppointmentDateTime"),
                        DurationMinutes = DatabaseHelper.GetIntValue(reader, "DurationMinutes"),
                        ReasonForVisit = DatabaseHelper.GetStringValue(reader, "ReasonForVisit"),
                        Status = DatabaseHelper.GetStringValue(reader, "Status"),
                        Notes = DatabaseHelper.GetStringValue(reader, "Notes"),
                        CancellationReason = DatabaseHelper.GetStringValue(reader, "CancellationReason"),
                        CancellationDateTime = DatabaseHelper.GetDateTimeValue(reader, "CancellationDateTime"),
                        CreatedDate = DatabaseHelper.GetDateTimeValue(reader, "CreatedDate"),
                        UpdatedDate = DatabaseHelper.GetDateTimeValue(reader, "UpdatedDate")
                    },
                    parameters);

                return appointments.Count > 0 ? appointments[0] : null;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving appointment: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving appointment: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Marks an appointment as completed.
        /// Calls sp_CompleteAppointment stored procedure.
        /// </summary>
        /// <param name="appointmentId">The ID of the appointment to complete.</param>
        /// <returns>True if completion was successful; false otherwise.</returns>
        public bool CompleteAppointment(int appointmentId)
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

                int rowsAffected = _dbHelper.ExecuteNonQuery("sp_CompleteAppointment", parameters);
                return rowsAffected > 0;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while completing appointment: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error completing appointment: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves all appointments for a specific patient.
        /// </summary>
        /// <param name="patientId">The ID of the patient.</param>
        /// <returns>A list of appointments for the patient.</returns>
        public List<Appointment> GetPatientAppointments(int patientId)
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

                var appointments = _dbHelper.ExecuteReader<Appointment>(
                    "sp_GetPatientAppointments",
                    reader => new Appointment
                    {
                        AppointmentID = DatabaseHelper.GetIntValue(reader, "AppointmentID"),
                        PatientID = DatabaseHelper.GetIntValue(reader, "PatientID"),
                        DoctorID = DatabaseHelper.GetIntValue(reader, "DoctorID"),
                        AppointmentDateTime = DatabaseHelper.GetDateTimeValue(reader, "AppointmentDateTime"),
                        DurationMinutes = DatabaseHelper.GetIntValue(reader, "DurationMinutes"),
                        ReasonForVisit = DatabaseHelper.GetStringValue(reader, "ReasonForVisit"),
                        Status = DatabaseHelper.GetStringValue(reader, "Status"),
                        Notes = DatabaseHelper.GetStringValue(reader, "Notes"),
                        CreatedDate = DatabaseHelper.GetDateTimeValue(reader, "CreatedDate"),
                        UpdatedDate = DatabaseHelper.GetDateTimeValue(reader, "UpdatedDate")
                    },
                    parameters);

                return appointments;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving patient appointments: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving patient appointments: {ex.Message}", ex);
            }
        }
    }
}
