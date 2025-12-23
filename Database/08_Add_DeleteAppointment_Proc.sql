CREATE OR ALTER PROCEDURE dbo.sp_DeleteAppointment
    @AppointmentId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Preserve medical records by removing the (nullable) appointment link.
    UPDATE dbo.MedicalRecords
    SET AppointmentID = NULL,
        UpdatedDate = sysdatetime()
    WHERE AppointmentID = @AppointmentId;

    DELETE FROM dbo.Appointments
    WHERE AppointmentID = @AppointmentId;
END
GO

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'HospitalAdminRole')
    GRANT EXECUTE ON OBJECT::dbo.sp_DeleteAppointment TO HospitalAdminRole;
GO

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'HospitalUserRole')
    GRANT EXECUTE ON OBJECT::dbo.sp_DeleteAppointment TO HospitalUserRole;
GO

