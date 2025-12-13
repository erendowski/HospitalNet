-- HospitalNet schema and stored procedures
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* Drop existing tables (order matters) */
IF OBJECT_ID('dbo.MedicalRecords', 'U') IS NOT NULL DROP TABLE dbo.MedicalRecords;
IF OBJECT_ID('dbo.Appointments', 'U') IS NOT NULL DROP TABLE dbo.Appointments;
IF OBJECT_ID('dbo.Patients', 'U') IS NOT NULL DROP TABLE dbo.Patients;
IF OBJECT_ID('dbo.Doctors', 'U') IS NOT NULL DROP TABLE dbo.Doctors;
GO

/* Core tables */
CREATE TABLE dbo.Patients
(
    PatientID             INT IDENTITY(1,1) PRIMARY KEY,
    FirstName             NVARCHAR(100) NOT NULL,
    LastName              NVARCHAR(100) NOT NULL,
    DateOfBirth           DATE NULL,
    Gender                NVARCHAR(50) NOT NULL,
    PhoneNumber           NVARCHAR(50) NOT NULL,
    Email                 NVARCHAR(255) NOT NULL,
    Address               NVARCHAR(255) NOT NULL,
    City                  NVARCHAR(100) NOT NULL,
    PostalCode            NVARCHAR(20) NOT NULL,
    InsuranceProviderID   INT NOT NULL DEFAULT(0),
    MedicalHistorySummary NVARCHAR(MAX) NOT NULL DEFAULT(''),
    Allergies             NVARCHAR(MAX) NOT NULL DEFAULT(''),
    LastVisitDate         DATETIME2 NULL,
    IsActive              BIT NOT NULL DEFAULT(1),
    CreatedDate           DATETIME2 NOT NULL DEFAULT(sysdatetime()),
    UpdatedDate           DATETIME2 NOT NULL DEFAULT(sysdatetime())
);
GO

CREATE TABLE dbo.Doctors
(
    DoctorID               INT IDENTITY(1,1) PRIMARY KEY,
    FirstName              NVARCHAR(100) NOT NULL,
    LastName               NVARCHAR(100) NOT NULL,
    Specialization         NVARCHAR(100) NOT NULL,
    LicenseNumber          NVARCHAR(100) NOT NULL,
    PhoneNumber            NVARCHAR(50) NOT NULL,
    Email                  NVARCHAR(255) NOT NULL,
    OfficeLocation         NVARCHAR(255) NOT NULL,
    YearsOfExperience      INT NOT NULL DEFAULT(0),
    MaxPatientCapacityPerDay INT NOT NULL DEFAULT(0),
    IsActive               BIT NOT NULL DEFAULT(1),
    CreatedDate            DATETIME2 NOT NULL DEFAULT(sysdatetime()),
    UpdatedDate            DATETIME2 NOT NULL DEFAULT(sysdatetime())
);
GO

CREATE TABLE dbo.Appointments
(
    AppointmentID       INT IDENTITY(1,1) PRIMARY KEY,
    PatientID           INT NOT NULL REFERENCES dbo.Patients(PatientID),
    DoctorID            INT NOT NULL REFERENCES dbo.Doctors(DoctorID),
    AppointmentDateTime DATETIME2 NOT NULL,
    DurationMinutes     INT NOT NULL DEFAULT(30),
    ReasonForVisit      NVARCHAR(500) NOT NULL,
    Status              NVARCHAR(50) NOT NULL DEFAULT('Scheduled'),
    Notes               NVARCHAR(MAX) NOT NULL DEFAULT(''),
    CancellationReason  NVARCHAR(500) NOT NULL DEFAULT(''),
    CancellationDateTime DATETIME2 NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT(sysdatetime()),
    UpdatedDate         DATETIME2 NOT NULL DEFAULT(sysdatetime()),
    CompletedAt         DATETIME2 NULL
);
GO

