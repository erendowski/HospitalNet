USE master;
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'hn_user1')
BEGIN
    CREATE LOGIN [hn_user1]
    WITH PASSWORD = N'1234',
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = OFF;
END;

ALTER LOGIN [hn_user1] WITH DEFAULT_DATABASE = [HospitalNet];
GO

USE HospitalNet;
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'hn_user1')
BEGIN
    CREATE USER [hn_user1] FOR LOGIN [hn_user1];
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members m
    JOIN sys.database_principals r ON m.role_principal_id = r.principal_id
    JOIN sys.database_principals p ON m.member_principal_id = p.principal_id
    WHERE r.name = 'HospitalUserRole' AND p.name = 'hn_user1'
)
BEGIN
    ALTER ROLE [HospitalUserRole] ADD MEMBER [hn_user1];
END;