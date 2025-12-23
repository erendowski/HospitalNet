# HospitalNet

## Quick Setup
1. Prereqs: .NET 8 SDK, SQL Server (Express is fine), SQL Server Management Studio.
2. Database: open `Database/01_HospitalNet_Schema.sql` in SSMS and run it against a database named `HospitalNet`.
3. Connection string: update `HospitalNet.UI/App.config` (`LocalSql`) for your server/instance (the app will prompt for login and use the effective signed-in connection string).
4. Run app: from repo root `dotnet run --project .\HospitalNet.UI\HospitalNet.UI.csproj -c Debug -p:Platform=AnyCPU`.

Notes:
- `OfflineMode` in `HospitalNet.UI/App.xaml.cs` controls DB usage; keep `false` for live DB.
- SQL roles/permissions: see `HospitalNet.UI/docs/sql-permissions.md`.
