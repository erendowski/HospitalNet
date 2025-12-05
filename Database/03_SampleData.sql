-- ============================================================
-- HospitalNet - Sample Data for Testing
-- Phase 1: Test Data Population
-- ============================================================

-- Use the HospitalNet database
USE HospitalNet;
GO

-- ============================================================
-- INSERT SAMPLE PATIENTS
-- ============================================================
INSERT INTO Patients (FirstName, LastName, DateOfBirth, Gender, PhoneNumber, Email, Address, City, PostalCode, MedicalHistorySummary, IsActive)
VALUES
('John', 'Smith', '1985-03-15', 'Male', '555-0101', 'john.smith@email.com', '123 Main St', 'New York', '10001', 'Hypertension, Diabetes Type 2', 1),
('Mary', 'Johnson', '1990-07-22', 'Female', '555-0102', 'mary.johnson@email.com', '456 Oak Ave', 'New York', '10002', 'Asthma, Allergies', 1),
('Robert', 'Williams', '1975-11-08', 'Male', '555-0103', 'robert.w@email.com', '789 Pine Rd', 'New York', '10003', 'Previous cardiac event, on medication', 1),
('Jennifer', 'Brown', '1988-05-30', 'Female', '555-0104', 'jen.brown@email.com', '321 Elm St', 'New York', '10004', 'Healthy, routine checkup', 1),
('Michael', 'Davis', '1992-09-12', 'Male', '555-0105', 'mdavis@email.com', '654 Maple Dr', 'New York', '10005', 'Minor back pain', 1),
('Patricia', 'Miller', '1980-01-25', 'Female', '555-0106', 'pmiller@email.com', '987 Cedar Ln', 'New York', '10006', 'Thyroid condition', 1),
('James', 'Wilson', '1978-06-14', 'Male', '555-0107', 'jwilson@email.com', '159 Birch St', 'New York', '10007', 'Sleep apnea', 1),
('Linda', 'Moore', '1995-02-19', 'Female', '555-0108', 'lmoore@email.com', '753 Spruce Ave', 'New York', '10008', 'Healthy, annual checkup', 1);
GO

-- ============================================================
-- INSERT SAMPLE DOCTORS
-- ============================================================
INSERT INTO Doctors (FirstName, LastName, Specialization, LicenseNumber, PhoneNumber, Email, OfficeLocation, YearsOfExperience, MaxPatientCapacityPerDay, IsActive)
VALUES
('David', 'Anderson', 'Cardiology', 'LIC-001', '555-1001', 'dr.anderson@hospital.com', 'Room 101', 15, 12, 1),
('Sarah', 'Thompson', 'Internal Medicine', 'LIC-002', '555-1002', 'dr.thompson@hospital.com', 'Room 102', 12, 15, 1),
('Michael', 'Chen', 'Pediatrics', 'LIC-003', '555-1003', 'dr.chen@hospital.com', 'Room 103', 8, 18, 1),
('Elizabeth', 'Martinez', 'Dermatology', 'LIC-004', '555-1004', 'dr.martinez@hospital.com', 'Room 104', 10, 14, 1),
('James', 'Taylor', 'Orthopedics', 'LIC-005', '555-1005', 'dr.taylor@hospital.com', 'Room 105', 20, 10, 1),
('Amanda', 'Garcia', 'Neurology', 'LIC-006', '555-1006', 'dr.garcia@hospital.com', 'Room 106', 14, 8, 1);
GO

-- ============================================================
-- INSERT SAMPLE APPOINTMENTS
-- ============================================================

-- Get current date and time for scheduling
-- Appointments scheduled for future dates
INSERT INTO Appointments (PatientID, DoctorID, AppointmentDateTime, DurationMinutes, ReasonForVisit, Status, Notes)
VALUES
-- Cardiology appointments with Dr. Anderson
(1, 1, DATEADD(DAY, 5, GETDATE()), 30, 'Follow-up for hypertension', 'Scheduled', 'Routine checkup, bring blood pressure log'),
(3, 1, DATEADD(DAY, 5, GETDATE()) + CONVERT(DATETIME, ' 11:00:00'), 45, 'Cardiac evaluation post-incident', 'Scheduled', 'EKG results available'),
(1, 1, DATEADD(DAY, 10, GETDATE()), 30, 'Blood pressure check', 'Scheduled', NULL),

