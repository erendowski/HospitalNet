USE HospitalNet
GO

-- =============================================
-- 1. ADMIN ROLE (HospitalAdminRole)
-- Grants full control over all procedures
-- =============================================

-- Doctor Management
GRANT EXECUTE ON OBJECT::dbo.sp_CreateDoctor                TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_UpdateDoctor                TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_DeleteDoctor                TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_SetDoctorActiveStatus       TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetAllActiveDoctors         TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetDoctorById               TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetDoctorByLicenseNumber    TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetDoctorsBySpecialization  TO HospitalAdminRole;

-- Patient Management
GRANT EXECUTE ON OBJECT::dbo.sp_CreatePatient               TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_UpdatePatient               TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_DeletePatient               TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_SetPatientActiveStatus      TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetAllActivePatients        TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetPatientById              TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetPatientByPhoneNumber     TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_SearchPatientsByName        TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetPatientVisitHistory      TO HospitalAdminRole;

-- Appointment Management
GRANT EXECUTE ON OBJECT::dbo.sp_CreateAppointment           TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_CancelAppointment           TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_CompleteAppointment         TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_DeleteAppointment           TO HospitalAdminRole; -- Hard delete
GRANT EXECUTE ON OBJECT::dbo.sp_GetAppointmentById          TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetDoctorSchedule           TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_CheckDoctorAvailability     TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetAvailableTimeSlots       TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetPatientAppointments      TO HospitalAdminRole;

-- Medical Records
GRANT EXECUTE ON OBJECT::dbo.sp_RecordMedicalVisit          TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_UpdateMedicalRecord         TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetMedicalRecordById        TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetMedicalRecordByAppointmentId TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetPatientMedicalRecords    TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetDoctorMedicalRecords     TO HospitalAdminRole;

-- Analytics & Reports (Admin Focused)
GRANT EXECUTE ON OBJECT::dbo.sp_GetDashboardMetrics         TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetAppointmentStatistics    TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetDoctorAppointmentCount   TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetDoctorPerformanceMetrics TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetFollowUpRequiredRecords  TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetOverdueFollowUps         TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetPatientLoadStatistics    TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetPeakAppointmentTimes     TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetSpecializationStatistics TO HospitalAdminRole;

-- Grant ALTER permission (as requested in original snippet)
GRANT ALTER ON SCHEMA::dbo TO HospitalAdminRole; 

GO

-- =============================================
-- 2. USER ROLE (HospitalUserRole)
-- Grants operational access, denies administrative/destructive access
-- =============================================

-- Doctor Management (Read-Only)
GRANT EXECUTE ON OBJECT::dbo.sp_GetAllActiveDoctors         TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetDoctorById               TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetDoctorByLicenseNumber    TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetDoctorsBySpecialization  TO HospitalUserRole;
-- Deny Admin Doctor Ops
DENY  EXECUTE ON OBJECT::dbo.sp_CreateDoctor                TO HospitalUserRole;
DENY  EXECUTE ON OBJECT::dbo.sp_UpdateDoctor                TO HospitalUserRole;
DENY  EXECUTE ON OBJECT::dbo.sp_DeleteDoctor                TO HospitalUserRole;
DENY  EXECUTE ON OBJECT::dbo.sp_SetDoctorActiveStatus       TO HospitalUserRole;

-- Patient Management (Create/Read/Update)
GRANT EXECUTE ON OBJECT::dbo.sp_CreatePatient               TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_UpdatePatient               TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetAllActivePatients        TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetPatientById              TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetPatientByPhoneNumber     TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_SearchPatientsByName        TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetPatientVisitHistory      TO HospitalUserRole;
-- Deny Hard Delete
DENY  EXECUTE ON OBJECT::dbo.sp_DeletePatient               TO HospitalUserRole;
DENY  EXECUTE ON OBJECT::dbo.sp_SetPatientActiveStatus      TO HospitalUserRole;

-- Appointment Management (Schedule/Cancel/Complete)
GRANT EXECUTE ON OBJECT::dbo.sp_CreateAppointment           TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_CancelAppointment           TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_CompleteAppointment         TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetAppointmentById          TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetDoctorSchedule           TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_CheckDoctorAvailability     TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetAvailableTimeSlots       TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetPatientAppointments      TO HospitalUserRole;
-- Deny Hard Delete
DENY  EXECUTE ON OBJECT::dbo.sp_DeleteAppointment           TO HospitalUserRole;

-- Medical Records (Operational)
GRANT EXECUTE ON OBJECT::dbo.sp_RecordMedicalVisit          TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_UpdateMedicalRecord         TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetMedicalRecordById        TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetMedicalRecordByAppointmentId TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetPatientMedicalRecords    TO HospitalUserRole;

-- Dashboard & Basic Metrics (Allowed for User Dashboard)
GRANT EXECUTE ON OBJECT::dbo.sp_GetDashboardMetrics         TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetFollowUpRequiredRecords  TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetOverdueFollowUps         TO HospitalUserRole;

-- Analytics (Restricted/Denied)
DENY  EXECUTE ON OBJECT::dbo.sp_GetAppointmentStatistics    TO HospitalUserRole;
DENY  EXECUTE ON OBJECT::dbo.sp_GetDoctorAppointmentCount   TO HospitalUserRole;
DENY  EXECUTE ON OBJECT::dbo.sp_GetDoctorPerformanceMetrics TO HospitalUserRole;
DENY  EXECUTE ON OBJECT::dbo.sp_GetPatientLoadStatistics    TO HospitalUserRole;
DENY  EXECUTE ON OBJECT::dbo.sp_GetPeakAppointmentTimes     TO HospitalUserRole;
DENY  EXECUTE ON OBJECT::dbo.sp_GetSpecializationStatistics TO HospitalUserRole;
GO
