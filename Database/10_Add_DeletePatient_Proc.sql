CREATE OR ALTER PROCEDURE dbo.sp_DeletePatient
    @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.MedicalRecords WHERE PatientID = @PatientId;
    DELETE FROM dbo.Appointments WHERE PatientID = @PatientId;
    DELETE FROM dbo.Patients WHERE PatientID = @PatientId;
END
GO

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'HospitalAdminRole')
    GRANT EXECUTE ON OBJECT::dbo.sp_DeletePatient TO HospitalAdminRole;
GO

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'HospitalUserRole')
    GRANT EXECUTE ON OBJECT::dbo.sp_DeletePatient TO HospitalUserRole;
GO