CREATE TABLE dbo.MedicalRecords
(
    RecordID                 INT IDENTITY(1,1) PRIMARY KEY,
    AppointmentID            INT NULL REFERENCES dbo.Appointments(AppointmentID),
    PatientID                INT NOT NULL REFERENCES dbo.Patients(PatientID),
    DoctorID                 INT NOT NULL REFERENCES dbo.Doctors(DoctorID),
    VisitDate                DATETIME2 NOT NULL DEFAULT(sysdatetime()),
    ClinicalNotes            NVARCHAR(MAX) NOT NULL DEFAULT(''),
    Diagnosis                NVARCHAR(MAX) NOT NULL DEFAULT(''),
    PrescriptionText         NVARCHAR(MAX) NOT NULL DEFAULT(''),
    AllergiesNotedDuringVisit NVARCHAR(MAX) NOT NULL DEFAULT(''),
    VitalSigns               NVARCHAR(MAX) NOT NULL DEFAULT(''),
    FollowUpNotes            NVARCHAR(MAX) NOT NULL DEFAULT(''),
    FollowUpRequired         BIT NOT NULL DEFAULT(0),
    FollowUpDate             DATETIME2 NULL,
    CreatedDate              DATETIME2 NOT NULL DEFAULT(sysdatetime()),
    UpdatedDate              DATETIME2 NOT NULL DEFAULT(sysdatetime())
);
GO

/* Patients */
CREATE OR ALTER PROCEDURE dbo.sp_CreatePatient
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @DateOfBirth DATE = NULL,
    @Gender NVARCHAR(50),
    @PhoneNumber NVARCHAR(50),
    @Email NVARCHAR(255),
    @Address NVARCHAR(255),
    @City NVARCHAR(100),
    @PostalCode NVARCHAR(20),
    @InsuranceProviderID INT = 0,
    @MedicalHistorySummary NVARCHAR(MAX) = N'',
    @Allergies NVARCHAR(MAX) = N'',
    @IsActive BIT = 1,
    @PatientID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Patients(FirstName,LastName,DateOfBirth,Gender,PhoneNumber,Email,Address,City,PostalCode,InsuranceProviderID,MedicalHistorySummary,Allergies,IsActive)
    VALUES(@FirstName,@LastName,@DateOfBirth,@Gender,@PhoneNumber,@Email,@Address,@City,@PostalCode,@InsuranceProviderID,@MedicalHistorySummary,@Allergies,@IsActive);
    SET @PatientID = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetPatientById @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Patients WHERE PatientID=@PatientId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetAllActivePatients
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Patients WHERE IsActive=1;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_SearchPatientsByName @SearchTerm NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Patients
    WHERE FirstName LIKE '%' + @SearchTerm + '%'
       OR LastName LIKE '%' + @SearchTerm + '%';
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetPatientByPhoneNumber @PhoneNumber NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Patients WHERE PhoneNumber=@PhoneNumber;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetPatientVisitHistory @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.*, d.FirstName + ' ' + d.LastName AS DoctorName
    FROM dbo.Appointments a
    JOIN dbo.Doctors d ON d.DoctorID = a.DoctorID
    WHERE a.PatientID=@PatientId
    ORDER BY a.AppointmentDateTime DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdatePatient
    @PatientId INT,
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @DateOfBirth DATE = NULL,
    @Gender NVARCHAR(50),
    @PhoneNumber NVARCHAR(50),
    @Email NVARCHAR(255),
    @Address NVARCHAR(255),
    @City NVARCHAR(100),
    @PostalCode NVARCHAR(20),
    @InsuranceProviderID INT = 0,
    @MedicalHistorySummary NVARCHAR(MAX) = N'',
    @Allergies NVARCHAR(MAX) = N'',
    @LastVisitDate DATETIME2 = NULL,
    @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Patients
    SET FirstName=@FirstName, LastName=@LastName, DateOfBirth=@DateOfBirth, Gender=@Gender,
        PhoneNumber=@PhoneNumber, Email=@Email, Address=@Address, City=@City,
        PostalCode=@PostalCode, InsuranceProviderID=@InsuranceProviderID, MedicalHistorySummary=@MedicalHistorySummary,
        Allergies=@Allergies, LastVisitDate=@LastVisitDate, IsActive=@IsActive,
        UpdatedDate=sysdatetime()
    WHERE PatientID=@PatientId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_SetPatientActiveStatus
    @PatientId INT,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Patients SET IsActive=@IsActive, UpdatedDate=sysdatetime() WHERE PatientID=@PatientId;
END
GO

