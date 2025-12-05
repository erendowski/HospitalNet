-- ============================================================
-- HospitalNet Database Creation Script
-- MSSQL Server 2019 or Later
-- Phase 1: Database Schema with Stored Procedures
-- ============================================================

-- Drop existing database if it exists (optional for development)
-- IF EXISTS (SELECT * FROM sys.databases WHERE name = 'HospitalNet')
--   DROP DATABASE HospitalNet;

-- Create database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'HospitalNet')
BEGIN
    CREATE DATABASE HospitalNet;
END
GO

-- Use the HospitalNet database
USE HospitalNet;
GO

-- ============================================================
-- TABLE 1: Patients
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Patients' AND type = 'U')
BEGIN
    CREATE TABLE Patients
    (
        PatientID INT PRIMARY KEY IDENTITY(1,1),
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        DateOfBirth DATE NOT NULL,
        Gender NVARCHAR(10) NOT NULL CHECK (Gender IN ('Male', 'Female', 'Other')),
        PhoneNumber NVARCHAR(15) NOT NULL,
        Email NVARCHAR(100) NULL,
        Address NVARCHAR(500) NOT NULL,
        City NVARCHAR(100) NOT NULL,
        PostalCode NVARCHAR(20) NOT NULL,
        InsuranceProviderID NVARCHAR(50) NULL,
        MedicalHistorySummary NVARCHAR(MAX) NULL,
        IsActive BIT DEFAULT 1,
        CreatedDate DATETIME DEFAULT GETDATE(),
        UpdatedDate DATETIME DEFAULT GETDATE(),
        LastVisitDate DATE NULL
    );
    
    CREATE NONCLUSTERED INDEX IX_Patients_PhoneEmail 
    ON Patients(PhoneNumber, Email);
    
    CREATE NONCLUSTERED INDEX IX_Patients_Active 
    ON Patients(IsActive) INCLUDE (FirstName, LastName);
END
GO

-- ============================================================
-- TABLE 2: Doctors
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Doctors' AND type = 'U')
BEGIN
    CREATE TABLE Doctors
    (
        DoctorID INT PRIMARY KEY IDENTITY(1,1),
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Specialization NVARCHAR(100) NOT NULL,
        LicenseNumber NVARCHAR(50) NOT NULL UNIQUE,
        PhoneNumber NVARCHAR(15) NOT NULL,
        Email NVARCHAR(100) NOT NULL UNIQUE,
        OfficeLocation NVARCHAR(200) NOT NULL,
        YearsOfExperience INT NOT NULL CHECK (YearsOfExperience >= 0),
        MaxPatientCapacityPerDay INT DEFAULT 20,
        IsActive BIT DEFAULT 1,
        CreatedDate DATETIME DEFAULT GETDATE(),
        UpdatedDate DATETIME DEFAULT GETDATE()
    );
    
    CREATE NONCLUSTERED INDEX IX_Doctors_Specialization 
    ON Doctors(Specialization) INCLUDE (FirstName, LastName);
    
    CREATE NONCLUSTERED INDEX IX_Doctors_Active 
    ON Doctors(IsActive);
END
GO

-- ============================================================
-- TABLE 3: Appointments
-- CRITICAL: This table enforces the core business rule
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Appointments' AND type = 'U')
BEGIN
    CREATE TABLE Appointments
    (
        AppointmentID INT PRIMARY KEY IDENTITY(1,1),
        PatientID INT NOT NULL,
        DoctorID INT NOT NULL,
        AppointmentDateTime DATETIME NOT NULL,
        DurationMinutes INT DEFAULT 30 CHECK (DurationMinutes > 0),
        ReasonForVisit NVARCHAR(500) NOT NULL,
        Status NVARCHAR(50) DEFAULT 'Scheduled' CHECK (Status IN ('Scheduled', 'Completed', 'Cancelled', 'No-Show')),
        Notes NVARCHAR(MAX) NULL,
        CancellationReason NVARCHAR(500) NULL,
        CancellationDateTime DATETIME NULL,
        CreatedDate DATETIME DEFAULT GETDATE(),
        UpdatedDate DATETIME DEFAULT GETDATE(),
        
        -- Foreign Keys
        CONSTRAINT FK_Appointments_Patient FOREIGN KEY (PatientID)
            REFERENCES Patients(PatientID) ON DELETE CASCADE,
        CONSTRAINT FK_Appointments_Doctor FOREIGN KEY (DoctorID)
            REFERENCES Doctors(DoctorID) ON DELETE RESTRICT,
        
        -- Unique Constraint to prevent double-booking at the same time slot
        -- This ensures a doctor cannot have overlapping appointments
        CONSTRAINT UQ_DoctorTimeSlot UNIQUE (DoctorID, AppointmentDateTime)
    );
    
    CREATE NONCLUSTERED INDEX IX_Appointments_PatientID 
    ON Appointments(PatientID) INCLUDE (AppointmentDateTime, Status);
    
    CREATE NONCLUSTERED INDEX IX_Appointments_DoctorID 
    ON Appointments(DoctorID, AppointmentDateTime) INCLUDE (Status);
    
    CREATE NONCLUSTERED INDEX IX_Appointments_Status 
    ON Appointments(Status, AppointmentDateTime);
    
    CREATE NONCLUSTERED INDEX IX_Appointments_DateTime 
    ON Appointments(AppointmentDateTime) INCLUDE (DoctorID, Status);
