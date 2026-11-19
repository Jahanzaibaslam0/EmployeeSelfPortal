# Ghazi Brothers HRMS — Technical Documentation

**Document type:** Technical Documentation  
**Application:** Ghazi Brothers Human Resource Management System (HRMS)  
**Audience:** Developers, DevOps, DBAs, system administrators  
**Scope:** Architecture and operations of the existing application (standalone documentation; not part of the runtime)

---

## 1. System Architecture

### 1.1 High-level view

```
┌─────────────────┐     HTTPS/HTTP      ┌──────────────────────────────┐
│  Browser client │ ◄─────────────────► │  IIS / ASP.NET Web Forms     │
└─────────────────┘                     │  (.NET Framework 4.8)         │
                                        │  *.aspx + AppBase + Services │
                                        └───────────┬──────────────────┘
                                                    │
                         ┌──────────────────────────┼──────────────────────────┐
                         ▼                          ▼                          │
              ┌────────────────────┐    ┌─────────────────────────┐            │
              │ HRMSConnection     │    │ AttendanceConnection    │            │
              │ LocalDB / SQL      │    │ GB_attendance_dashboard │ (read-only)│
              │ HRMSDB             │    │ attendance_dashboard    │ (read-only)│
              └────────────────────┘    └─────────────────────────┘            │
                                                                              │
                                        File system (uploads, LMS docs, images)◄┘
```

### 1.2 Architectural style