/* Doctors */
CREATE OR ALTER PROCEDURE dbo.sp_CreateDoctor
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @Specialization NVARCHAR(100),
    @LicenseNumber NVARCHAR(100),
    @PhoneNumber NVARCHAR(50),
    @Email NVARCHAR(255),
    @OfficeLocation NVARCHAR(255),
    @YearsOfExperience INT = 0,
    @MaxPatientCapacityPerDay INT = 0,
    @IsActive BIT = 1,
    @DoctorID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Doctors(FirstName,LastName,Specialization,LicenseNumber,PhoneNumber,Email,OfficeLocation,YearsOfExperience,MaxPatientCapacityPerDay,IsActive)
    VALUES(@FirstName,@LastName,@Specialization,@LicenseNumber,@PhoneNumber,@Email,@OfficeLocation,@YearsOfExperience,@MaxPatientCapacityPerDay,@IsActive);
    SET @DoctorID = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetAllActiveDoctors
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Doctors WHERE IsActive=1;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetDoctorById @DoctorId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Doctors WHERE DoctorID=@DoctorId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetDoctorsBySpecialization @Specialization NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Doctors WHERE Specialization=@Specialization AND IsActive=1;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetDoctorByLicenseNumber @LicenseNumber NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Doctors WHERE LicenseNumber=@LicenseNumber;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateDoctor
    @DoctorId INT,
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @Specialization NVARCHAR(100),
    @LicenseNumber NVARCHAR(100),
    @PhoneNumber NVARCHAR(50),
    @Email NVARCHAR(255),
    @OfficeLocation NVARCHAR(255),
    @YearsOfExperience INT = 0,
    @MaxPatientCapacityPerDay INT = 0,
    @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Doctors
    SET FirstName=@FirstName, LastName=@LastName, Specialization=@Specialization,
        LicenseNumber=@LicenseNumber, PhoneNumber=@PhoneNumber, Email=@Email,
        OfficeLocation=@OfficeLocation, YearsOfExperience=@YearsOfExperience,
        MaxPatientCapacityPerDay=@MaxPatientCapacityPerDay, IsActive=@IsActive, UpdatedDate=sysdatetime()
    WHERE DoctorID=@DoctorId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetDoctorAppointmentCount @DoctorId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS AppointmentCount FROM dbo.Appointments WHERE DoctorID=@DoctorId;
END
GO

/* Appointments */
CREATE OR ALTER PROCEDURE dbo.sp_CreateAppointment
    @PatientID INT,
    @DoctorID INT,
    @AppointmentDateTime DATETIME2,
    @DurationMinutes INT,
    @ReasonForVisit NVARCHAR(500),
    @Status NVARCHAR(50) = N'Scheduled',
    @Notes NVARCHAR(MAX) = N'',
    @AppointmentID INT OUTPUT,
    @ErrorMessage NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @ErrorMessage = NULL;

    DECLARE @EndTime DATETIME2 = DATEADD(minute, @DurationMinutes, @AppointmentDateTime);

    -- simple overlap check
    IF EXISTS (
        SELECT 1 FROM dbo.Appointments
        WHERE DoctorID = @DoctorID
          AND Status IN (N'Scheduled', N'Completed')
          AND (@AppointmentDateTime < DATEADD(minute, DurationMinutes, AppointmentDateTime))
          AND (@EndTime > AppointmentDateTime)
    )
    BEGIN
        SET @ErrorMessage = N'Doctor already has an appointment in this time range.';
        RETURN;
    END

    INSERT INTO dbo.Appointments(PatientID,DoctorID,AppointmentDateTime,DurationMinutes,ReasonForVisit,Status,Notes)
    VALUES(@PatientID,@DoctorID,@AppointmentDateTime,@DurationMinutes,@ReasonForVisit,@Status,@Notes);
    SET @AppointmentID = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CancelAppointment
    @AppointmentId INT,
    @CancellationReason NVARCHAR(500) = N''
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Appointments
    SET Status = N'Cancelled',
        CancellationReason = @CancellationReason,
        CancellationDateTime = sysdatetime(),
        UpdatedDate = sysdatetime()
    WHERE AppointmentID=@AppointmentId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CheckDoctorAvailability
    @DoctorId INT,
    @StartTime DATETIME2,
    @EndTime DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @conflict INT = (
        SELECT COUNT(*) FROM dbo.Appointments
        WHERE DoctorID=@DoctorId
          AND Status IN (N'Scheduled', N'Completed')
          AND ((@StartTime < DATEADD(minute, DurationMinutes, AppointmentDateTime)) AND (AppointmentDateTime < @EndTime))
    );
    SELECT CASE WHEN @conflict=0 THEN 1 ELSE 0 END AS IsAvailable;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetDoctorSchedule
    @DoctorId INT,
    @StartDate DATETIME2,
    @EndDate DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Appointments
    WHERE DoctorID=@DoctorId AND AppointmentDateTime BETWEEN @StartDate AND @EndDate
    ORDER BY AppointmentDateTime;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetAppointmentById @AppointmentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Appointments WHERE AppointmentID=@AppointmentId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CompleteAppointment
    @AppointmentId INT,
    @Notes NVARCHAR(MAX) = N'',
    @Status NVARCHAR(50) = N'Completed'
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Appointments
    SET Status=@Status, Notes=@Notes, CompletedAt=sysdatetime(), UpdatedDate=sysdatetime()
    WHERE AppointmentID=@AppointmentId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetPatientAppointments @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Appointments WHERE PatientID=@PatientId ORDER BY AppointmentDateTime DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetAvailableTimeSlots
    @DoctorId INT,
    @Date DATE
