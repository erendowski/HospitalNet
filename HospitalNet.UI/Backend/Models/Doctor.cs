using System;

namespace HospitalNet.Backend.Models
{
    public class Doctor
    {
        public int DoctorID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Specialization { get; set; }
        public string LicenseNumber { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string OfficeLocation { get; set; }
        public int YearsOfExperience { get; set; }
        public int MaxPatientCapacityPerDay { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string DisplayTitle => $"Dr. {LastName} ({Specialization})";

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

        public override string ToString()
        {
            return $"Dr. {FullName} - {Specialization} (License: {LicenseNumber}, Experience: {YearsOfExperience} years, Location: {OfficeLocation})";
        }
    }
}
