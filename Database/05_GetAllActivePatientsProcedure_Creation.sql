USE HospitalNet;
GO
IF OBJECT_ID('dbo.sp_GetAllActivePatients', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetAllActivePatients;
GO

CREATE PROCEDURE dbo.sp_GetAllActivePatients
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        PatientID,
        FirstName,
        LastName,
        DateOfBirth,
        Gender,
        PhoneNumber,
        Email,
        Address,
        City,
        PostalCode,
        InsuranceProviderID,
        MedicalHistorySummary,
        IsActive,
        CreatedDate,
        UpdatedDate,
        LastVisitDate
    FROM dbo.Patients
    WHERE IsActive = 1;
END;
GO
