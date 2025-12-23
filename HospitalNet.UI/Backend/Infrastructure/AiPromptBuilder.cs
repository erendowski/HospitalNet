using HospitalNet.Backend.BusinessLogic;
using System;
using System.Linq;
using System.Text;

namespace HospitalNet.Backend.Infrastructure
{
    public static class AiPromptBuilder
    {
        public static string BuildAnalyticsPrompt(PerformanceReport report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            var sb = new StringBuilder();

            sb.AppendLine("You are a healthcare operations analyst assistant.");
            sb.AppendLine("Write a concise, professional report for hospital management.");
            sb.AppendLine("Use clear headings and bullet points. Do not invent data.");
            sb.AppendLine("Provide: key findings, anomalies, risks, and 3-6 actionable recommendations.");
            sb.AppendLine("Keep it readable and practical.");
            sb.AppendLine();

            sb.AppendLine("REPORT CONTEXT");
            sb.AppendLine($"- Date range: {report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}");
            sb.AppendLine($"- Generated at: {report.ReportDate:yyyy-MM-dd HH:mm}");
            sb.AppendLine();

            if (report.AppointmentStats != null)
            {
                sb.AppendLine("APPOINTMENTS");
                sb.AppendLine($"- Total: {report.AppointmentStats.TotalAppointments}");
                sb.AppendLine($"- Scheduled: {report.AppointmentStats.ScheduledAppointments}");
                sb.AppendLine($"- Completed: {report.AppointmentStats.CompletedAppointments}");
                sb.AppendLine($"- Cancelled: {report.AppointmentStats.CancelledAppointments}");
                sb.AppendLine($"- No-show: {report.AppointmentStats.NoShowAppointments}");
                sb.AppendLine($"- Cancellation rate (%): {report.AppointmentStats.CancellationRate}");
                sb.AppendLine($"- Completion rate (%): {report.AppointmentStats.CompletionRate}");
                sb.AppendLine();
            }

            if (report.PatientLoadStats != null)
            {
                sb.AppendLine("PATIENT LOAD");
                sb.AppendLine($"- Total active patients: {report.PatientLoadStats.TotalActivePatients}");
                sb.AppendLine($"- New registrations: {report.PatientLoadStats.NewPatientsRegistered}");
                sb.AppendLine($"- Patients with appointments: {report.PatientLoadStats.PatientsWithAppointments}");
                sb.AppendLine($"- Avg patients/doctor: {report.PatientLoadStats.AveragePatientsPerDoctor}");
                sb.AppendLine($"- Retention rate (%): {report.PatientLoadStats.PatientRetentionRate:F1}");
                sb.AppendLine();
            }

            if (report.DoctorMetrics != null && report.DoctorMetrics.Count > 0)
            {
                var top = report.DoctorMetrics
                    .OrderByDescending(m => m.TotalAppointments)
                    .Take(8)
                    .ToList();

                sb.AppendLine("TOP DOCTORS (by total appointments)");
                foreach (var m in top)
                {
                    sb.AppendLine($"- {m.FullName} ({m.Specialization}): total={m.TotalAppointments}, completed={m.CompletedAppointments}, cancelled={m.CancelledAppointments}, no-show={m.NoShowAppointments}, completion%={m.CompletionRate}, avgDur(min)={m.AverageAppointmentDuration}");
                }
                sb.AppendLine();
            }

            if (report.SpecializationStats != null && report.SpecializationStats.Count > 0)
            {
                var top = report.SpecializationStats
                    .OrderByDescending(s => s.DoctorCount)
                    .Take(10)
                    .ToList();

                sb.AppendLine("SPECIALIZATIONS");
                foreach (var s in top)
                {
                    sb.AppendLine($"- {s.Specialization}: doctors={s.DoctorCount}, totalAppointments={s.TotalAppointments}, completed={s.CompletedAppointments}, completion%={s.CompletionRate}, avgPatients/doctor={s.AveragePatientsPerDoctor}");
                }
                sb.AppendLine();
            }

            if (report.PeakTimes != null && report.PeakTimes.Count > 0)
            {
                sb.AppendLine("PEAK TIMES");
                foreach (var p in report.PeakTimes.Take(10))
                {
                    sb.AppendLine($"- {p.TimeRange} — appointments={p.AppointmentCount}, doctors={p.DoctorCount}, avgPatients={p.AveragePatients}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("OUTPUT FORMAT");
            sb.AppendLine("- Title");
            sb.AppendLine("- Executive summary (3-6 bullets)");
            sb.AppendLine("- Detailed insights (bullets)");
            sb.AppendLine("- Recommendations (numbered list)");

            return sb.ToString();
        }
    }
}
