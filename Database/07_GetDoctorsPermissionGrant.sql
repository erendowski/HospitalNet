GRANT EXECUTE ON OBJECT::dbo.sp_CreatePatient     TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_CreateAppointment TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetDoctorByLicenseNumber TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetAllActiveDoctors TO HospitalUserRole;
DENY  EXECUTE ON OBJECT::dbo.sp_CreateDoctor      TO HospitalUserRole;
GO

GRANT EXECUTE ON OBJECT::dbo.sp_CreateDoctor      TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_CreatePatient     TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_CreateAppointment TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetDoctorByLicenseNumber TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetAllActiveDoctors TO HospitalAdminRole;