USE [HospitalNet]
GO
/****** Object:  StoredProcedure [dbo].[sp_CreateDoctor]    Script Date: 22/12/2025 22:58:04 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_CreateDoctor]  
    @FirstName NVARCHAR(100),  
    @LastName NVARCHAR(100),  
    @Specialization NVARCHAR(100),  
    @LicenseNumber NVARCHAR(100),  
    @PhoneNumber NVARCHAR(50),  
    @Email NVARCHAR(255),  
    @OfficeLocation NVARCHAR(255),  
    @YearsOfExperience INT = 0,  
    @MaxPatientCapacityPerDay INT = 0,  
    @Salary DECIMAL(18,2) = 0,  
    @IsActive BIT = 1,  
    @DoctorID INT OUTPUT  
AS  
BEGIN  
    SET NOCOUNT ON;  
    INSERT INTO dbo.Doctors(FirstName,LastName,Specialization,LicenseNumber,PhoneNumber,Email,OfficeLocation,YearsOfExperience,MaxPatientCapacityPerDay,Salary,IsActive)  
    VALUES(@FirstName,@LastName,@Specialization,@LicenseNumber,@PhoneNumber,@Email,@OfficeLocation,@YearsOfExperience,@MaxPatientCapacityPerDay,@Salary,@IsActive);  
    SET @DoctorID = SCOPE_IDENTITY();  
END  