| Aspect | Choice |
|--------|--------|
| UI paradigm | Classic ASP.NET Web Forms (code-behind / AppBase page classes) |
| Business logic | `Services\` C# classes |
| Shared page behaviour | `AppBase\` base pages and helpers |
| Authorization catalog | `Services\AppForms.cs` form definitions |
| Auth | Custom session authentication (`authentication mode="None"`) |
| Data access | ADO.NET `SqlConnection` / `SqlCommand` |
| Alternate UI folder | `Pages\` Razor content is **reference-only**, not the live runtime |

### 1.3 Request pipeline (authorized page)

1. ASPX page loads; inherits from `AppBasePage` (or a specialized AppBase class).  
2. `OnInit` enforces login and page access via permissions.  
3. Page / AppBase method calls a Service.  
4. Service uses `HRMSConnection` and/or `AttendanceConnection`.  
5. Result binds to Web Forms controls / markup.

---

## 2. Technology Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET Framework **4.8** |
| Web framework | ASP.NET Web Forms |
| Language | C# |
| Database | Microsoft SQL Server / SQL Server **LocalDB** (`MSSQLLocalDB`) for HRMS |
| External DB | SQL Server catalog `GB_attendance_dashboard` (attendance) |
| Hosting | IIS-compatible Web Forms application (`RUN.bat` for local) |
| Client | Modern browsers (HTML/CSS/JS as shipped with pages) |
| Project | `HRMS.csproj` → assembly `HRMS.dll` |

---

## 3. Application Components and Modules

### 3.1 Folder map

| Path | Responsibility |
|------|----------------|
| Root `*.aspx` / `*.aspx.cs` | Live UI pages |
| `AppBase\` | Base pages (`AppBasePage`, Leave/Request/Discipline/Attendance/LMS bases), `SessionHelper` |
| `Services\` | Auth, permissions, AppForms, domain services, audit, Excel, dashboard, bootstrap |
| `Controls\` | Shared controls (e.g. `AppHeader.ascx`) |
| `Database\` | Schema scripts, LocalDB setup, production patches |
| `Pages\` | Razor **reference only** (`Pages\README.md`) |
| `images\` | Branding assets (e.g. `gb-logo.png`) |
| `Documentation\` | Standalone manuals (this folder) |

### 3.2 Core services (selected)

| Service | Role |
|---------|------|
| `AuthService` | Login, session, 2FA pending step, admin insert (no overwrite of existing admin hash) |
| `PasswordHelper` | PBKDF2 hash / verify; password policy |
| `TotpHelper` | RFC 6238 TOTP (HMAC-SHA1, 30s, 6 digits); no NuGet MFA library |
| `AccountSecurityService` | Lockout, recovery questions/codes, 2FA flags, schema ensure |
| `ProfileChangeRequestService` | Own-profile diffs, pending requests, approve/reject apply, inbox |
| `PermissionService` | Form rights checks |
| `EmployeeProfileAccessService` | Profile / Employee Master / My Documents rules |
| `DataAccessScopeService` | Own / restricted / all data scoping |
| `AppForms` | Canonical list of FormKey → path → category |
| `EmployeeRequestService` | Request CRUD, workflow, alerts |
| `DisciplineService` | Discipline cases, findings, audit |
| `AttendanceService` | Read attendance DB; BiometricID enrichment |
| `LmsDocumentService` | Knowledge library access and documents |
| Audit / Dashboard / Excel helpers | Supporting cross-cutting features |

### 3.3 Form catalog

All controllable screens are registered in `Services\AppForms.cs` with categories:

- Transactions  
- Organization Setup  
- Employee Setup  
- Customer Setup  
- Product Setup  
- Security  

Special FormKeys sharing one physical page:

| FormKey | Page | Purpose |
|---------|------|---------|
| `EmployeeRequest` | `/EmployeeRequest.aspx` | Module access / employee actions |
| `EmployeeRequestHR` | same | HR approval stage |
| `EmployeeRequestManagement` | same | Management approval stage |

---

## 4. Database Structure and Data Flow

### 4.1 Connection strings (`Web.config`)

| Name | Typical target | Usage |
|------|----------------|--------|
| `HRMSConnection` | `(LocalDB)\MSSQLLocalDB` + `D:\Project\DATA\HRMSDB.mdf` | Primary application data |
| `AttendanceConnection` | Server `GB-HLPDESK`, catalog `GB_attendance_dashboard` | Read-only attendance |

**Operational note:** Set real attendance credentials via server-specific config or secrets management. Do not embed secrets in client scripts.

### 4.2 Major HRMS table groups

| Domain | Representative tables |
|--------|----------------------|
| Security | `tblUser` (including 2FA/lockout columns), `tblUserRecoveryCode`, `tblUserSecurityQuestion`, `tblAuthThrottle`, `tblAppForm`, `tblUserPermission`, `tblUserDataScope`, `tblUserDepartmentScope`, `tblUserLocationScope`, `tblAuditLog` |
| Employee | `tblEmployee` (includes `BiometricID`, work arrangement, attendance method, convenience allowance as present), child tables (contacts, address, identification, family, bank, education, documents, certificates) |
| Profile workflow | `tblProfileChangeRequest`, `tblProfileChangeLine`, `tblProfileChangeHistory`, `tblAppInbox` |
| Organization | Division, Department, Location, Region, Job, Position, etc. |
| Leave | `tblLeaveApplication`, `tblLeaveCategory` |
| Requests | `tblEmployeeRequest`, `tblEmployeeRequestAttachment`, `tblEmployeeRequestAction`, `tblEmployeeRequestAlert` |
| Discipline | `tblEmployeeDiscipline`, attachments, audit, findings |
| LMS | `tblLmsDocument`, `tblLmsDocumentAccess` |
| Communication | `tblNotification`, `tblMemorandum`, `tblGalleryImage` |
| Other HR | Performance, Training, Recruitment, Expense tables as present in schema |
| Links | `tblSoftwareLink` (Quick Links) |

### 4.3 Scripts and patches

| Artifact | Purpose |
|----------|---------|
| `Database\SetupLocalDB.bat` / `LocalDB_*.sql` | LocalDB create and seed |
| `Database\Script.sql` | Core schema |
| `Database\UserSecurity_Script.sql` | Security objects |
| `Database\HRMSDB_FullRegeneration.sql` | Full rebuild option |
| `Database\Production_*.sql` | Incremental production patches |

**Production patches present in repo (examples):**

- `Production_EmployeeBiometric_Patch.sql`  
- `Production_EmployeeRequest_Patch.sql`  
- `Production_EmployeeDiscipline_Patch.sql`  
- `Production_LeaveApplication_Patch.sql`  
- `Production_LmsDocuments_Patch.sql`  
- `Production_EmployeeAttendance_Form_Patch.sql`  
- `Production_MyDocuments_Form_Patch.sql`  
- `Production_BankSetup_Patch.sql`  
- `Production_TrainingMaster_Patch.sql`  
- `Production_EmployeeChildTables_Patch.sql`  
- `Production_EmployeeIdentification_Patch.sql`  
- `Production_AccountSecurity_Patch.sql`  
- `Production_ProfileChange_Patch.sql`  

Some services also call runtime `EnsureSchema` helpers (`AccountSecurityService`, `ProfileChangeRequestService`, and others). If DDL is blocked in production, apply the matching SQL patch manually. The profile workflow does **not** migrate existing employee rows; it only adds request/inbox tables.

### 4.4 Key data flows

**Employee Request**

```
UI → EmployeeRequestPage → EmployeeRequestService
  → tblEmployeeRequest (+ attachments / actions / alerts)
  → Manager resolved from Job.ReportsToEmployeeID or PermanentResponsibleEmployeeID
