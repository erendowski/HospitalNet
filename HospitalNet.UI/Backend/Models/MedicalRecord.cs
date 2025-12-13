using System;

namespace HospitalNet.Backend.Models
{
    public class MedicalRecord
    {
        public int RecordID { get; set; }
        public int AppointmentID { get; set; }
        public int PatientID { get; set; }
        public int DoctorID { get; set; }
        public DateTime VisitDate { get; set; }
        public string ClinicalNotes { get; set; }
        public string Diagnosis { get; set; }
        public string PrescriptionText { get; set; }
        public string AllergiesNotedDuringVisit { get; set; }
        public string VitalSigns { get; set; }
        public string FollowUpNotes { get; set; }
        public bool FollowUpRequired { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public TimeSpan TimeSinceVisit => DateTime.Now - VisitDate;
        public bool IsFollowUpOverdue => FollowUpRequired && FollowUpDate.HasValue && FollowUpDate.Value < DateTime.Today;

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

        public bool IsFollowUpValid()
        {
            if (!FollowUpRequired)
                return true;

            return FollowUpDate.HasValue && FollowUpDate.Value > VisitDate;
        }

        public int GetPrescriptionLineCount()
        {
            if (string.IsNullOrWhiteSpace(PrescriptionText))
                return 0;

            return PrescriptionText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        public override string ToString()
        {
            string followUpInfo = FollowUpRequired ? $", Follow-up: {FollowUpDate:yyyy-MM-dd}" : ", No follow-up required";

            return $"Medical Record ID: {RecordID}, Visit: {VisitDate:yyyy-MM-dd}, Diagnosis: {Diagnosis}, Prescriptions: {GetPrescriptionLineCount()} items" + followUpInfo;
        }

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
