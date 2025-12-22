USE [HospitalNet]
GO
/****** Object:  StoredProcedure [dbo].[sp_CreatePatient]    Script Date: 22/12/2025 22:58:16 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


/* Patients */
ALTER   PROCEDURE [dbo].[sp_CreatePatient]
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @DateOfBirth DATE = NULL,
    @Gender NVARCHAR(50),
    @PhoneNumber NVARCHAR(50),
    @Email NVARCHAR(255),
    @Address NVARCHAR(255),
    @City NVARCHAR(100),
    @PostalCode NVARCHAR(20),
    @InsuranceProviderID INT = 0,
    @MedicalHistorySummary NVARCHAR(MAX) = N'',
    @Allergies NVARCHAR(MAX) = N'',
    @IsActive BIT = 1,
    @PatientID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Patients(FirstName,LastName,DateOfBirth,Gender,PhoneNumber,Email,Address,City,PostalCode,InsuranceProviderID,MedicalHistorySummary,Allergies,IsActive)
    VALUES(@FirstName,@LastName,@DateOfBirth,@Gender,@PhoneNumber,@Email,@Address,@City,@PostalCode,@InsuranceProviderID,@MedicalHistorySummary,@Allergies,@IsActive);
    SET @PatientID = SCOPE_IDENTITY();
END