AS
BEGIN
    SET NOCOUNT ON;
    -- Placeholder: returns empty set. Replace with real slot generation if needed.
    SELECT CAST(NULL AS DATETIME2) AS StartTime, CAST(NULL AS DATETIME2) AS EndTime WHERE 1=0;
END
GO

/* Medical Records */
CREATE OR ALTER PROCEDURE dbo.sp_RecordMedicalVisit
    @AppointmentId INT = NULL,
    @PatientId INT,
    @DoctorId INT,
    @ClinicalNotes NVARCHAR(MAX) = N'',
    @Diagnosis NVARCHAR(MAX) = N'',
    @PrescriptionText NVARCHAR(MAX) = N'',
    @AllergiesNotedDuringVisit NVARCHAR(MAX) = N'',
    @VitalSigns NVARCHAR(MAX) = N'',
    @FollowUpNotes NVARCHAR(MAX) = N'',
    @FollowUpRequired BIT = 0,
    @FollowUpDate DATETIME2 = NULL,
    @NewMedicalRecordId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.MedicalRecords(AppointmentID,PatientID,DoctorID,VisitDate,ClinicalNotes,Diagnosis,PrescriptionText,AllergiesNotedDuringVisit,VitalSigns,FollowUpNotes,FollowUpRequired,FollowUpDate)
    VALUES(@AppointmentId,@PatientId,@DoctorId,sysdatetime(),@ClinicalNotes,@Diagnosis,@PrescriptionText,@AllergiesNotedDuringVisit,@VitalSigns,@FollowUpNotes,@FollowUpRequired,@FollowUpDate);
    SET @NewMedicalRecordId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetMedicalRecordById @MedicalRecordId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.MedicalRecords WHERE RecordID=@MedicalRecordId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetPatientMedicalRecords @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.MedicalRecords WHERE PatientID=@PatientId ORDER BY VisitDate DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetDoctorMedicalRecords @DoctorId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.MedicalRecords WHERE DoctorID=@DoctorId ORDER BY VisitDate DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetMedicalRecordByAppointmentId @AppointmentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.MedicalRecords WHERE AppointmentID=@AppointmentId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetFollowUpRequiredRecords
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.MedicalRecords WHERE FollowUpRequired=1 ORDER BY VisitDate DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetOverdueFollowUps
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.MedicalRecords WHERE FollowUpRequired=1 AND VisitDate < DATEADD(day,-14,sysdatetime()) ORDER BY VisitDate DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateMedicalRecord
    @MedicalRecordId INT,
    @ClinicalNotes NVARCHAR(MAX) = N'',
    @Diagnosis NVARCHAR(MAX) = N'',
    @PrescriptionText NVARCHAR(MAX) = N'',
    @AllergiesNotedDuringVisit NVARCHAR(MAX) = N'',
    @VitalSigns NVARCHAR(MAX) = N'',
    @FollowUpNotes NVARCHAR(MAX) = N'',
    @FollowUpRequired BIT = 0,
    @FollowUpDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.MedicalRecords
    SET ClinicalNotes=@ClinicalNotes, Diagnosis=@Diagnosis, PrescriptionText=@PrescriptionText,
        AllergiesNotedDuringVisit=@AllergiesNotedDuringVisit, VitalSigns=@VitalSigns,
        FollowUpNotes=@FollowUpNotes, FollowUpRequired=@FollowUpRequired, FollowUpDate=@FollowUpDate,
        UpdatedDate=sysdatetime()
    WHERE RecordID=@MedicalRecordId;
