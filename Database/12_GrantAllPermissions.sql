USE HospitalNet
GO

GRANT EXECUTE ON SCHEMA::dbo TO [HospitalAdminRole];

-- Normal can execute everything under dbo (then we restrict)
GRANT EXECUTE ON SCHEMA::dbo TO [HospitalUserRole];

-- Normal user restrictions (Doctor administration)
DENY EXECUTE ON OBJECT::dbo.sp_CreateDoctor          TO [HospitalUserRole];
DENY EXECUTE ON OBJECT::dbo.sp_UpdateDoctor          TO [HospitalUserRole];
DENY EXECUTE ON OBJECT::dbo.sp_DeleteDoctor          TO [HospitalUserRole];
DENY EXECUTE ON OBJECT::dbo.sp_SetDoctorActiveStatus TO [HospitalUserRole];
GO