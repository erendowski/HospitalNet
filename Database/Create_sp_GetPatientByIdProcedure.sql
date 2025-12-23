USE HospitalNet;
GO

CREATE PROCEDURE dbo.sp_GetPatientById
    @PatientID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        p.PatientID,
        p.FirstName,
        p.LastName,
        p.DateOfBirth,
        p.Gender,
        p.PhoneNumber,
        p.Email,
        p.Address,
        p.City,
        p.PostalCode,
        p.InsuranceProviderID,
        p.MedicalHistorySummary,
        p.Allergies,
        p.IsActive,
        p.CreatedDate,
        p.UpdatedDate
    FROM dbo.Patients AS p
    WHERE p.PatientID = @PatientID;
END
GO
