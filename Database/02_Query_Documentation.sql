-- ============================================================
-- HospitalNet - Raw SQL Queries for Critical Operations
-- Phase 1: Supplementary Query Documentation
-- ============================================================

-- ============================================================
-- CRITICAL QUERY 1: Detect if Doctor is Already Booked
-- This is the core query for preventing double-booking
-- Usage: Execute before creating a new appointment
-- ============================================================

/*
This query detects if a doctor has any conflicting appointments
at a specific date and time with a given duration.

Parameters:
- @DoctorID: ID of the doctor
- @AppointmentDateTime: Start time of the requested appointment
- @DurationMinutes: Duration of the appointment (default 30 minutes)

Returns:
- NULL if doctor is available
- AppointmentID if there's a conflict
*/

DECLARE @DoctorID INT = 1;
DECLARE @AppointmentDateTime DATETIME = '2025-12-15 10:00:00';
DECLARE @DurationMinutes INT = 30;
DECLARE @AppointmentEndTime DATETIME = DATEADD(MINUTE, @DurationMinutes, @AppointmentDateTime);

-- Query to check for overlapping appointments
SELECT TOP 1
    a.AppointmentID,
    a.PatientID,
    p.FirstName,
    p.LastName,
    a.AppointmentDateTime,
    DATEADD(MINUTE, a.DurationMinutes, a.AppointmentDateTime) AS AppointmentEndTime,
    a.Status,
    'CONFLICT' AS ConflictStatus
FROM Appointments a
INNER JOIN Patients p ON a.PatientID = p.PatientID
WHERE a.DoctorID = @DoctorID
  AND a.Status NOT IN ('Cancelled', 'No-Show')
  AND DATEADD(MINUTE, a.DurationMinutes, a.AppointmentDateTime) > @AppointmentDateTime
  AND a.AppointmentDateTime < @AppointmentEndTime;

-- ============================================================
-- CRITICAL QUERY 2: Get All Appointments in Time Range for Doctor
-- Useful for viewing doctor's full schedule
-- ============================================================

DECLARE @DoctorID INT = 1;
DECLARE @StartDate DATE = '2025-12-01';
DECLARE @EndDate DATE = '2025-12-31';

SELECT
    a.AppointmentID,
    a.PatientID,
    CONCAT(p.FirstName, ' ', p.LastName) AS PatientFullName,
    a.AppointmentDateTime,
    DATEADD(MINUTE, a.DurationMinutes, a.AppointmentDateTime) AS AppointmentEndTime,
    a.DurationMinutes,
    a.ReasonForVisit,
    a.Status,
    a.Notes
FROM Appointments a
INNER JOIN Patients p ON a.PatientID = p.PatientID
WHERE a.DoctorID = @DoctorID
  AND CAST(a.AppointmentDateTime AS DATE) BETWEEN @StartDate AND @EndDate
  AND a.Status NOT IN ('Cancelled', 'No-Show')
ORDER BY a.AppointmentDateTime ASC;

-- ============================================================
-- ANALYTICS QUERY 1: Calculate Cancellation Rate by Doctor
-- ============================================================

SELECT
    d.DoctorID,
    CONCAT(d.FirstName, ' ', d.LastName) AS DoctorName,
    d.Specialization,
    COUNT(CASE WHEN a.Status = 'Cancelled' THEN 1 END) AS CancelledAppointments,
    COUNT(CASE WHEN a.Status = 'Completed' THEN 1 END) AS CompletedAppointments,
    COUNT(CASE WHEN a.Status = 'No-Show' THEN 1 END) AS NoShowAppointments,
    COUNT(a.AppointmentID) AS TotalAppointments,
    CAST(COUNT(CASE WHEN a.Status = 'Cancelled' THEN 1 END) * 100.0 / 
         NULLIF(COUNT(a.AppointmentID), 0) AS DECIMAL(5,2)) AS CancellationRatePercent
FROM Doctors d
LEFT JOIN Appointments a ON d.DoctorID = a.DoctorID
WHERE a.AppointmentDateTime BETWEEN DATEADD(MONTH, -3, GETDATE()) AND GETDATE()
GROUP BY d.DoctorID, d.FirstName, d.LastName, d.Specialization
ORDER BY CancellationRatePercent DESC;

