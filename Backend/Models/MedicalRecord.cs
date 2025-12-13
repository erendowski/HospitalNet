using System;

namespace HospitalNet.Backend.Models
{
    /// <summary>
    /// MedicalRecord POCO Model
    /// Maps to the MedicalRecords table in the HospitalNet database
    /// Stores clinical information including diagnoses and prescriptions
    /// </summary>
    public class MedicalRecord
    {
        /// <summary>
        /// Primary Key: Unique medical record identifier (auto-increment)
        /// </summary>
        public int RecordID { get; set; }

        /// <summary>
        /// Foreign Key: Reference to Appointments table
        /// </summary>
        public int AppointmentID { get; set; }

        /// <summary>
        /// Foreign Key: Reference to Patients table (denormalized for query performance)
        /// </summary>
        public int PatientID { get; set; }

        /// <summary>
        /// Foreign Key: Reference to Doctors table (denormalized for query performance)
        /// </summary>
        public int DoctorID { get; set; }

        /// <summary>
        /// Date of the visit
        /// </summary>
        public DateTime VisitDate { get; set; }

        /// <summary>
        /// Doctor's clinical observations and notes
        /// Detailed medical findings from the appointment
        /// </summary>
        public string ClinicalNotes { get; set; }

        /// <summary>
        /// Medical diagnosis determined during the visit
        /// </summary>
        public string Diagnosis { get; set; }

        /// <summary>
        /// Complete prescription details (UNLIMITED TEXT)
        /// Can include multiple medications, dosages, instructions, warnings
        /// </summary>
        public string PrescriptionText { get; set; }

        /// <summary>
        /// Any allergies noted or identified during the visit
        /// </summary>
        public string AllergiesNotedDuringVisit { get; set; }

        /// <summary>
        /// Vital signs recorded during the visit
        /// Format example: "BP: 120/80, HR: 72, Temp: 98.6F"
        /// </summary>
        public string VitalSigns { get; set; }

        /// <summary>
        /// Optional follow-up notes captured by clinicians
        /// </summary>
        public string FollowUpNotes { get; set; }

        /// <summary>
        /// Whether a follow-up appointment is required
        /// </summary>
        public bool FollowUpRequired { get; set; }

        /// <summary>
        /// Scheduled date for follow-up appointment (if required)
        /// </summary>
        public DateTime? FollowUpDate { get; set; }

        /// <summary>
        /// Timestamp when the record was created
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Timestamp when the record was last updated
        /// </summary>
        public DateTime UpdatedDate { get; set; }

        /// <summary>
        /// Computed property: Time since visit
        /// </summary>
        public TimeSpan TimeSinceVisit => DateTime.Now - VisitDate;

        /// <summary>
        /// Computed property: Whether follow-up is overdue
        /// Returns true if follow-up was required but date has passed
        /// </summary>
        public bool IsFollowUpOverdue
        {
            get
            {
                if (!FollowUpRequired || !FollowUpDate.HasValue)
                    return false;

                return FollowUpDate.Value < DateTime.Today;
            }
        }

        /// <summary>
        /// Computed property: Whether follow-up is upcoming
        /// Returns true if follow-up is required and date is within 7 days
        /// </summary>
        public bool IsFollowUpUpcoming
        {
            get
            {
                if (!FollowUpRequired || !FollowUpDate.HasValue)
                    return false;

                TimeSpan daysUntilFollowUp = FollowUpDate.Value - DateTime.Today;
                return daysUntilFollowUp.TotalDays <= 7 && daysUntilFollowUp.TotalDays > 0;
            }
        }

        /// <summary>
        /// Validates the MedicalRecord object for required fields
        /// </summary>
        /// <returns>True if valid, False otherwise</returns>
        public bool IsValid()
        {
            return AppointmentID > 0 &&
                   PatientID > 0 &&
                   DoctorID > 0 &&
                   VisitDate != DateTime.MinValue &&
                   !string.IsNullOrWhiteSpace(ClinicalNotes) &&
                   !string.IsNullOrWhiteSpace(Diagnosis) &&
                   !string.IsNullOrWhiteSpace(PrescriptionText);
        }

        /// <summary>
        /// Validates the follow-up information if follow-up is required
        /// </summary>
        /// <returns>True if follow-up info is valid (or not required), False otherwise</returns>
        public bool IsFollowUpValid()
        {
            if (!FollowUpRequired)
                return true; // Valid if follow-up not required

            // If follow-up is required, date must be set and in the future
            return FollowUpDate.HasValue && FollowUpDate.Value > VisitDate;
        }

        /// <summary>
        /// Extracts prescription count from the prescription text
        /// Assumes each line is a separate medication
        /// </summary>
        /// <returns>Number of lines in prescription (approximate medication count)</returns>
        public int GetPrescriptionLineCount()
        {
            if (string.IsNullOrWhiteSpace(PrescriptionText))
                return 0;

            return PrescriptionText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        /// <summary>
        /// Returns a string representation of the MedicalRecord
        /// </summary>
        public override string ToString()
        {
            string followUpInfo = FollowUpRequired ? 
                $", Follow-up: {FollowUpDate:yyyy-MM-dd}" : 
                ", No follow-up required";

            return $"Medical Record ID: {RecordID}, " +
                   $"Visit: {VisitDate:yyyy-MM-dd}, " +
                   $"Diagnosis: {Diagnosis}, " +
                   $"Prescriptions: {GetPrescriptionLineCount()} items" +
                   followUpInfo;
        }

        /// <summary>
        /// Returns a summary of the medical record suitable for display
        /// </summary>
        public string GetSummary()
        {
            return $"Visit Date: {VisitDate:MMMM d, yyyy}\n" +
                   $"Diagnosis: {Diagnosis}\n" +
                   $"Clinical Notes: {ClinicalNotes}\n" +
                   $"Vital Signs: {VitalSigns}\n" +
                   $"Prescriptions: {GetPrescriptionLineCount()} medication(s)\n" +
                   (FollowUpRequired ? $"Follow-up Required: {FollowUpDate:MMMM d, yyyy}" : "No follow-up required");
        }
    }
}
