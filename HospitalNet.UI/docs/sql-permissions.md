# SQL Server (SSMS) – Assigning Admin vs Normal Permissions

This app supports two user types:

- **Admin**: can add doctors, add patients, add appointments
- **Normal**: cannot add doctors, but can add patients and add appointments

The application determines **Admin vs Normal** by checking whether the signed-in SQL/Windows user is a member of the database role `HospitalNetAdmin`.

## Quick setup (recommended)

Run the script in docs/sql-user-setup.sql. It will create:

- Database roles: `HospitalNetAdmin`, `HospitalNetUser`
- Local SQL-auth users for SSMS testing:
  - **Admin**: `hospitalnet_admin`
  - **Normal**: `hospitalnet_user`

It also keeps a compatibility user `hospitalnet_normal` (also Normal).

## Where in SSMS to assign permissions

### 1) Create / map the user

**Server-level login**

- Object Explorer → **Security** → **Logins** → **New Login…**
  - Choose either:
    - **Windows authentication** (recommended for on-prem)
    - **SQL Server authentication** (common for Azure SQL / mixed mode)

**Database user** (maps the login into the database)

- Object Explorer → **Databases** → **HospitalNet** → **Security** → **Users** → **New User…**
  - Map it to the login created above.

### 2) Create database roles

- Object Explorer → **Databases** → **HospitalNet** → **Security** → **Roles** → **Database Roles**
  - Create these roles:
    - `HospitalNetAdmin`
    - `HospitalNetUser`

### 3) Assign users to roles

- Object Explorer → **Databases** → **HospitalNet** → **Security** → **Roles** → **Database Roles**
  - Right-click `HospitalNetAdmin` → **Properties** → **Members** → **Add…**
  - Right-click `HospitalNetUser` → **Properties** → **Members** → **Add…**

The app checks membership of `HospitalNetAdmin`. If the user is not a member, they are treated as **Normal**.

## Recommended permission model (stored procedure EXECUTE)

HospitalNet uses stored procedures for writes. To enforce permissions at the database level:

- Admin: allowed to execute doctor/patient/appointment create procedures
- Normal: allowed to execute patient/appointment create procedures, but **denied** executing the doctor create procedure

### Stored procedures used by the UI

- Add doctor: `sp_CreateDoctor`
- Add patient: `sp_CreatePatient`
- Add appointment: `sp_CreateAppointment`

## T‑SQL (copy/paste) – Create roles and grant/deny

Run this in SSMS (or just run docs/sql-user-setup.sql):

```sql
USE [HospitalNet];
GO

-- 1) Create roles (if they do not exist)
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'HospitalNetAdmin' AND type = 'R')
    CREATE ROLE [HospitalNetAdmin];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'HospitalNetUser' AND type = 'R')
    CREATE ROLE [HospitalNetUser];
GO

-- 2) Grant permissions
GRANT EXECUTE ON OBJECT::dbo.sp_CreatePatient TO [HospitalNetUser];
GRANT EXECUTE ON OBJECT::dbo.sp_CreateAppointment TO [HospitalNetUser];

GRANT EXECUTE ON OBJECT::dbo.sp_CreateDoctor TO [HospitalNetAdmin];
GRANT EXECUTE ON OBJECT::dbo.sp_CreatePatient TO [HospitalNetAdmin];
GRANT EXECUTE ON OBJECT::dbo.sp_CreateAppointment TO [HospitalNetAdmin];

-- 3) Explicitly deny adding doctors to normal users
DENY EXECUTE ON OBJECT::dbo.sp_CreateDoctor TO [HospitalNetUser];
GO

-- 4) Add a database user to a role (example)
 ALTER ROLE [HospitalNetUser] ADD MEMBER [YourDbUserName];
ALTER ROLE [HospitalNetAdmin] ADD MEMBER [YourDbUserName];
```

Notes:
- Replace `[HospitalNet]` with your database name if different.
- For Azure SQL, you typically create the database user directly and then add it to roles.
- If your stored procedures are not in `dbo`, update the schema in the `OBJECT::dbo.` prefix.
