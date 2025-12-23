/*
HospitalNet – Admin vs Normal Users (SQL Logins + DB Roles + Stored Proc Permissions)

GOAL
- Admin User:
  - Can execute: sp_CreateDoctor, sp_CreatePatient, sp_CreateAppointment
- Normal User:
  - Can execute: sp_CreatePatient, sp_CreateAppointment
  - Cannot execute: sp_CreateDoctor (not granted)

WHAT THIS SCRIPT DOES
1) Creates 2 SQL Server *logins* (server-level): Admin + Normal
2) Creates 2 *database roles* (database-level): HospitalAdminRole + HospitalUserRole
3) Creates 2 *database users* mapped to those logins (in HospitalNet database)
4) Grants EXECUTE on stored procedures to roles
5) Adds users to roles
6) Idempotent where practical (checks before create/add)

PREREQUISITES / NOTES
- To use SQL Username/Password logins, SQL Server must be in Mixed Mode.
  If SERVERPROPERTY('IsIntegratedSecurityOnly') = 1, SQL logins will not be able to authenticate until Mixed Mode is enabled.
- You must run this script using an account with permission to CREATE LOGIN (typically sysadmin) and to ALTER the database security.
- Stored procedures are assumed to be in schema dbo:
  dbo.sp_CreateDoctor, dbo.sp_CreatePatient, dbo.sp_CreateAppointment
  If your schema differs, change dbo. accordingly.

USAGE
- Set the variables in the "CONFIG" section (login names + passwords).
- Run the script in SSMS.
*/

SET NOCOUNT ON;

--------------------------------------------
-- CONFIG (edit these)
--------------------------------------------
DECLARE @DatabaseName sysname = N'HospitalNet';

DECLARE @AdminLogin sysname  = N'adminOmer';
DECLARE @AdminPassword nvarchar(128) = N'1234';

DECLARE @UserLogin sysname   = N'normalUser';
DECLARE @UserPassword nvarchar(128) = N'1234';

-- Database role names (DB roles, not server roles)
DECLARE @AdminRole sysname = N'HospitalAdminRole';
DECLARE @UserRole  sysname = N'HospitalUserRole';

--------------------------------------------
-- Safety checks
--------------------------------------------
IF DB_ID(@DatabaseName) IS NULL
BEGIN
    THROW 50000, 'Database does not exist. Update @DatabaseName and try again.', 1;
END;

DECLARE @IsWindowsOnly int = TRY_CAST(SERVERPROPERTY('IsIntegratedSecurityOnly') AS int);
IF @IsWindowsOnly = 1
BEGIN
    PRINT 'WARNING: SERVERPROPERTY(''IsIntegratedSecurityOnly'') = 1 (Windows Authentication only).';
    PRINT '         SQL Username/Password logins will NOT be able to connect until Mixed Mode is enabled.';
END;

--------------------------------------------
-- 1) Create SQL Server Logins (server-level)
--------------------------------------------
DECLARE @sql nvarchar(max);

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @AdminLogin)
BEGIN
    SET @sql =
        N'CREATE LOGIN ' + QUOTENAME(@AdminLogin) + N'
          WITH PASSWORD = ' + QUOTENAME(REPLACE(@AdminPassword, '''', ''''''), '''') + N',
               CHECK_POLICY = ON,
               CHECK_EXPIRATION = OFF;';
    EXEC (@sql);
    PRINT 'Created server login: ' + @AdminLogin;
END
ELSE
BEGIN
    PRINT 'Server login already exists: ' + @AdminLogin;
END;

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @UserLogin)
BEGIN
    SET @sql =
        N'CREATE LOGIN ' + QUOTENAME(@UserLogin) + N'
          WITH PASSWORD = ' + QUOTENAME(REPLACE(@UserPassword, '''', ''''''), '''') + N',
               CHECK_POLICY = ON,
               CHECK_EXPIRATION = OFF;';
    EXEC (@sql);
    PRINT 'Created server login: ' + @UserLogin;
END
ELSE
BEGIN
    PRINT 'Server login already exists: ' + @UserLogin;
END;

--------------------------------------------
-- 2) Switch to target database
--------------------------------------------
SET @sql = N'USE ' + QUOTENAME(@DatabaseName) + N';';
EXEC (@sql);

--------------------------------------------
-- 3) Create database roles (DB roles)
--------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @AdminRole AND type = 'R')
BEGIN
    SET @sql = N'CREATE ROLE ' + QUOTENAME(@AdminRole) + N';';
    EXEC (@sql);
    PRINT 'Created database role: ' + @AdminRole;
END
ELSE
BEGIN
    PRINT 'Database role already exists: ' + @AdminRole;