END
GO

/* Analytics */
CREATE OR ALTER PROCEDURE dbo.sp_GetAppointmentStatistics
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        COUNT(*) AS TotalAppointments,
        SUM(CASE WHEN Status = N'Completed' THEN 1 ELSE 0 END) AS CompletedAppointments,
        SUM(CASE WHEN Status = N'Cancelled' THEN 1 ELSE 0 END) AS CancelledAppointments,
        AVG(DurationMinutes) AS AvgDurationMinutes
    FROM dbo.Appointments
    WHERE (@StartDate IS NULL OR AppointmentDateTime >= @StartDate)
      AND (@EndDate IS NULL OR AppointmentDateTime <= @EndDate);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetDoctorPerformanceMetrics
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        d.DoctorID,
        d.FirstName + ' ' + d.LastName AS DoctorName,
        COUNT(a.AppointmentID) AS TotalAppointments,
        SUM(CASE WHEN a.Status = N'Completed' THEN 1 ELSE 0 END) AS CompletedAppointments,
        CASE WHEN COUNT(a.AppointmentID)=0 THEN 0 ELSE 1.0*SUM(CASE WHEN a.Status=N'Completed' THEN 1 ELSE 0 END)/COUNT(a.AppointmentID) END AS CompletionRate,
        AVG(DurationMinutes) AS AvgVisitDuration,
        CAST(NULL AS DECIMAL(5,2)) AS PatientSatisfaction
    FROM dbo.Doctors d
    LEFT JOIN dbo.Appointments a ON a.DoctorID = d.DoctorID
        AND (@StartDate IS NULL OR a.AppointmentDateTime >= @StartDate)
        AND (@EndDate IS NULL OR a.AppointmentDateTime <= @EndDate)
    GROUP BY d.DoctorID, d.FirstName, d.LastName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetPatientLoadStatistics
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        COUNT(*) AS TotalPatients,
        SUM(CASE WHEN IsActive=1 THEN 1 ELSE 0 END) AS ActivePatients
    FROM dbo.Patients;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetSpecializationStatistics
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Specialization, COUNT(*) AS DoctorCount
    FROM dbo.Doctors
    WHERE IsActive=1
    GROUP BY Specialization;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetPeakAppointmentTimes
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DATEPART(hour, AppointmentDateTime) AS HourOfDay, COUNT(*) AS AppointmentCount
    FROM dbo.Appointments
    GROUP BY DATEPART(hour, AppointmentDateTime)
    ORDER BY HourOfDay;
END
GO

/* Dashboard */
CREATE OR ALTER PROCEDURE dbo.sp_GetDashboardMetrics
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TodayStart DATETIME2 = CAST(CONVERT(date, sysdatetime()) AS DATETIME2);
    DECLARE @TodayEnd   DATETIME2 = DATEADD(day, 1, @TodayStart);

    SELECT
        (SELECT COUNT(*) FROM dbo.Patients) AS TotalPatients,
        (SELECT COUNT(*) FROM dbo.Doctors WHERE IsActive = 1) AS ActiveDoctors,
    (SELECT COUNT(*) FROM dbo.Appointments WHERE AppointmentDateTime >= @TodayStart AND AppointmentDateTime < @TodayEnd) AS TodayAppointments,
    (SELECT COUNT(*) FROM dbo.Appointments WHERE Status = N'Completed' AND AppointmentDateTime >= @TodayStart AND AppointmentDateTime < @TodayEnd) AS CompletedToday;
END
GO