-- Internal Medicine appointments with Dr. Thompson
(2, 2, DATEADD(DAY, 6, GETDATE()), 30, 'Respiratory assessment', 'Scheduled', 'Asthma control evaluation'),
(4, 2, DATEADD(DAY, 7, GETDATE()), 30, 'Annual physical examination', 'Scheduled', NULL),
(6, 2, DATEADD(DAY, 8, GETDATE()), 30, 'Thyroid function test follow-up', 'Scheduled', 'TSH levels need monitoring'),

-- Pediatrics appointments with Dr. Chen
(8, 3, DATEADD(DAY, 5, GETDATE()) + CONVERT(DATETIME, ' 14:00:00'), 20, 'Well-child checkup', 'Scheduled', 'Vaccinations up to date'),

-- Orthopedics appointment with Dr. Taylor
(5, 5, DATEADD(DAY, 9, GETDATE()), 45, 'Lower back pain evaluation', 'Scheduled', 'Patient reports pain level 6/10'),

-- Dermatology appointments with Dr. Martinez
(2, 4, DATEADD(DAY, 7, GETDATE()) + CONVERT(DATETIME, ' 10:00:00'), 30, 'Skin rash evaluation', 'Scheduled', 'Occurred on arms and chest'),
(4, 4, DATEADD(DAY, 11, GETDATE()), 30, 'Mole check', 'Scheduled', NULL),

-- Neurology appointment with Dr. Garcia
(7, 6, DATEADD(DAY, 6, GETDATE()) + CONVERT(DATETIME, ' 15:30:00'), 45, 'Sleep disorder evaluation', 'Scheduled', 'Symptoms worsening');
GO

-- ============================================================
-- INSERT COMPLETED APPOINTMENTS (for analytics)
-- ============================================================
INSERT INTO Appointments (PatientID, DoctorID, AppointmentDateTime, DurationMinutes, ReasonForVisit, Status, Notes, CreatedDate)
VALUES
-- Past completed appointments
(1, 1, DATEADD(DAY, -30, GETDATE()), 30, 'Routine checkup', 'Completed', 'All vitals normal', DATEADD(DAY, -30, GETDATE())),
(2, 2, DATEADD(DAY, -25, GETDATE()), 30, 'Asthma management', 'Completed', 'Prescribed new inhaler', DATEADD(DAY, -25, GETDATE())),
(3, 1, DATEADD(DAY, -20, GETDATE()), 45, 'Cardiac follow-up', 'Completed', 'EKG normal, continue medication', DATEADD(DAY, -20, GETDATE())),
(4, 2, DATEADD(DAY, -15, GETDATE()), 30, 'Annual physical', 'Completed', 'Good health status', DATEADD(DAY, -15, GETDATE())),
(5, 5, DATEADD(DAY, -10, GETDATE()), 45, 'Back pain consultation', 'Completed', 'Prescribed physical therapy', DATEADD(DAY, -10, GETDATE())),
(6, 2, DATEADD(DAY, -8, GETDATE()), 30, 'Thyroid follow-up', 'Completed', 'TSH levels improving', DATEADD(DAY, -8, GETDATE())),
(1, 1, DATEADD(DAY, -5, GETDATE()), 30, 'Blood pressure monitoring', 'Completed', 'BP stable on current medication', DATEADD(DAY, -5, GETDATE())),
(2, 2, DATEADD(DAY, -3, GETDATE()), 30, 'Follow-up visit', 'Completed', 'Symptoms resolved', DATEADD(DAY, -3, GETDATE()));
GO