```

**Attendance**

```
UI → EmployeeAttendancePage → AttendanceService
  → AttendanceConnection.[attendance_dashboard] (configurable table name)
  → Enrich via HRMS tblEmployee WHERE BiometricID matches emp code
```

**Permissions**

```
Login → Session
Page gate → PermissionService / specialized CanAccess* methods
Rights stored in tblUserPermission keyed by FormKey
```

**Own-profile change (HR approval)**

```
EmployeeMaster save (OwnsEmployee)
  → ProfileChangeRequestService.BuildDraft (all posted field diffs)
  → Session draft → ProfileChangeConfirm.aspx
  → SubmitDraft → tblProfileChangeRequest + Line + History (Pending)
  → tblAppInbox (employee + HR reviewers)
  → Official tblEmployee / child tables unchanged
  → ProfileChangeReview.aspx Decide
       Approve → ApplyApproved in one SqlTransaction (all fields or rollback)
       Reject → status only; no official writes
```

`RequiresWorkflow` is true when `EmployeeProfileAccessService.OwnsEmployee(employeeId)`. Editing a different employee skips the workflow and uses `SaveEmployeeCore` / child sync immediately.

**Account security**

```
Login.aspx → AuthService (hash, lockout, MustChangePassword)
  → if 2FA confirmed: Verify2FA.aspx (TotpHelper / backup code)
  → AccountSettings.aspx → AccountSecurityService
