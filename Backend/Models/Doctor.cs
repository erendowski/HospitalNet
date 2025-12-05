using System;

namespace HospitalNet.Backend.Models
{
    /// <summary>
    /// Doctor POCO Model
    /// Maps to the Doctors table in the HospitalNet database
    /// </summary>
    public class Doctor
    {
        /// <summary>
        /// Primary Key: Unique doctor identifier (auto-increment)
        /// </summary>
        public int DoctorID { get; set; }

        /// <summary>
        /// Doctor's first name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Doctor's last name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Medical specialization (e.g., Cardiology, Pediatrics, etc.)
        /// </summary>
        public string Specialization { get; set; }

        /// <summary>
        /// Medical license number (unique, verified)
        /// </summary>
        public string LicenseNumber { get; set; }

        /// <summary>
        /// Doctor's contact phone number
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Doctor's email address (unique)
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Office location or room number
        /// </summary>
        public string OfficeLocation { get; set; }

        /// <summary>
        /// Years of experience in medical practice
        /// </summary>
        public int YearsOfExperience { get; set; }

        /// <summary>
        /// Maximum number of patients the doctor can see per day
        /// </summary>
        public int MaxPatientCapacityPerDay { get; set; }

        /// <summary>
        /// Active status: 1 = active, 0 = inactive
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Timestamp when the record was created
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Timestamp when the record was last updated
        /// </summary>
        public DateTime UpdatedDate { get; set; }

        /// <summary>
        /// Computed property: Doctor's full name
        /// </summary>
        public string FullName => $"{FirstName} {LastName}";

        /// <summary>
        /// Computed property: Display title with specialty
        /// </summary>
        public string DisplayTitle => $"Dr. {LastName} ({Specialization})";

        /// <summary>
        /// Validates the Doctor object for required fields
        /// </summary>
        /// <returns>True if valid, False otherwise</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(FirstName) &&
                   !string.IsNullOrWhiteSpace(LastName) &&
                   !string.IsNullOrWhiteSpace(Specialization) &&
                   !string.IsNullOrWhiteSpace(LicenseNumber) &&
                   !string.IsNullOrWhiteSpace(PhoneNumber) &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(OfficeLocation) &&
                   YearsOfExperience >= 0 &&
                   MaxPatientCapacityPerDay > 0;
        }

        /// <summary>
        /// Returns a string representation of the Doctor
        /// </summary>
        public override string ToString()
        {
            return $"Dr. {FullName} - {Specialization} (License: {LicenseNumber}, Experience: {YearsOfExperience} years, Location: {OfficeLocation})";
        }
    }
}
