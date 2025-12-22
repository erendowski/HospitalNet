USE HospitalNet;
GO
GRANT EXECUTE ON OBJECT::dbo.sp_GetAllActivePatients TO [HospitalUserRole];
GRANT EXECUTE ON OBJECT::dbo.sp_GetAllActivePatients TO [HospitalAdminRole];