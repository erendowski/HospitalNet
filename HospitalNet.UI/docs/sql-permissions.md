# SQL Server (SSMS) — Admin vs Normal Permissions

This app supports two user types:

- **Admin**: can add doctors, add patients, add appointments
- **Normal**: cannot add doctors, but can add patients and add appointments

The application determines **Admin vs Normal** by checking whether the signed-in SQL/Windows user is a member of one of these database roles:

- `HospitalAdminRole` (recommended, created by the provided `Database/` scripts)
- `HospitalNetAdmin` (legacy/alternate name)
- `HospitalNet Admin` (legacy name with space)

## Quick setup (recommended)

Run `Database/07_Create_Normal_AdminUsers_AssignRoles.sql`. It creates:

- Database roles: `HospitalAdminRole`, `HospitalUserRole`
- Example SQL-auth logins/users (for SSMS testing): Admin + Normal

You can also use Windows logins; just map the login to a database user in `HospitalNet` and add it to the appropriate role.

## Stored procedures used by the UI

- Add doctor: `dbo.sp_CreateDoctor`
- Add patient: `dbo.sp_CreatePatient`
- Add appointment: `dbo.sp_CreateAppointment`

## Minimal T-SQL (copy/paste)

```sql
USE [HospitalNet];
GO

-- 1) Create roles (if they do not exist)
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'HospitalAdminRole' AND type = 'R')
    CREATE ROLE [HospitalAdminRole];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'HospitalUserRole' AND type = 'R')
    CREATE ROLE [HospitalUserRole];
GO

-- 2) Grant permissions
GRANT EXECUTE ON OBJECT::dbo.sp_CreatePatient TO [HospitalUserRole];
GRANT EXECUTE ON OBJECT::dbo.sp_CreateAppointment TO [HospitalUserRole];

GRANT EXECUTE ON OBJECT::dbo.sp_CreateDoctor TO [HospitalAdminRole];
GRANT EXECUTE ON OBJECT::dbo.sp_CreatePatient TO [HospitalAdminRole];
GRANT EXECUTE ON OBJECT::dbo.sp_CreateAppointment TO [HospitalAdminRole];

-- 3) Explicitly deny adding doctors to normal users
DENY EXECUTE ON OBJECT::dbo.sp_CreateDoctor TO [HospitalUserRole];
GO

-- 4) Add a database user to a role (examples)
-- ALTER ROLE [HospitalUserRole] ADD MEMBER [YourDbUserName];
-- ALTER ROLE [HospitalAdminRole] ADD MEMBER [YourDbUserName];
```

Notes:
- Replace `[HospitalNet]` with your database name if different.
- If your stored procedures are not in `dbo`, update the `OBJECT::dbo.` prefix.
