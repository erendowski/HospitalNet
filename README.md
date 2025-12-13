# HospitalNet

## Quick Setup
1. Prereqs: .NET 8 SDK, SQL Server (Express is fine), SQL Server Management Studio.
2. Database: open `Database/01_HospitalNet_Schema.sql` in SSMS and run it against a database named `HospitalNet`.
3. Connection string: in `HospitalNet.UI/App.xaml.cs`, set `GetConnectionString()` to your server/instance, e.g. `Server=localhost\SQLEXPRESS;Database=HospitalNet;Trusted_Connection=True;TrustServerCertificate=True;Connection Timeout=5;`.
4. Run app: from repo root `dotnet run --project .\HospitalNet.UI\HospitalNet.UI.csproj -c Debug -p:Platform=AnyCPU`.

Notes:
- `OfflineMode` in `App.xaml.cs` controls DB usage; keep `false` for live DB.
- All required tables/SP’ler setup dosyasında mevcut.