END;

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @UserRole AND type = 'R')
BEGIN
    SET @sql = N'CREATE ROLE ' + QUOTENAME(@UserRole) + N';';
    EXEC (@sql);
    PRINT 'Created database role: ' + @UserRole;
END
ELSE
BEGIN
    PRINT 'Database role already exists: ' + @UserRole;
END;

--------------------------------------------
-- 4) Map logins to database users
--    (User name = Login name; adjust if desired)
--------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @AdminLogin)
BEGIN
    SET @sql = N'CREATE USER ' + QUOTENAME(@AdminLogin) + N' FOR LOGIN ' + QUOTENAME(@AdminLogin) + N';';
    EXEC (@sql);
    PRINT 'Created database user: ' + @AdminLogin;
END
ELSE
BEGIN
    PRINT 'Database user already exists: ' + @AdminLogin;
END;

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @UserLogin)
BEGIN
    SET @sql = N'CREATE USER ' + QUOTENAME(@UserLogin) + N' FOR LOGIN ' + QUOTENAME(@UserLogin) + N';';
    EXEC (@sql);
    PRINT 'Created database user: ' + @UserLogin;
END
ELSE
BEGIN
    PRINT 'Database user already exists: ' + @UserLogin;
END;

--------------------------------------------
-- 5) Add each database user to the correct role
--------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.database_role_members drm
    JOIN sys.database_principals r ON r.principal_id = drm.role_principal_id
    JOIN sys.database_principals u ON u.principal_id = drm.member_principal_id
    WHERE r.name = @AdminRole AND u.name = @AdminLogin
)
BEGIN
    SET @sql = N'ALTER ROLE ' + QUOTENAME(@AdminRole) + N' ADD MEMBER ' + QUOTENAME(@AdminLogin) + N';';
    EXEC (@sql);
    PRINT 'Added user to role: ' + @AdminLogin + ' -> ' + @AdminRole;
END
ELSE
BEGIN
    PRINT 'User already in role: ' + @AdminLogin + ' -> ' + @AdminRole;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.database_role_members drm
    JOIN sys.database_principals r ON r.principal_id = drm.role_principal_id
    JOIN sys.database_principals u ON u.principal_id = drm.member_principal_id
    WHERE r.name = @UserRole AND u.name = @UserLogin
)
BEGIN
    SET @sql = N'ALTER ROLE ' + QUOTENAME(@UserRole) + N' ADD MEMBER ' + QUOTENAME(@UserLogin) + N';';
    EXEC (@sql);
    PRINT 'Added user to role: ' + @UserLogin + ' -> ' + @UserRole;
END
ELSE
BEGIN
    PRINT 'User already in role: ' + @UserLogin + ' -> ' + @UserRole;
END;

--------------------------------------------
-- 6) Assign permissions (GRANT EXECUTE)
--    NOTE: This assumes stored procedures are in dbo schema.
--------------------------------------------

-- Admin role: all 3
GRANT EXECUTE ON OBJECT::dbo.sp_CreateDoctor      TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_CreatePatient     TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_CreateAppointment TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetDoctorByLicenseNumber TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetAllActiveDoctors TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetDoctorSchedule  TO HospitalAdminRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetPatientById  TO HospitalAdminRole;

GRANT ALTER ON OBJECT::dbo.sp_CreateDoctor      TO HospitalAdminRole;
GRANT ALTER ON OBJECT::dbo.sp_CreatePatient     TO HospitalAdminRole;
GRANT ALTER ON OBJECT::dbo.sp_CreateAppointment TO HospitalAdminRole;
GRANT ALTER ON OBJECT::dbo.sp_GetDoctorByLicenseNumber TO HospitalAdminRole;
GRANT ALTER ON OBJECT::dbo.sp_GetAllActiveDoctors TO HospitalAdminRole;
GO
-- Normal role: 
GRANT EXECUTE ON OBJECT::dbo.sp_CreatePatient     TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_CreateAppointment TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetDoctorByLicenseNumber TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetAllActiveDoctors TO HospitalUserRole;
GRANT EXECUTE ON OBJECT::dbo.sp_GetPatientById  TO HospitalUserRole;
DENY  EXECUTE ON OBJECT::dbo.sp_CreateDoctor      TO HospitalUserRole;

GRANT ALTER ON OBJECT::dbo.sp_CreatePatient     TO HospitalUserRole;
GRANT ALTER ON OBJECT::dbo.sp_CreateAppointment TO HospitalUserRole;
GRANT ALTER ON OBJECT::dbo.sp_GetDoctorByLicenseNumber TO HospitalUserRole;
GRANT ALTER ON OBJECT::dbo.sp_GetAllActiveDoctors TO HospitalUserRole;
GO

PRINT 'Done. Logins/users/roles/permissions have been applied.';