USE [HospitalNet]
GO
/****** Object:  StoredProcedure [dbo].[sp_CreateAppointment]    Script Date: 22/12/2025 11:01:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


/* Appointments */
ALTER   PROCEDURE [dbo].[sp_CreateAppointment]
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
