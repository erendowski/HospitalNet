USE HospitalNet;
GO

CREATE PROCEDURE dbo.sp_CheckDoctorAvailability
    @DoctorId  INT,
    @StartTime DATETIME,
    @EndTime   DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    -- Basic validations (optional but helpful)
    IF @DoctorId IS NULL OR @DoctorId <= 0
        RETURN 0;

    IF @StartTime IS NULL OR @EndTime IS NULL OR @EndTime <= @StartTime
        RETURN 0;

    /*
      Conflict rule:
      Existing appointment interval:  [AptStart, AptEnd)
      New requested interval:         [@StartTime, @EndTime)

      Conflict if: AptStart < @EndTime AND AptEnd > @StartTime
    */

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Appointments a
        WHERE a.DoctorID = @DoctorId
          AND (a.Status IS NULL OR a.Status NOT IN ('Cancelled', 'Canceled')) -- adjust if you use other statuses
          AND a.AppointmentDateTime < @EndTime
          AND DATEADD(MINUTE, a.DurationMinutes, a.AppointmentDateTime) > @StartTime
    )
    BEGIN
        SELECT 0; -- Not available
        RETURN;
    END

    SELECT 1; -- Available
END
GO
