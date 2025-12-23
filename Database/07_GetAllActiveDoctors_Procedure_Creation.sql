USE [HospitalNet];
GO

-- Create stub if it doesn't exist (for older SQL Server)
IF OBJECT_ID(N'dbo.sp_GetAllActiveDoctors', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.sp_GetAllActiveDoctors AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.sp_GetAllActiveDoctors
AS
BEGIN
    SET NOCOUNT ON;

    -- If dbo.Doctors table exists
    IF OBJECT_ID(N'dbo.Doctors', N'U') IS NOT NULL
    BEGIN
        SELECT
            d.DoctorID,
            d.FirstName,
            d.LastName,
            d.Specialization,
            d.LicenseNumber,
            d.PhoneNumber,
            d.Email,
            d.OfficeLocation,
            d.YearsOfExperience,
            d.MaxPatientCapacityPerDay,
            d.Salary,
            d.IsActive,
            d.CreatedDate,
            d.UpdatedDate
        FROM dbo.Doctors AS d
        WHERE d.IsActive = 1;

        RETURN;
    END

    -- If dbo.Doctor table exists
    IF OBJECT_ID(N'dbo.Doctor', N'U') IS NOT NULL
    BEGIN
        SELECT
            d.DoctorID,
            d.FirstName,
            d.LastName,
            d.Specialization,
            d.LicenseNumber,
            d.PhoneNumber,
            d.Email,
            d.OfficeLocation,
            d.YearsOfExperience,
            d.MaxPatientCapacityPerDay,
            d.Salary,
            d.IsActive,
            d.CreatedDate,
            d.UpdatedDate
        FROM dbo.Doctor AS d
        WHERE d.IsActive = 1;

        RETURN;
    END
END;
GO
