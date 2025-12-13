using System;

namespace HospitalNet.Backend.Models
{
    public class Appointment
    {
        public int AppointmentID { get; set; }
        public int PatientID { get; set; }
        public int DoctorID { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public int DurationMinutes { get; set; }
        public string ReasonForVisit { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public string CancellationReason { get; set; }
        public DateTime? CancellationDateTime { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime AppointmentEndTime => AppointmentDateTime.AddMinutes(DurationMinutes);
        public bool IsPast => AppointmentDateTime < DateTime.Now;
        public bool IsFuture => AppointmentDateTime > DateTime.Now;
        public int MinutesUntilAppointment => (int)(AppointmentDateTime - DateTime.Now).TotalMinutes;

        public static class AppointmentStatus
        {
            public const string Scheduled = "Scheduled";
            public const string Completed = "Completed";
            public const string Cancelled = "Cancelled";
            public const string NoShow = "No-Show";
        }

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

        public bool OverlapsWith(Appointment otherAppointment)
        {
            if (otherAppointment == null)
                return false;

            if (otherAppointment.Status == AppointmentStatus.Cancelled ||
                otherAppointment.Status == AppointmentStatus.NoShow)
                return false;

            return otherAppointment.AppointmentEndTime > AppointmentDateTime &&
                   otherAppointment.AppointmentDateTime < AppointmentEndTime;
        }

        public override string ToString()
        {
            return $"Appointment ID: {AppointmentID}, Patient ID: {PatientID}, Doctor ID: {DoctorID}, DateTime: {AppointmentDateTime:yyyy-MM-dd HH:mm}, Status: {Status}, Reason: {ReasonForVisit}";
        }
    }
}
