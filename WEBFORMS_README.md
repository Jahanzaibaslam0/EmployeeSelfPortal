# Target is .NET Framework 4.8 ASPX only — not .NET 8

The UI is classic **ASPX Web Forms** on **.NET Framework 4.8**, not Razor / `dotnet run` / .NET 8.

## Prerequisites

1. Visual Studio 2019/2022 with **ASP.NET and web development**
2. .NET Framework **4.8** Developer Pack
3. SQL Server **LocalDB** (`MSSQLLocalDB`)
4. NuGet restore for `packages.config` (ClosedXML)

## Database

Run once:

`D:\Project\HRMS\Database\SetupLocalDB.bat`

Or apply the SQL scripts under `Database\` / `D:\Project\DATA`.

## Run

**Option A:** Double-click `RUN.bat` (starts IIS Express on port **5080** if installed).

**Option B:** Open `HRMS.csproj` in Visual Studio → F5.

Start URL: http://localhost:5080/Login.aspx  

Login: **admin** / **Admin@123**

## Layout

| Path | Role |
|------|------|
| `*.aspx` + `*.aspx.cs` | Pages |
| `Site.Master` | Main chrome |
| `LookupSetup.Master` | Shared lookup CRUD UI |
| `AppBase\` | Base pages + helpers |
| `Services\` | Auth, permissions, Excel, inventory |
| `Controls\` | Header / footer user controls |
| `css\`, `js\`, `images\` | Static assets (from wwwroot) |
| `Pages\` | Legacy Razor sources (not used at runtime) |
| `Program.cs` | Legacy net8 host (not used) |

## Notes

- Connection string: `Web.config` → `HRMSConnection` (LocalDB / `D:\Project\DATA\HRMSDB.mdf`)
- Schema bootstrap runs in `Global.asax` → `StartupMigrations`