END
GO

-- ============================================================
-- TABLE 4: MedicalRecords
-- Stores clinical notes, diagnoses, and prescriptions
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MedicalRecords' AND type = 'U')
BEGIN
    CREATE TABLE MedicalRecords
    (
        RecordID INT PRIMARY KEY IDENTITY(1,1),
        AppointmentID INT NOT NULL,
        PatientID INT NOT NULL,
        DoctorID INT NOT NULL,
        VisitDate DATE NOT NULL,
        ClinicalNotes NVARCHAR(MAX) NOT NULL,
        Diagnosis NVARCHAR(500) NOT NULL,
        PrescriptionText NVARCHAR(MAX) NOT NULL,
        AllergiesNotedDuringVisit NVARCHAR(MAX) NULL,
        VitalSigns NVARCHAR(500) NULL, -- e.g., "BP: 120/80, HR: 72, Temp: 98.6"
        FollowUpRequired BIT DEFAULT 0,
        FollowUpDate DATE NULL,
        CreatedDate DATETIME DEFAULT GETDATE(),
        UpdatedDate DATETIME DEFAULT GETDATE(),
        
        -- Foreign Keys
        CONSTRAINT FK_MedicalRecords_Appointment FOREIGN KEY (AppointmentID)
            REFERENCES Appointments(AppointmentID) ON DELETE CASCADE,
        CONSTRAINT FK_MedicalRecords_Patient FOREIGN KEY (PatientID)
            REFERENCES Patients(PatientID) ON DELETE CASCADE,
        CONSTRAINT FK_MedicalRecords_Doctor FOREIGN KEY (DoctorID)
            REFERENCES Doctors(DoctorID) ON DELETE RESTRICT
    );
    
    CREATE NONCLUSTERED INDEX IX_MedicalRecords_PatientID 
    ON MedicalRecords(PatientID) INCLUDE (VisitDate, Diagnosis);
    
    CREATE NONCLUSTERED INDEX IX_MedicalRecords_DoctorID 
    ON MedicalRecords(DoctorID) INCLUDE (VisitDate);
    
    CREATE NONCLUSTERED INDEX IX_MedicalRecords_VisitDate 
    ON MedicalRecords(VisitDate) INCLUDE (PatientID, DoctorID);
    
    CREATE NONCLUSTERED INDEX IX_MedicalRecords_FollowUp 
    ON MedicalRecords(FollowUpRequired, FollowUpDate) WHERE FollowUpRequired = 1;
END
GO

-- ============================================================
-- STORED PROCEDURE 1: Check for Double-Booking
-- CRITICAL FUNCTION: Validates appointment slot availability
-- ============================================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_CheckDoctorAvailability')
    DROP PROCEDURE sp_CheckDoctorAvailability;
GO

CREATE PROCEDURE sp_CheckDoctorAvailability
    @DoctorID INT,
    @AppointmentDateTime DATETIME,
    @DurationMinutes INT = 30,
    @IsAvailable BIT OUTPUT,
    @ConflictingAppointmentID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @AppointmentEndTime DATETIME;
    SET @AppointmentEndTime = DATEADD(MINUTE, @DurationMinutes, @AppointmentDateTime);
    
    -- Check if there's any appointment that overlaps with the requested time slot
    -- An overlap occurs if:
    -- Existing Start < Requested End AND Existing End > Requested Start
    SELECT TOP 1 
        @ConflictingAppointmentID = AppointmentID
    FROM Appointments
    WHERE DoctorID = @DoctorID
      AND Status NOT IN ('Cancelled', 'No-Show')
      AND DATEADD(MINUTE, DurationMinutes, AppointmentDateTime) > @AppointmentDateTime
      AND AppointmentDateTime < @AppointmentEndTime;
    
    IF @ConflictingAppointmentID IS NOT NULL
    BEGIN
        SET @IsAvailable = 0;
    END
    ELSE
    BEGIN
        SET @IsAvailable = 1;
        SET @ConflictingAppointmentID = NULL;
    END
