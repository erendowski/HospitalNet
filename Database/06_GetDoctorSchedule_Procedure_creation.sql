USE HospitalNet;
GO

IF OBJECT_ID(N'dbo.sp_GetDoctorSchedule', N'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.sp_GetDoctorSchedule AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.sp_GetDoctorSchedule
    @DoctorID INT,
    @StartDate DATETIME,
    @EndDate   DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.Appointments', N'U') IS NOT NULL
    BEGIN
        SELECT a.AppointmentID, a.PatientID, a.DoctorID, a.AppointmentDateTime, a.DurationMinutes,
               a.ReasonForVisit, a.Status, a.Notes, a.CreatedDate, a.UpdatedDate
        FROM dbo.Appointments AS a
        WHERE a.DoctorID = @DoctorID
          AND a.AppointmentDateTime >= @StartDate
          AND a.AppointmentDateTime <= @EndDate
        ORDER BY a.AppointmentDateTime ASC;
        RETURN;
    END

    IF OBJECT_ID(N'dbo.Appointment', N'U') IS NOT NULL
    BEGIN
        SELECT a.AppointmentID, a.PatientID, a.DoctorID, a.AppointmentDateTime, a.DurationMinutes,
               a.ReasonForVisit, a.Status, a.Notes, a.CreatedDate, a.UpdatedDate
        FROM dbo.Appointment AS a
        WHERE a.DoctorID = @DoctorID
          AND a.AppointmentDateTime >= @StartDate
          AND a.AppointmentDateTime <= @EndDate
        ORDER BY a.AppointmentDateTime ASC;
        RETURN;
    END

END
GO

