using System;

namespace HospitalNet.Backend.Models
{
    public class Patient
    {
        public int PatientID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public int InsuranceProviderID { get; set; }
        public string MedicalHistorySummary { get; set; }
        public string Allergies { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime? LastVisitDate { get; set; }

        // Legacy aliases used by earlier UI code.
        public string Phone
        {
            get => PhoneNumber;
            set => PhoneNumber = value;
        }

        public string MedicalHistory
        {
            get => MedicalHistorySummary;
            set => MedicalHistorySummary = value;
        }

        public DateTime RegistrationDate
        {
            get => CreatedDate;
            set => CreatedDate = value;
        }

        public string FullName => $"{FirstName} {LastName}";

        public int Age
        {
            get
            {
                DateTime today = DateTime.Today;
                int age = today.Year - DateOfBirth.Year;
                if (DateOfBirth.Date > today.AddYears(-age))
                    age--;
                return age;
            }
        }

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

        public override string ToString()
        {
            return $"Patient: {FullName} (ID: {PatientID}, Age: {Age}, Contact: {PhoneNumber})";
        }
    }
}