-- ============================================================
-- INSERT CANCELLED APPOINTMENTS (for analytics)
-- ============================================================
INSERT INTO Appointments (PatientID, DoctorID, AppointmentDateTime, DurationMinutes, ReasonForVisit, Status, CancellationReason, CancellationDateTime, CreatedDate)
VALUES
(3, 1, DATEADD(DAY, -22, GETDATE()), 30, 'Checkup', 'Cancelled', 'Patient requested reschedule', DATEADD(DAY, -22, GETDATE()), DATEADD(DAY, -25, GETDATE())),
(4, 2, DATEADD(DAY, -18, GETDATE()), 30, 'Follow-up', 'Cancelled', 'Doctor emergency', DATEADD(DAY, -18, GETDATE()), DATEADD(DAY, -20, GETDATE())),
(5, 5, DATEADD(DAY, -12, GETDATE()), 45, 'Consultation', 'Cancelled', 'Patient illness', DATEADD(DAY, -12, GETDATE()), DATEADD(DAY, -15, GETDATE()));
GO

-- ============================================================
-- INSERT NO-SHOW APPOINTMENTS (for analytics)
-- ============================================================
INSERT INTO Appointments (PatientID, DoctorID, AppointmentDateTime, DurationMinutes, ReasonForVisit, Status, Notes, CreatedDate)
VALUES
(6, 2, DATEADD(DAY, -7, GETDATE()), 30, 'Thyroid check', 'No-Show', 'Patient did not appear', DATEADD(DAY, -10, GETDATE())),
(1, 1, DATEADD(DAY, -2, GETDATE()), 30, 'BP check', 'No-Show', 'No notification', DATEADD(DAY, -5, GETDATE()));
GO

-- ============================================================
-- INSERT SAMPLE MEDICAL RECORDS
-- ============================================================

-- Get AppointmentIDs for medical records
DECLARE @ApptID_1 INT = (SELECT TOP 1 AppointmentID FROM Appointments WHERE PatientID = 1 AND Status = 'Completed' ORDER BY AppointmentDateTime DESC);
DECLARE @ApptID_2 INT = (SELECT TOP 1 AppointmentID FROM Appointments WHERE PatientID = 2 AND Status = 'Completed' ORDER BY AppointmentDateTime DESC);
DECLARE @ApptID_3 INT = (SELECT TOP 1 AppointmentID FROM Appointments WHERE PatientID = 3 AND Status = 'Completed' ORDER BY AppointmentDateTime DESC);
DECLARE @ApptID_4 INT = (SELECT TOP 1 AppointmentID FROM Appointments WHERE PatientID = 4 AND Status = 'Completed' ORDER BY AppointmentDateTime DESC);

INSERT INTO MedicalRecords (AppointmentID, PatientID, DoctorID, VisitDate, ClinicalNotes, Diagnosis, PrescriptionText, VitalSigns, FollowUpRequired, FollowUpDate)
SELECT 
    @ApptID_1, 1, 1, CAST(GETDATE() AS DATE),
    'Patient presenting with stable hypertension. On Lisinopril 10mg daily. Blood pressure readings show good control over past month. No new symptoms reported.',
    'Essential Hypertension - Controlled',
    'Lisinopril 10mg once daily - continue current dose. Home BP monitoring recommended. Salt reduction advised.',
    'BP: 128/82, HR: 72, Temp: 98.6F, Weight: 185 lbs',
    1,
    DATEADD(MONTH, 1, GETDATE())
UNION ALL
SELECT 
    @ApptID_2, 2, 2, CAST(DATEADD(DAY, -3, GETDATE()) AS DATE),
    'Patient with asthma presenting for routine follow-up. Current inhaler usage down to 2-3 times per week. No acute exacerbations. Advised on trigger avoidance.',
    'Asthma - Well Controlled',
    'Albuterol inhaler - continue as needed (2-3 times weekly expected)\nFluticasone/Salmeterol combination inhaler - use twice daily',
    'O2 Sat: 98%, HR: 76, Temp: 98.7F, Respiratory Rate: 16',
    0,
    NULL
UNION ALL
SELECT 
    @ApptID_3, 3, 1, CAST(DATEADD(DAY, -20, GETDATE()) AS DATE),
    'Post-cardiac event follow-up. EKG shows normal sinus rhythm. Patient compliant with medications. No chest pain, dyspnea, or palpitations. Tolerating exercise well.',
    'History of Myocardial Infarction - Stable',
    'Aspirin 81mg daily - antiplatelet therapy\nAtenolol 50mg twice daily - beta blocker\nLisinopril 5mg daily - ACE inhibitor\nAtorvastatin 40mg nightly - statin therapy',
    'BP: 126/78, HR: 64, Temp: 98.5F',
    1,
    DATEADD(MONTH, 3, GETDATE())