-- ============================================================
-- ANALYTICS QUERY 2: Patient Load per Doctor (by Date)
-- ============================================================

SELECT
    CAST(a.AppointmentDateTime AS DATE) AS AppointmentDate,
    d.DoctorID,
    CONCAT(d.FirstName, ' ', d.LastName) AS DoctorName,
    d.Specialization,
    COUNT(DISTINCT a.PatientID) AS UniquePatients,
    COUNT(a.AppointmentID) AS TotalAppointments,
    SUM(a.DurationMinutes) AS TotalMinutesBooked
FROM Appointments a
INNER JOIN Doctors d ON a.DoctorID = d.DoctorID
WHERE a.Status NOT IN ('Cancelled', 'No-Show')
  AND CAST(a.AppointmentDateTime AS DATE) BETWEEN DATEADD(DAY, -30, GETDATE()) AND GETDATE()
GROUP BY CAST(a.AppointmentDateTime AS DATE), d.DoctorID, d.FirstName, d.LastName, d.Specialization
ORDER BY AppointmentDate DESC, DoctorName ASC;

-- ============================================================
-- ANALYTICS QUERY 3: Completed Appointments Rate
-- Measures how many scheduled appointments were actually completed
-- ============================================================

SELECT
    d.DoctorID,
    CONCAT(d.FirstName, ' ', d.LastName) AS DoctorName,
    d.Specialization,
    COUNT(CASE WHEN a.Status = 'Completed' THEN 1 END) AS CompletedAppointments,
    COUNT(CASE WHEN a.Status = 'Scheduled' THEN 1 END) AS ScheduledAppointments,
    COUNT(CASE WHEN a.Status = 'No-Show' THEN 1 END) AS NoShowAppointments,
    CAST(COUNT(CASE WHEN a.Status = 'Completed' THEN 1 END) * 100.0 / 
         NULLIF(COUNT(a.AppointmentID), 0) AS DECIMAL(5,2)) AS CompletionRatePercent,
    CAST(COUNT(CASE WHEN a.Status = 'No-Show' THEN 1 END) * 100.0 / 
         NULLIF(COUNT(a.AppointmentID), 0) AS DECIMAL(5,2)) AS NoShowRatePercent
FROM Doctors d
LEFT JOIN Appointments a ON d.DoctorID = a.DoctorID
WHERE a.AppointmentDateTime BETWEEN DATEADD(MONTH, -6, GETDATE()) AND GETDATE()
GROUP BY d.DoctorID, d.FirstName, d.LastName, d.Specialization
ORDER BY CompletionRatePercent DESC;

-- ============================================================
-- ANALYTICS QUERY 4: Medical Records Summary
-- Shows patient diagnoses and prescriptions by doctor
-- ============================================================

SELECT
    mr.RecordID,
    CAST(mr.VisitDate AS DATE) AS VisitDate,
    CONCAT(p.FirstName, ' ', p.LastName) AS PatientName,
    CONCAT(d.FirstName, ' ', d.LastName) AS DoctorName,
    d.Specialization,
    mr.Diagnosis,
    mr.PrescriptionText,
    mr.ClinicalNotes,
    CASE WHEN mr.FollowUpRequired = 1 THEN 'Yes' ELSE 'No' END AS FollowUpRequired,
    mr.FollowUpDate
FROM MedicalRecords mr
INNER JOIN Patients p ON mr.PatientID = p.PatientID
INNER JOIN Doctors d ON mr.DoctorID = d.DoctorID
WHERE mr.VisitDate BETWEEN DATEADD(MONTH, -3, GETDATE()) AND GETDATE()
ORDER BY mr.VisitDate DESC;

-- ============================================================
-- ANALYTICS QUERY 5: Doctor Workload Analysis (Hours per Day)
-- ============================================================

SELECT
    CAST(a.AppointmentDateTime AS DATE) AS WorkDate,
    d.DoctorID,
    CONCAT(d.FirstName, ' ', d.LastName) AS DoctorName,
    SUM(a.DurationMinutes) / 60.0 AS HoursWorked,
    COUNT(a.AppointmentID) AS AppointmentCount,
    AVG(a.DurationMinutes) AS AvgAppointmentDuration,
    MIN(a.AppointmentDateTime) AS FirstAppointmentTime,
    MAX(DATEADD(MINUTE, a.DurationMinutes, a.AppointmentDateTime)) AS LastAppointmentTime