END
GO

-- ============================================================
-- STORED PROCEDURE 2: Get Doctor Schedule for a Date Range
-- Returns all appointments for a specific doctor within a date range
-- ============================================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetDoctorSchedule')
    DROP PROCEDURE sp_GetDoctorSchedule;
GO

CREATE PROCEDURE sp_GetDoctorSchedule
    @DoctorID INT,
    @StartDate DATE,
    @EndDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT
        a.AppointmentID,
        a.PatientID,
        CONCAT(p.FirstName, ' ', p.LastName) AS PatientName,
        a.AppointmentDateTime,
        a.DurationMinutes,
        a.ReasonForVisit,
        a.Status,
        a.Notes,
        d.FirstName AS DoctorFirstName,
        d.LastName AS DoctorLastName,
        d.Specialization
    FROM Appointments a
    INNER JOIN Patients p ON a.PatientID = p.PatientID
    INNER JOIN Doctors d ON a.DoctorID = d.DoctorID
    WHERE a.DoctorID = @DoctorID
      AND CAST(a.AppointmentDateTime AS DATE) BETWEEN @StartDate AND @EndDate
      AND a.Status NOT IN ('Cancelled', 'No-Show')
    ORDER BY a.AppointmentDateTime ASC;
END
GO

-- ============================================================
-- STORED PROCEDURE 3: Get Patient Visit History
-- Returns all appointments and medical records for a patient
-- ============================================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetPatientVisitHistory')
    DROP PROCEDURE sp_GetPatientVisitHistory;
GO

CREATE PROCEDURE sp_GetPatientVisitHistory
    @PatientID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT
        a.AppointmentID,
        a.AppointmentDateTime,
        d.FirstName AS DoctorFirstName,
        d.LastName AS DoctorLastName,
        d.Specialization,
        a.ReasonForVisit,
        a.Status,
        mr.RecordID,
        mr.ClinicalNotes,
        mr.Diagnosis,
        mr.PrescriptionText,
        mr.VisitDate
    FROM Appointments a
    INNER JOIN Doctors d ON a.DoctorID = d.DoctorID
    LEFT JOIN MedicalRecords mr ON a.AppointmentID = mr.AppointmentID
    WHERE a.PatientID = @PatientID
    ORDER BY a.AppointmentDateTime DESC;
END
GO

-- ============================================================
-- STORED PROCEDURE 4: Create New Appointment (with validation)
-- This procedure prevents double-booking at the database level
-- ============================================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_CreateAppointment')
    DROP PROCEDURE sp_CreateAppointment;
GO

CREATE PROCEDURE sp_CreateAppointment
    @PatientID INT,
    @DoctorID INT,
    @AppointmentDateTime DATETIME,
    @DurationMinutes INT = 30,
    @ReasonForVisit NVARCHAR(500),
    @Notes NVARCHAR(MAX) = NULL,
    @AppointmentID INT OUTPUT,
    @ErrorMessage NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @IsAvailable BIT;
    DECLARE @ConflictingAppointmentID INT;
    
    -- Validate patient exists
    IF NOT EXISTS (SELECT 1 FROM Patients WHERE PatientID = @PatientID)
    BEGIN
        SET @ErrorMessage = 'Patient not found.';
        RETURN 1;
    END
    
    -- Validate doctor exists and is active
    IF NOT EXISTS (SELECT 1 FROM Doctors WHERE DoctorID = @DoctorID AND IsActive = 1)
    BEGIN
        SET @ErrorMessage = 'Doctor not found or is inactive.';
        RETURN 1;
    END
    
    -- Validate appointment is in the future
    IF @AppointmentDateTime <= GETDATE()
    BEGIN
        SET @ErrorMessage = 'Appointment date and time must be in the future.';
        RETURN 1;
    END
    
    -- Check doctor availability
    EXEC sp_CheckDoctorAvailability 
        @DoctorID = @DoctorID,
        @AppointmentDateTime = @AppointmentDateTime,
        @DurationMinutes = @DurationMinutes,
        @IsAvailable = @IsAvailable OUTPUT,
        @ConflictingAppointmentID = @ConflictingAppointmentID OUTPUT;
    
    IF @IsAvailable = 0
    BEGIN
        SET @ErrorMessage = 'Doctor is not available at the requested time. Conflicting appointment ID: ' + CAST(@ConflictingAppointmentID AS NVARCHAR(50));
        RETURN 1;
    END
    
    -- Create the appointment
    BEGIN TRY
        INSERT INTO Appointments (PatientID, DoctorID, AppointmentDateTime, DurationMinutes, ReasonForVisit, Notes, Status)
        VALUES (@PatientID, @DoctorID, @AppointmentDateTime, @DurationMinutes, @ReasonForVisit, @Notes, 'Scheduled');
        
        SET @AppointmentID = SCOPE_IDENTITY();
        SET @ErrorMessage = NULL;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        SET @ErrorMessage = 'Error creating appointment: ' + ERROR_MESSAGE();
        RETURN 1;
    END CATCH
