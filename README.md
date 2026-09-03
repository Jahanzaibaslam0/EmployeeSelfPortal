# HRMS Web Forms Project

ASP.NET Web Forms copy of the HRMS application (no MVC / Razor).

## Project location
- **Web app:** `D:\Project\HRMS`
- **Database scripts:** `D:\Project\DATA`

## Setup

### 1. Database
Run on SQL Server (LocalDB or full instance):
```
D:\Project\DATA\Script.sql
D:\Project\DATA\UserSecurity_Script.sql
```

`Web.config` connection string points to:
```
AttachDbFilename=D:\Project\DATA\HRMSDB.mdf
Data Source=(LocalDB)\MSSQLLocalDB
```

Change `Web.config` if using a named SQL Server instance.

### 2. Generate lookup setup pages (if not present)
```powershell
powershell -ExecutionPolicy Bypass -File D:\Project\HRMS\GenerateLookupPages.ps1
```

### 3. Build & run
Open `D:\Project\HRMS\HRMS.csproj` in Visual Studio (.NET Framework 4.8).
Publish to IIS or run with IIS Express.

Default login page: `/Login.aspx`  
Home page: `/Home.aspx`

## Architecture
| Component | Description |
|-----------|-------------|
| `Web.config` | Connection string, session, compilation |
| `Global.asax` | App start, DB seed |
| `AppBase/AppBasePage.cs` | Login + permission checks |
| `AppBase/LookupSetupBasePage.cs` | Shared CRUD for lookup tables |
| `LookupSetup.Master` | Master page for all *Setup pages |
| `Controls/AppHeader.ascx` | Navigation header |
| `Services/` | Auth, permissions, audit, etc. |
| `Pages/` | Original Razor source (reference only, excluded from build) |

## Original source
Copied from: `C:\Users\IT-KHI\Desktop\HRMS`