UNION ALL
SELECT 
    @ApptID_4, 4, 2, CAST(DATEADD(DAY, -15, GETDATE()) AS DATE),
    'Comprehensive annual physical. Overall health status good. Lab work pending. Patient maintaining healthy lifestyle with regular exercise and balanced diet.',
    'Annual Physical Examination - Healthy',
    'Multivitamin once daily\nContinue current lifestyle\nRecommend annual checkup and appropriate age-related screening',
    'BP: 118/76, HR: 70, Temp: 98.4F, Weight: 140 lbs',
    0,
    NULL;

GO

-- ============================================================
-- VERIFY DATA INSERTION
-- ============================================================
PRINT '=== Sample Data Summary ===';
PRINT '';
PRINT 'Total Patients: ' + CAST((SELECT COUNT(*) FROM Patients) AS VARCHAR(10));
PRINT 'Total Doctors: ' + CAST((SELECT COUNT(*) FROM Doctors) AS VARCHAR(10));
PRINT 'Total Appointments: ' + CAST((SELECT COUNT(*) FROM Appointments) AS VARCHAR(10));
PRINT '  - Scheduled: ' + CAST((SELECT COUNT(*) FROM Appointments WHERE Status = 'Scheduled') AS VARCHAR(10));
PRINT '  - Completed: ' + CAST((SELECT COUNT(*) FROM Appointments WHERE Status = 'Completed') AS VARCHAR(10));
PRINT '  - Cancelled: ' + CAST((SELECT COUNT(*) FROM Appointments WHERE Status = 'Cancelled') AS VARCHAR(10));
PRINT '  - No-Show: ' + CAST((SELECT COUNT(*) FROM Appointments WHERE Status = 'No-Show') AS VARCHAR(10));
PRINT 'Total Medical Records: ' + CAST((SELECT COUNT(*) FROM MedicalRecords) AS VARCHAR(10));
PRINT '';
PRINT 'Sample data inserted successfully!';
GO

-- ============================================================
-- SAMPLE QUERIES TO TEST
-- ============================================================

-- 1. View all active doctors with upcoming appointments
SELECT 
    d.DoctorID,
    CONCAT(d.FirstName, ' ', d.LastName) AS DoctorName,
    d.Specialization,
    COUNT(a.AppointmentID) AS UpcomingAppointments
FROM Doctors d
LEFT JOIN Appointments a ON d.DoctorID = a.DoctorID 
    AND a.AppointmentDateTime >= GETDATE() 
    AND a.Status = 'Scheduled'
WHERE d.IsActive = 1
GROUP BY d.DoctorID, d.FirstName, d.LastName, d.Specialization
ORDER BY d.FirstName;

-- 2. View appointment schedule for Dr. Anderson (Cardiologist)
SELECT 
    a.AppointmentID,
    CONCAT(p.FirstName, ' ', p.LastName) AS PatientName,
    a.AppointmentDateTime,
    DATEADD(MINUTE, a.DurationMinutes, a.AppointmentDateTime) AS EndTime,
    a.ReasonForVisit,
    a.Status
FROM Appointments a
INNER JOIN Patients p ON a.PatientID = p.PatientID
WHERE a.DoctorID = 1 AND a.Status IN ('Scheduled', 'Completed')
ORDER BY a.AppointmentDateTime DESC;

-- 3. View patient visit history for John Smith (PatientID = 1)
SELECT 
    a.AppointmentDateTime,
    CONCAT(d.FirstName, ' ', d.LastName) AS DoctorName,
    d.Specialization,
    a.Status,
    mr.Diagnosis,
    mr.PrescriptionText
FROM Appointments a
INNER JOIN Doctors d ON a.DoctorID = d.DoctorID
LEFT JOIN MedicalRecords mr ON a.AppointmentID = mr.AppointmentID
WHERE a.PatientID = 1
ORDER BY a.AppointmentDateTime DESC;