ForgotPassword.aspx → questions or unused backup code (no email)
```

---

## 5. APIs and Integrations

This application is **not** primarily a REST API host. Integration points:

| Integration | Type | Direction | Notes |
|-------------|------|-----------|--------|
| Attendance SQL Server | ADO.NET | HRMS → Attendance DB (read) | Table `attendance_dashboard`; column alias discovery |
| File system | Local paths | Bidirectional | LMS documents, uploads, gallery images |
| Browser | HTTP | Bidirectional | Standard Web Forms posts |

There is no separate public HTTP LMS product; Knowledge Library is internal. Account security and profile notifications are **offline** (no email, SMS, or cloud MFA).

### 5.1 Attendance appSettings

| Key | Default | Meaning |
|-----|---------|---------|
| `Attendance:TableName` | `attendance_dashboard` | Physical attendance table |
| `Attendance:PageSize` | `50` | Page size for listings |

Status codes handled in service logic include examples such as: P, A, Z, H, G, LT, K, E, B, F, ER (and UI views for late/early/absent/etc.).

---

## 6. Authentication and Authorization

### 6.1 Authentication

| Item | Detail |
|------|--------|
| Login page | `Login.aspx` (`IsPublicPage`) |
| Credential store | `tblUser.Username`, `PasswordHash`, `IsActive` |
| Extra user columns | `TwoFactorEnabled`, `TwoFactorSecret`, `TwoFactorConfirmedOn`, `MustChangePassword`, `FailedLoginCount`, `LockoutUntil`, `PasswordChangedOn`, `SecurityStamp` |
| 2FA | `TotpHelper` (no NuGet); backup hashes in `tblUserRecoveryCode`; questions in `tblUserSecurityQuestion` |
| Throttle | `tblAuthThrottle` plus per-user lockout |
| Public pages | `Login`, `ForgotPassword`, `Verify2FA`, plus bootstrap utilities (`ResetAdmin`, `InitDatabase`) |
| Session | InProc; keys include `UserID`, `Username`, `FullName`, `IsAdmin`, `LinkedEmployeeID`, security stamp; profile draft key `ProfileChangeDraft` |
| Linked employee | `TOP 1 EmployeeID FROM tblEmployee WHERE UserID=@UserID` |
| Bootstrap admin | `EnsureUserTableAndAdmin` **inserts** `admin` only if missing; it does not overwrite an existing admin hash on startup |

**Security consideration:** Rotate default credentials immediately after deploy. Restrict `ResetAdmin.aspx` on production. Keep IIS and authenticator phones NTP-aligned (±1 TOTP step).

### 6.2 Authorization model

| Mechanism | Implementation |
|-----------|----------------|
| Form rights | `tblUserPermission` flags |
| Admin bypass | `IsAdmin` |
| Path resolution | `AppForms.FindByPath` / `NormalizePath` (strips `.aspx`) |
| Profile rules | `EmployeeProfileAccessService` |
| Module rules | e.g. `EmployeeRequestService.CanAccessModule`, `DisciplineService.CanAccessModule`, `LmsDocumentService.CanAccessLibrary` |
| Data scope | `DataAccessScopeService` |

Permission flags: **CanRead**, **CanWrite**, **CanDelete**, **CanApprove**, **CanExport**.

---

## 7. Configuration and Environment

### 7.1 `Web.config` (critical)

| Setting | Purpose |
|---------|---------|
| `HRMSConnection` | Primary DB |
| `AttendanceConnection` | Attendance DB |
| `compilation targetFramework` | 4.8; must include `netstandard` assembly for ASPX runtime compilation (ClosedXML is netstandard2.0) |
| `httpRuntime maxRequestLength` | `51200` (KB) — upload ceiling |
| `sessionState` | `InProc`, `timeout="480"` (minutes), cookie `HRMS_Session` |
| `httpCookies` | `httpOnlyCookies="true"` (set `requireSSL` when the site is HTTPS) |
| `authentication mode` | `None` (custom session) |
| `customErrors` | Currently `Off` in source — tighten for production |
| `debug` | Currently `true` in source — set `false` for production |
| default document | `Default.aspx` |
| SVG MIME | Mapped for static SVG |

### 7.2 Environments

| Environment | Typical HRMS DB | Attendance |
|-------------|-----------------|------------|
| Developer LocalDB | Attach `HRMSDB.mdf` under `D:\Project\DATA` | Optional / VPN to attendance server |
| Production | Full SQL Server instance | Production attendance server with ReadOnly intent |

### 7.3 Local setup (summary)

Per `Database\README_LocalDB.md`:

1. Run LocalDB setup batch or ordered SQL scripts.  
2. Start app via `RUN.bat`.  
3. Login with seeded admin (change after first use in non-dev).

---

## 8. Deployment and Infrastructure

### 8.1 Prerequisites

- Windows host with **IIS** (or IIS Express / project runner)  
- **.NET Framework 4.8**  
- SQL Server or **LocalDB** with `sqlcmd` / `sqllocaldb` for scripted setup  
- Network reachability to attendance SQL Server if attendance module is used  
- Write access to upload/document folders used by the app  

### 8.2 Deploy checklist

1. Publish / copy Web Forms site files to IIS site folder.  
2. Set `HRMSConnection` and `AttendanceConnection` for the environment.  
3. Apply base schema + all required `Production_*` patches not yet applied, including **Account Security** and **Profile Change** if those features are in the build.  
4. Ensure app pool identity can read/write needed folders and connect to SQL. If the SQL login cannot `CREATE`/`ALTER`, patches must be run by a DBA.  
5. Disable debug; enable appropriate `customErrors`; harden secrets.  
6. Create non-default admin users; assign User Rights (include **Profile Update Requests** for HR reviewers); link employees to users.  
7. Verify Home tiles, login/2FA, own-profile workflow, leave, requests, attendance, and LMS smoke tests.  
8. Confirm production `Web.config` contains the `netstandard` compilation assembly and that `bin\netstandard.dll` is present if IIS compiles `.aspx` on the server. App pool CLR must be v4.0.  
9. Profile confirm uses InProc session for the draft: web farms need sticky sessions (or another session mode).  

### 8.3 Infrastructure notes

- Session is **InProc** → multi-server web farms need sticky sessions or a different session mode.  
- Attendance uses `ApplicationIntent=ReadOnly` in the sample connection string.  
- Dual-database design isolates biometric punch data from HRMS transactional writes.

---

## 9. Error Handling, Logging, Security, and Performance

### 9.1 Error handling

- Domain services throw `InvalidOperationException` (and similar) for unauthorized workflow actions.  
- UI surfaces user-facing messages on pages.  
- `customErrors Off` exposes detailed errors in the checked-in config — **not recommended for production**.

### 9.2 Logging and audit

- Security / data changes may be recorded via audit facilities (`tblAuditLog`, discipline audit tables, request action logs).  
- Prefer reviewing Audit Log Report and module-specific histories for investigations.

### 9.3 Security considerations

| Topic | Guidance |
|-------|----------|
| Passwords / recovery | PBKDF2 hashes only; never log plaintext, TOTP secrets, or backup codes after display |
| Secrets | Keep SQL passwords out of source control and client code |
| Authorization | Fail-closed for unknown paths |
| Uploads | Respect `maxRequestLength`; validate file types where UI enforces |
| Attendance | Treat as read-only; least-privilege SQL login |
| Admin account | Change default password; limit IsAdmin |
| Transport | Use HTTPS in production |

### 9.4 Performance considerations

| Area | Notes |
|------|-------|
| Attendance queries | Paginate (`Attendance:PageSize`); filter by date/employee |
| Biometric join | Index on `tblEmployee.BiometricID` (patch provides) |
| Session timeout | 480 minutes reduces re-login but increases idle session memory |
| InProc session | Memory bound to worker process; recycle clears sessions |
| MARS | Enabled on HRMS connection for multiple active readers |

---

## 10. Technical Dependencies and Prerequisites

### 10.1 Build / runtime

- Visual Studio or MSBuild targeting .NET Framework 4.8  
- NuGet / assembly redirects as in `Web.config` (e.g. `System.Runtime.CompilerServices.Unsafe`)  
- ClosedXML (netstandard2.0) for Excel; requires `netstandard` facade for ASPX compilation  
- IIS modules for ASP.NET 4.x; no internet/MFA SaaS dependencies for 2FA or profile notices  

### 10.2 Database tools

- SQL Server Management Studio or Azure Data Studio  
- LocalDB: `sqllocaldb`, `sqlcmd`  

### 10.3 External systems

- Attendance dashboard database with compatible column shapes (service discovers aliases for emp code, name, dept, dates, in/out, status)  

### 10.4 Optional / related

- Excel export helpers where implemented  
- Image gallery content for Home  

---

## 11. Special Technical Behaviours

### 11.1 BiometricID join

- Column: `tblEmployee.BiometricID` (INT).  
- Attendance emp code matched as `CAST(BiometricID AS NVARCHAR(50))`.  
- Missing matches still return attendance rows without full enrichment.

### 11.2 Manager resolution

Shared by Employee Request (and related discipline display):

1. `tblJob.ReportsToEmployeeID` via employee `JobID`  
2. Else `tblEmployee.PermanentResponsibleEmployeeID`  

### 11.3 Schema self-heal vs patches

`StartupMigrations.Run()` calls `DatabaseBootstrap.EnsureCoreSchema`, `AuthService.EnsureUserTableAndAdmin`, `AccountSecurityService.EnsureSchema`, and `ProfileChangeRequestService.EnsureSchema`. Production environments that deny DDL must apply `Database\Production_*.sql` scripts explicitly (`Production_AccountSecurity_Patch.sql`, `Production_ProfileChange_Patch.sql`, identification and child-table patches as needed).

### 11.3.1 ASPX compilation and netstandard

Pages that use LINQ in markup (historically `ProfileChangeConfirm.aspx`) can fail on IIS with CS0012 (`netstandard` not referenced) even when the site compiles in Visual Studio. Prefer filtering in code-behind (`VisibleLines`). Deploy `Web.config` compilation reference to `netstandard, Version=2.0.0.0` and copy `netstandard.dll` into `bin` from the .NET 4.8 Facades folder when the server lacks the targeting pack.

### 11.4 Pages vs AppBase

Prefer understanding logic in `AppBase\*` and `Services\*` rather than Razor under `Pages\`, which is not the production UI path.

---

## 12. Document Control

| Item | Value |
|------|--------|
| Companion documents | Functional Documentation; End-User Process Manual |
| Integration | Standalone only — not wired into application menus or help UI |
| Updated | Account security schema/services, profile change workflow, production patches, netstandard IIS compile note |

---
