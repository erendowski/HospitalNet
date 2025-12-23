CREATE OR ALTER PROCEDURE dbo.sp_DeleteDoctor
    @DoctorId INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.MedicalRecords WHERE DoctorID = @DoctorId;
    DELETE FROM dbo.Appointments WHERE DoctorID = @DoctorId;
    DELETE FROM dbo.Doctors WHERE DoctorID = @DoctorId;
END
GO

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'HospitalAdminRole')
    GRANT EXECUTE ON OBJECT::dbo.sp_DeleteDoctor TO HospitalAdminRole;
GO

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'HospitalUserRole')
    GRANT EXECUTE ON OBJECT::dbo.sp_DeleteDoctor TO HospitalUserRole;
GO

