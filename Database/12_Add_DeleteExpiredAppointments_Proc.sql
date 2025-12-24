CREATE OR ALTER PROCEDURE dbo.sp_DeleteExpiredAppointments
    @Now DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Expired AS
    (
        SELECT AppointmentID
        FROM dbo.Appointments
        WHERE DATEADD(minute, DurationMinutes, AppointmentDateTime) < @Now
          AND Status IN (N'Scheduled', N'Cancelled')
    )
    UPDATE dbo.MedicalRecords
    SET AppointmentID = NULL,
        UpdatedDate = sysdatetime()
    WHERE AppointmentID IN (SELECT AppointmentID FROM Expired);

    DELETE FROM dbo.Appointments
    WHERE AppointmentID IN (SELECT AppointmentID FROM Expired);
END
GO

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'HospitalAdminRole')
    GRANT EXECUTE ON OBJECT::dbo.sp_DeleteExpiredAppointments TO HospitalAdminRole;
GO

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'HospitalUserRole')
    GRANT EXECUTE ON OBJECT::dbo.sp_DeleteExpiredAppointments TO HospitalUserRole;
GO
