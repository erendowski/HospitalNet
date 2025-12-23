CREATE OR ALTER PROCEDURE dbo.sp_SetDoctorActiveStatus
    @DoctorId INT,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Doctors
    SET IsActive=@IsActive,
        UpdatedDate=sysdatetime()
    WHERE DoctorID=@DoctorId;
END
GO

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'HospitalAdminRole')
    GRANT EXECUTE ON OBJECT::dbo.sp_SetDoctorActiveStatus TO HospitalAdminRole;
GO

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'HospitalUserRole')
    GRANT EXECUTE ON OBJECT::dbo.sp_SetDoctorActiveStatus TO HospitalUserRole;
GO