END
GO

-- ============================================================
-- STORED PROCEDURE 5: Record Medical Visit
-- Creates a medical record after an appointment is completed
-- ============================================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_RecordMedicalVisit')
    DROP PROCEDURE sp_RecordMedicalVisit;
GO

CREATE PROCEDURE sp_RecordMedicalVisit
    @AppointmentID INT,
    @ClinicalNotes NVARCHAR(MAX),
    @Diagnosis NVARCHAR(500),
    @PrescriptionText NVARCHAR(MAX),
    @AllergiesNotedDuringVisit NVARCHAR(MAX) = NULL,
    @VitalSigns NVARCHAR(500) = NULL,
    @FollowUpRequired BIT = 0,
    @FollowUpDate DATE = NULL,
    @RecordID INT OUTPUT,
    @ErrorMessage NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @PatientID INT;
    DECLARE @DoctorID INT;
    DECLARE @VisitDate DATE;
    
    -- Get appointment details
    SELECT 
        @PatientID = PatientID,
        @DoctorID = DoctorID,
        @VisitDate = CAST(AppointmentDateTime AS DATE)
    FROM Appointments
    WHERE AppointmentID = @AppointmentID;
    
    -- Validate appointment exists
    IF @PatientID IS NULL
    BEGIN
        SET @ErrorMessage = 'Appointment not found.';
        RETURN 1;
    END
    
    BEGIN TRY
        INSERT INTO MedicalRecords 
            (AppointmentID, PatientID, DoctorID, VisitDate, ClinicalNotes, Diagnosis, PrescriptionText, AllergiesNotedDuringVisit, VitalSigns, FollowUpRequired, FollowUpDate)
        VALUES 
            (@AppointmentID, @PatientID, @DoctorID, @VisitDate, @ClinicalNotes, @Diagnosis, @PrescriptionText, @AllergiesNotedDuringVisit, @VitalSigns, @FollowUpRequired, @FollowUpDate);
        
        SET @RecordID = SCOPE_IDENTITY();
        
        -- Update appointment status to Completed
        UPDATE Appointments
        SET Status = 'Completed', UpdatedDate = GETDATE()
        WHERE AppointmentID = @AppointmentID;
        
        -- Update patient's last visit date
        UPDATE Patients
        SET LastVisitDate = @VisitDate, UpdatedDate = GETDATE()
        WHERE PatientID = @PatientID;
        
        SET @ErrorMessage = NULL;
        RETURN 0;
    END TRY
    BEGIN CATCH
        SET @ErrorMessage = 'Error recording medical visit: ' + ERROR_MESSAGE();
        RETURN 1;
    END CATCH
END
GO

-- ============================================================
-- STORED PROCEDURE 6: Cancel Appointment
-- ============================================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_CancelAppointment')
    DROP PROCEDURE sp_CancelAppointment;
GO

CREATE PROCEDURE sp_CancelAppointment
    @AppointmentID INT,
    @CancellationReason NVARCHAR(500),
    @ErrorMessage NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Validate appointment exists and is not already cancelled/completed
    IF NOT EXISTS (SELECT 1 FROM Appointments WHERE AppointmentID = @AppointmentID AND Status IN ('Scheduled'))
    BEGIN
        SET @ErrorMessage = 'Appointment not found or cannot be cancelled.';
        RETURN 1;
    END
    
    BEGIN TRY
        UPDATE Appointments
        SET Status = 'Cancelled',
            CancellationReason = @CancellationReason,
            CancellationDateTime = GETDATE(),
            UpdatedDate = GETDATE()
        WHERE AppointmentID = @AppointmentID;
        
        SET @ErrorMessage = NULL;
        RETURN 0;
    END TRY
    BEGIN CATCH
        SET @ErrorMessage = 'Error cancelling appointment: ' + ERROR_MESSAGE();
        RETURN 1;
    END CATCH
END
GO

PRINT 'Database schema created successfully!';
