USE HospitalNet;
GO
GRANT ALTER ON SCHEMA::dbo TO [adminOmer]; ---required to let Admin execute procedures and alter those procedures that has been used by program.
-- or to role:
-- GRANT ALTER ON SCHEMA::dbo TO [HospitalAdminRole];
GO