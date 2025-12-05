using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using HospitalNet.Backend.Infrastructure;

namespace HospitalNet.Backend.BusinessLogic
{
    /// <summary>
    /// Manages analytics and reporting operations for the hospital system.
    /// Provides business intelligence metrics for performance analysis and decision-making.
    /// </summary>
    public class AnalyticsManager
    {
        private readonly DatabaseHelper _dbHelper;

        /// <summary>
        /// Initializes a new instance of the AnalyticsManager class.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        public AnalyticsManager(string connectionString)
        {
            _dbHelper = new DatabaseHelper(connectionString);
        }

        /// <summary>
        /// Retrieves appointment statistics including cancellation rates and metrics.
        /// </summary>
        /// <param name="startDate">The start date for analysis.</param>
        /// <param name="endDate">The end date for analysis.</param>
        /// <returns>Statistics object containing appointment metrics.</returns>
        public AppointmentStatistics GetAppointmentStatistics(DateTime startDate, DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                {
                    throw new ArgumentException("Start date cannot be after end date.");
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@StartDate", startDate),
                    DatabaseHelper.CreateInputParameter("@EndDate", endDate)
                };

                var dataTable = _dbHelper.ExecuteReader("sp_GetAppointmentStatistics", parameters);

                var stats = new AppointmentStatistics
                {
                    StartDate = startDate,
                    EndDate = endDate
                };

                if (dataTable.Rows.Count > 0)
                {
                    var row = dataTable.Rows[0];
                    stats.TotalAppointments = DatabaseHelper.GetIntValue(row, "TotalAppointments");
                    stats.ScheduledAppointments = DatabaseHelper.GetIntValue(row, "ScheduledAppointments");
                    stats.CompletedAppointments = DatabaseHelper.GetIntValue(row, "CompletedAppointments");
                    stats.CancelledAppointments = DatabaseHelper.GetIntValue(row, "CancelledAppointments");
                    stats.NoShowAppointments = DatabaseHelper.GetIntValue(row, "NoShowAppointments");
                    stats.CancellationRate = DatabaseHelper.GetIntValue(row, "CancellationRate");
                    stats.CompletionRate = DatabaseHelper.GetIntValue(row, "CompletionRate");
                }

                return stats;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving appointment statistics: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving appointment statistics: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves doctor performance metrics including patient load and specialization data.
        /// </summary>
        /// <param name="doctorId">Optional: Specific doctor ID. If null, returns all doctors.</param>
        /// <returns>List of doctor performance metrics.</returns>
        public List<DoctorPerformanceMetrics> GetDoctorPerformanceMetrics(int? doctorId = null)
        {
            try
            {
                var parameters = new List<SqlParameter>();
                
                if (doctorId.HasValue)
                {
                    parameters.Add(DatabaseHelper.CreateInputParameter("@DoctorID", doctorId.Value));
                }
                else
                {
                    parameters.Add(DatabaseHelper.CreateInputParameter("@DoctorID", -1)); // -1 means all doctors
                }

                var dataTable = _dbHelper.ExecuteReader("sp_GetDoctorPerformanceMetrics", parameters.ToArray());
                var metrics = new List<DoctorPerformanceMetrics>();

                foreach (DataRow row in dataTable.Rows)
                {
                    var metric = new DoctorPerformanceMetrics
                    {
                        DoctorID = DatabaseHelper.GetIntValue(row, "DoctorID"),
                        FirstName = DatabaseHelper.GetStringValue(row, "FirstName"),
                        LastName = DatabaseHelper.GetStringValue(row, "LastName"),
                        Specialization = DatabaseHelper.GetStringValue(row, "Specialization"),
                        TotalAppointments = DatabaseHelper.GetIntValue(row, "TotalAppointments"),
                        CompletedAppointments = DatabaseHelper.GetIntValue(row, "CompletedAppointments"),
                        CancelledAppointments = DatabaseHelper.GetIntValue(row, "CancelledAppointments"),
                        NoShowAppointments = DatabaseHelper.GetIntValue(row, "NoShowAppointments"),
                        AverageAppointmentDuration = DatabaseHelper.GetIntValue(row, "AverageAppointmentDuration"),
                        TotalPatientsSeen = DatabaseHelper.GetIntValue(row, "TotalPatientsSeen")
                    };

                    metrics.Add(metric);
                }

                return metrics;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving doctor performance metrics: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving doctor performance metrics: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves patient load statistics by time period.
        /// </summary>
        /// <param name="startDate">The start date for analysis.</param>
        /// <param name="endDate">The end date for analysis.</param>
        /// <returns>Statistics object containing patient load metrics.</returns>
        public PatientLoadStatistics GetPatientLoadStatistics(DateTime startDate, DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                {
                    throw new ArgumentException("Start date cannot be after end date.");
                }

                var parameters = new[]
                {
                    DatabaseHelper.CreateInputParameter("@StartDate", startDate),
                    DatabaseHelper.CreateInputParameter("@EndDate", endDate)
                };

                var dataTable = _dbHelper.ExecuteReader("sp_GetPatientLoadStatistics", parameters);

                var stats = new PatientLoadStatistics
                {
                    StartDate = startDate,
                    EndDate = endDate
                };

                if (dataTable.Rows.Count > 0)
                {
                    var row = dataTable.Rows[0];
                    stats.TotalActivePatients = DatabaseHelper.GetIntValue(row, "TotalActivePatients");
                    stats.NewPatientsRegistered = DatabaseHelper.GetIntValue(row, "NewPatientsRegistered");
                    stats.PatientsWithAppointments = DatabaseHelper.GetIntValue(row, "PatientsWithAppointments");
                    stats.AveragePatientsPerDoctor = DatabaseHelper.GetIntValue(row, "AveragePatientsPerDoctor");
                    stats.TotalUniqueVisitors = DatabaseHelper.GetIntValue(row, "TotalUniqueVisitors");
                }

                return stats;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving patient load statistics: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving patient load statistics: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves specialization distribution across doctors.
        /// </summary>
        /// <returns>List of specialization statistics.</returns>
        public List<SpecializationStatistics> GetSpecializationStatistics()
        {
            try
            {
                var dataTable = _dbHelper.ExecuteReader("sp_GetSpecializationStatistics");
                var stats = new List<SpecializationStatistics>();

                foreach (DataRow row in dataTable.Rows)
                {
                    var stat = new SpecializationStatistics
                    {
                        Specialization = DatabaseHelper.GetStringValue(row, "Specialization"),
                        DoctorCount = DatabaseHelper.GetIntValue(row, "DoctorCount"),
                        TotalAppointments = DatabaseHelper.GetIntValue(row, "TotalAppointments"),
                        CompletedAppointments = DatabaseHelper.GetIntValue(row, "CompletedAppointments"),
                        AveragePatientsPerDoctor = DatabaseHelper.GetIntValue(row, "AveragePatientsPerDoctor")
                    };

                    stats.Add(stat);
                }

                return stats;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving specialization statistics: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving specialization statistics: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves peak appointment times.
        /// </summary>
        /// <returns>List of hourly appointment statistics.</returns>
        public List<HourlyAppointmentStatistics> GetPeakAppointmentTimes()
        {
            try
            {
                var dataTable = _dbHelper.ExecuteReader("sp_GetPeakAppointmentTimes");
                var stats = new List<HourlyAppointmentStatistics>();

                foreach (DataRow row in dataTable.Rows)
                {
                    var stat = new HourlyAppointmentStatistics
                    {
                        Hour = DatabaseHelper.GetIntValue(row, "Hour"),
                        AppointmentCount = DatabaseHelper.GetIntValue(row, "AppointmentCount"),
                        DoctorCount = DatabaseHelper.GetIntValue(row, "DoctorCount"),
                        AveragePatients = DatabaseHelper.GetIntValue(row, "AveragePatients")
                    };

                    stats.Add(stat);
                }

                return stats;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Database error while retrieving peak appointment times: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving peak appointment times: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generates a comprehensive performance report for a date range.
        /// </summary>
        /// <param name="startDate">The start date for the report.</param>
        /// <param name="endDate">The end date for the report.</param>
        /// <returns>Comprehensive performance report object.</returns>
        public PerformanceReport GeneratePerformanceReport(DateTime startDate, DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                {
                    throw new ArgumentException("Start date cannot be after end date.");
                }

                var report = new PerformanceReport
                {
                    ReportDate = DateTime.Now,
                    StartDate = startDate,
                    EndDate = endDate,
                    AppointmentStats = GetAppointmentStatistics(startDate, endDate),
                    PatientLoadStats = GetPatientLoadStatistics(startDate, endDate),
                    DoctorMetrics = GetDoctorPerformanceMetrics(),
                    SpecializationStats = GetSpecializationStatistics(),
                    PeakTimes = GetPeakAppointmentTimes()
                };

                return report;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating performance report: {ex.Message}", ex);
            }
        }
    }

    #region Statistics and Metrics Classes

    /// <summary>
    /// Represents appointment statistics for a given time period.
    /// </summary>
    public class AppointmentStatistics
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalAppointments { get; set; }
        public int ScheduledAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public int NoShowAppointments { get; set; }
        public int CancellationRate { get; set; } // Percentage
        public int CompletionRate { get; set; } // Percentage
    }

    /// <summary>
    /// Represents a doctor's performance metrics.
    /// </summary>
    public class DoctorPerformanceMetrics
    {
        public int DoctorID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Specialization { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public int NoShowAppointments { get; set; }
        public int AverageAppointmentDuration { get; set; }
        public int TotalPatientsSeen { get; set; }

        public string FullName => $"{FirstName} {LastName}";

        public int CompletionRate => TotalAppointments > 0
            ? (CompletedAppointments * 100) / TotalAppointments
            : 0;

        public int CancellationRate => TotalAppointments > 0
            ? (CancelledAppointments * 100) / TotalAppointments
            : 0;
    }

    /// <summary>
    /// Represents patient load statistics for a given time period.
    /// </summary>
    public class PatientLoadStatistics
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalActivePatients { get; set; }
        public int NewPatientsRegistered { get; set; }
        public int PatientsWithAppointments { get; set; }
        public int AveragePatientsPerDoctor { get; set; }
        public int TotalUniqueVisitors { get; set; }

        public decimal PatientRetentionRate => TotalActivePatients > 0
            ? (decimal)PatientsWithAppointments / TotalActivePatients * 100
            : 0;
    }

    /// <summary>
    /// Represents specialization-level statistics.
    /// </summary>
    public class SpecializationStatistics
    {
        public string Specialization { get; set; }
        public int DoctorCount { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int AveragePatientsPerDoctor { get; set; }

        public int CompletionRate => TotalAppointments > 0
            ? (CompletedAppointments * 100) / TotalAppointments
            : 0;
    }

    /// <summary>
    /// Represents hourly appointment statistics for peak time analysis.
    /// </summary>
    public class HourlyAppointmentStatistics
    {
        public int Hour { get; set; }
        public int AppointmentCount { get; set; }
        public int DoctorCount { get; set; }
        public int AveragePatients { get; set; }

        public string TimeRange => $"{Hour:D2}:00 - {Hour:D2}:59";
    }

    /// <summary>
    /// Represents a comprehensive performance report.
    /// </summary>
    public class PerformanceReport
    {
        public DateTime ReportDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public AppointmentStatistics AppointmentStats { get; set; }
        public PatientLoadStatistics PatientLoadStats { get; set; }
        public List<DoctorPerformanceMetrics> DoctorMetrics { get; set; }
        public List<SpecializationStatistics> SpecializationStats { get; set; }
        public List<HourlyAppointmentStatistics> PeakTimes { get; set; }

        public string GetSummary()
        {
            return $"Performance Report: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}\n" +
                   $"Total Appointments: {AppointmentStats.TotalAppointments}\n" +
                   $"Completion Rate: {AppointmentStats.CompletionRate}%\n" +
                   $"Cancellation Rate: {AppointmentStats.CancellationRate}%\n" +
                   $"Total Active Patients: {PatientLoadStats.TotalActivePatients}\n" +
                   $"New Patients: {PatientLoadStats.NewPatientsRegistered}\n" +
                   $"Average Patients per Doctor: {PatientLoadStats.AveragePatientsPerDoctor}";
        }
    }

    #endregion
}