FROM Appointments a
INNER JOIN Doctors d ON a.DoctorID = d.DoctorID
WHERE a.Status = 'Completed'
  AND CAST(a.AppointmentDateTime AS DATE) BETWEEN DATEADD(MONTH, -1, GETDATE()) AND GETDATE()
GROUP BY CAST(a.AppointmentDateTime AS DATE), d.DoctorID, d.FirstName, d.LastName
ORDER BY WorkDate DESC, DoctorName ASC;

-- ============================================================
-- ANALYTICS QUERY 6: Patient Visit Frequency (Repeat Patients)
-- ============================================================

SELECT
    p.PatientID,
    CONCAT(p.FirstName, ' ', p.LastName) AS PatientName,
    p.PhoneNumber,
    p.Email,
    COUNT(DISTINCT a.DoctorID) AS DoctorsVisited,
    COUNT(a.AppointmentID) AS TotalAppointments,
    COUNT(CASE WHEN a.Status = 'Completed' THEN 1 END) AS CompletedAppointments,
    MAX(a.AppointmentDateTime) AS LastVisitDate,
    DATEDIFF(DAY, MAX(a.AppointmentDateTime), GETDATE()) AS DaysSinceLastVisit
FROM Patients p
INNER JOIN Appointments a ON p.PatientID = a.PatientID
WHERE a.Status IN ('Completed', 'Scheduled')
GROUP BY p.PatientID, p.FirstName, p.LastName, p.PhoneNumber, p.Email
HAVING COUNT(a.AppointmentID) > 1
ORDER BY TotalAppointments DESC;

-- ============================================================
-- UTILITY QUERY: Active Doctors and Their Appointment Counts
-- ============================================================

SELECT
    d.DoctorID,
    CONCAT(d.FirstName, ' ', d.LastName) AS DoctorName,
    d.Specialization,
    d.OfficeLocation,
    d.YearsOfExperience,
    d.MaxPatientCapacityPerDay,
    COUNT(a.AppointmentID) AS UpcomingAppointments,
    DATEDIFF(DAY, GETDATE(), MIN(CASE WHEN a.Status = 'Scheduled' THEN a.AppointmentDateTime END)) AS DaysUntilNextAppointment
FROM Doctors d
LEFT JOIN Appointments a ON d.DoctorID = a.DoctorID 
    AND a.AppointmentDateTime >= GETDATE() 
    AND a.Status = 'Scheduled'
WHERE d.IsActive = 1
GROUP BY d.DoctorID, d.FirstName, d.LastName, d.Specialization, d.OfficeLocation, 
         d.YearsOfExperience, d.MaxPatientCapacityPerDay
ORDER BY d.FirstName ASC;

-- ============================================================
-- UTILITY QUERY: Check Double-Booking Conflicts in Database
-- Returns any existing overlapping appointments (data integrity check)
-- ============================================================

SELECT
    d.DoctorID,
    CONCAT(d.FirstName, ' ', d.LastName) AS DoctorName,
    a1.AppointmentID AS Appointment1ID,
    a2.AppointmentID AS Appointment2ID,
    a1.AppointmentDateTime AS Appointment1Start,
    DATEADD(MINUTE, a1.DurationMinutes, a1.AppointmentDateTime) AS Appointment1End,
    a2.AppointmentDateTime AS Appointment2Start,
    DATEADD(MINUTE, a2.DurationMinutes, a2.AppointmentDateTime) AS Appointment2End,
    'DOUBLE-BOOKING CONFLICT' AS AlertStatus
FROM Appointments a1
INNER JOIN Appointments a2 ON a1.DoctorID = a2.DoctorID 
    AND a1.AppointmentID < a2.AppointmentID
    AND a1.Status NOT IN ('Cancelled', 'No-Show')
    AND a2.Status NOT IN ('Cancelled', 'No-Show')
INNER JOIN Doctors d ON a1.DoctorID = d.DoctorID
WHERE DATEADD(MINUTE, a1.DurationMinutes, a1.AppointmentDateTime) > a2.AppointmentDateTime
  AND a1.AppointmentDateTime < DATEADD(MINUTE, a2.DurationMinutes, a2.AppointmentDateTime)
ORDER BY d.DoctorID, a1.AppointmentDateTime;
