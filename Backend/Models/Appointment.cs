using System;

namespace HospitalNet.Backend.Models
{
    /// <summary>
    /// Appointment POCO Model
    /// Maps to the Appointments table in the HospitalNet database
    /// CRITICAL TABLE: Contains double-booking prevention via UNIQUE constraint
    /// </summary>
    public class Appointment
    {
        /// <summary>
        /// Primary Key: Unique appointment identifier (auto-increment)
        /// </summary>
        public int AppointmentID { get; set; }

        /// <summary>
        /// Foreign Key: Reference to Patients table
        /// </summary>
        public int PatientID { get; set; }

        /// <summary>
        /// Foreign Key: Reference to Doctors table
        /// </summary>
        public int DoctorID { get; set; }

        /// <summary>
        /// Appointment date and time
        /// CRITICAL: Part of UNIQUE constraint with DoctorID to prevent double-booking
        /// </summary>
        public DateTime AppointmentDateTime { get; set; }

        /// <summary>
        /// Duration of the appointment in minutes (default 30)
        /// </summary>
        public int DurationMinutes { get; set; }

        /// <summary>
        /// Reason for the visit / Chief complaint
        /// </summary>
        public string ReasonForVisit { get; set; }

        /// <summary>
        /// Appointment status: 'Scheduled', 'Completed', 'Cancelled', 'No-Show'
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Additional notes about the appointment
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Reason for cancellation (if applicable)
        /// </summary>
        public string CancellationReason { get; set; }

        /// <summary>
        /// Timestamp when the appointment was cancelled (if applicable)
        /// </summary>
        public DateTime? CancellationDateTime { get; set; }

        /// <summary>
        /// Timestamp when the record was created
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Timestamp when the record was last updated
        /// </summary>
        public DateTime UpdatedDate { get; set; }

        /// <summary>
        /// Computed property: Appointment end time (start + duration)
        /// </summary>
        public DateTime AppointmentEndTime => AppointmentDateTime.AddMinutes(DurationMinutes);

        /// <summary>
        /// Computed property: Whether appointment is in the past
        /// </summary>
        public bool IsPast => AppointmentDateTime < DateTime.Now;

        /// <summary>
        /// Computed property: Whether appointment is in the future
        /// </summary>
        public bool IsFuture => AppointmentDateTime > DateTime.Now;

        /// <summary>
        /// Computed property: Time until appointment in minutes
        /// Returns negative if appointment is in the past
        /// </summary>
        public int MinutesUntilAppointment
        {
            get
            {
                TimeSpan difference = AppointmentDateTime - DateTime.Now;
                return (int)difference.TotalMinutes;
            }
        }

        /// <summary>
        /// Status constants for validation
        /// </summary>
        public static class AppointmentStatus
        {
            public const string Scheduled = "Scheduled";
            public const string Completed = "Completed";
            public const string Cancelled = "Cancelled";
            public const string NoShow = "No-Show";
        }

        /// <summary>
        /// Validates the Appointment object for required fields
        /// </summary>
        /// <returns>True if valid, False otherwise</returns>
        public bool IsValid()
        {
            bool isValidStatus = Status == AppointmentStatus.Scheduled ||
                               Status == AppointmentStatus.Completed ||
                               Status == AppointmentStatus.Cancelled ||
                               Status == AppointmentStatus.NoShow;

            return PatientID > 0 &&
                   DoctorID > 0 &&
                   AppointmentDateTime != DateTime.MinValue &&
                   DurationMinutes > 0 &&
                   !string.IsNullOrWhiteSpace(ReasonForVisit) &&
                   isValidStatus;
        }

        /// <summary>
        /// Checks if the appointment overlaps with another appointment
        /// Used for double-booking detection
        /// </summary>
        /// <param name="otherAppointment">Another appointment to check against</param>
        /// <returns>True if appointments overlap, False otherwise</returns>
        public bool OverlapsWith(Appointment otherAppointment)
        {
            if (otherAppointment == null)
                return false;

            // Don't consider cancelled or no-show as overlapping
            if (otherAppointment.Status == AppointmentStatus.Cancelled || 
                otherAppointment.Status == AppointmentStatus.NoShow)
                return false;

            // Overlap occurs if:
            // - Other end time > this start time AND
            // - Other start time < this end time
            return otherAppointment.AppointmentEndTime > AppointmentDateTime &&
                   otherAppointment.AppointmentDateTime < AppointmentEndTime;
        }

        /// <summary>
        /// Returns a string representation of the Appointment
        /// </summary>
        public override string ToString()
        {
            return $"Appointment ID: {AppointmentID}, " +
                   $"Patient ID: {PatientID}, " +
                   $"Doctor ID: {DoctorID}, " +
                   $"DateTime: {AppointmentDateTime:yyyy-MM-dd HH:mm}, " +
                   $"Status: {Status}, " +
                   $"Reason: {ReasonForVisit}";
        }
    }
}
