using System;

namespace HospitalNet.Backend.Models
{
    /// <summary>
    /// Patient POCO Model
    /// Maps to the Patients table in the HospitalNet database
    /// </summary>
    public class Patient
    {
        /// <summary>
        /// Primary Key: Unique patient identifier (auto-increment)
        /// </summary>
        public int PatientID { get; set; }

        /// <summary>
        /// Patient's first name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Patient's last name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Patient's date of birth
        /// </summary>
        public DateTime DateOfBirth { get; set; }

        /// <summary>
        /// Patient's gender: 'Male', 'Female', 'Other'
        /// </summary>
        public string Gender { get; set; }

        /// <summary>
        /// Patient's phone number for contact
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Patient's email address (optional)
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Patient's street address
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Patient's city
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Patient's postal code
        /// </summary>
        public string PostalCode { get; set; }

        /// <summary>
        /// Insurance provider identification number (optional)
        /// </summary>
        public string InsuranceProviderID { get; set; }

        /// <summary>
        /// Summary of patient's medical history
        /// </summary>
        public string MedicalHistorySummary { get; set; }

        /// <summary>
        /// Soft delete flag: 1 = active, 0 = inactive
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
        /// Date of the patient's last visit
        /// </summary>
        public DateTime? LastVisitDate { get; set; }

        /// <summary>
        /// Computed property: Patient's full name
        /// </summary>
        public string FullName => $"{FirstName} {LastName}";

        /// <summary>
        /// Computed property: Patient's age based on DateOfBirth
        /// </summary>
        public int Age
        {
            get
            {
                DateTime today = DateTime.Today;
                int age = today.Year - DateOfBirth.Year;

                // Subtract 1 if birthday hasn't occurred this year yet
                if (DateOfBirth.Date > today.AddYears(-age))
                    age--;

                return age;
            }
        }

        /// <summary>
        /// Validates the Patient object for required fields
        /// </summary>
        /// <returns>True if valid, False otherwise</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(FirstName) &&
                   !string.IsNullOrWhiteSpace(LastName) &&
                   DateOfBirth != DateTime.MinValue &&
                   !string.IsNullOrWhiteSpace(Gender) &&
                   !string.IsNullOrWhiteSpace(PhoneNumber) &&
                   !string.IsNullOrWhiteSpace(Address) &&
                   !string.IsNullOrWhiteSpace(City) &&
                   !string.IsNullOrWhiteSpace(PostalCode);
        }

        /// <summary>
        /// Returns a string representation of the Patient
        /// </summary>
        public override string ToString()
        {
            return $"Patient: {FullName} (ID: {PatientID}, Age: {Age}, Contact: {PhoneNumber})";
        }
    }
}
