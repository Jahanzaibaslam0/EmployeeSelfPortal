# Ghazi Brothers HRMS — Functional Documentation

**Document type:** Functional Documentation  
**Application:** Ghazi Brothers Human Resource Management System (HRMS)  
**Audience:** Business analysts, HR process owners, product owners, trainers  
**Scope:** Existing application behaviour only (standalone documentation; not part of the application runtime)

---

## 1. Application Overview and Purpose

### 1.1 Purpose

Ghazi Brothers HRMS is an internal web application that supports Human Resources and related administrative processes for Ghazi Brothers. It provides:

- Centralized **employee master data** and organizational structure
- **Self-service** for profile (with HR approval of official changes), documents, and knowledge materials
- **Account security** (password policy, offline two-factor authentication, recovery, lockout)
- **HR process** workflows (leave, attendance viewing, employee requests, profile change approval, discipline, performance, training, expenses, recruitment)
- **Communication** (announcements, memorandums, and in-app inbox / notification bell)
- Extensive **setup / reference data** for organization, employees, customers, and products
- **Security administration** (users, form rights, audit)

### 1.2 Intended Users

| User type | Typical use |
|-----------|-------------|
| Employees | Profile, documents, leave, requests, knowledge library, announcements |
| Managers | Approve requests for direct reports; participate in discipline/performance as allowed |
| HR staff | Employee master, leave, requests (HR stage), discipline, attendance, directory |
| Management | Final approval of employee requests; oversight of cases as permitted |
| System administrators | Users, rights, setup masters, LMS document publishing, audit |

### 1.3 Delivery Model

The live application is a **Classic ASP.NET Web Forms** site. Users access it through a browser after logging in. Home presents process tiles filtered by each user’s permissions and profile linkage.

---

## 2. Features and Functionality (Summary)

| Area | Capabilities |
|------|----------------|
| Authentication | Username/password, optional TOTP 2FA, lockout, must-change password, session stamp |
| Authorization | Form-level Read / Write / Delete / Approve / Export; admin bypass; data scope |
| Self-service | My Profile (HR-approved official changes), Account Settings, My Documents, Knowledge Library |
| Communication | Notifications (announcements), Memorandums, in-app inbox and header bell |
| Master data | Employee Master, Position Master / Hierarchy, Employee Directory |
| HR processes | Leave, Attendance (view), Employee Requests (3-level), Profile Update Requests, Discipline & Non-Conformance, Performance, Training, Expenses, Recruitment |
| Shortcuts | Quick Links (software / process links) |
| Setup | Organization, Employee, Customer, Product reference tables |
| Security | User Setup (including 2FA/lockout assist), User Rights Setup, Audit Log Report |
| Dashboards | Home gallery + HRMS Dashboard (where permitted) |

---

## 3. Modules and Components

### 3.1 Home and navigation

| Module | Form key | Page | Description |
|--------|----------|------|-------------|
| Home | `Home` | `/Home.aspx` | Landing page with gallery and process tiles |
| Dashboard | `Dashboard` | `/Dashboard.aspx` | HRMS dashboard |
| Quick Links | `QuickLinks` | `/QuickLinks.aspx` | Shortcuts to configured software links |

**Home tile categories:** Self Service, Communication, HR Process, Master Data, Shortcuts.

The application **header** includes Account Settings, an in-app **notification bell** (unread inbox count), and Inbox.

Tiles appear only when the user passes module-specific access checks (see §4).

### 3.2 Self-service

| Module | Form key | Page | Description |
|--------|----------|------|-------------|
| My Profile | `UserProfile` | `/UserProfile.aspx` | Opens own employee record (when linked) |
| Account Settings | `AccountSettings` | `/AccountSettings.aspx` | Display name, password, recovery questions, TOTP 2FA |
| Inbox | (unlisted; session user) | `/Inbox.aspx` | In-app messages (profile request notices and similar) |
| My profile requests | (unlisted; owner/HR) | `/ProfileChangeHistory.aspx` | Status and history of own profile change requests |
| Confirm profile update | (unlisted) | `/ProfileChangeConfirm.aspx` | Review proposed vs approved values before submit |
| My Documents | `MyDocuments` | `/MyDocuments.aspx` | Access employee documents |
| Knowledge Library | `LmsLibrary` | `/LmsLibrary.aspx` | Browse manuals, SOPs, policies, references |

### 3.3 Communication

| Module | Form key | Page | Description |
|--------|----------|------|-------------|
| Announcements | `Notifications` | `/Notifications.aspx` | Company / HR notifications |
| Memorandums | `Memorandums` | `/Memorandums.aspx` | Active memorandums and policy documents |

Related setup: `NotificationSetup`, `MemorandumSetup`, `ImageGallerySetup`.

### 3.4 HR processes

| Module | Form key(s) | Page | Description |
|--------|-------------|------|-------------|
| Leave Management | `LeaveMaster` | `/LeaveMaster.aspx` | Leave applications and categories |
| Employee Attendance | `EmployeeAttendance` | `/EmployeeAttendance.aspx` | Read-only attendance from external dashboard DB |
| Employee Requests | `EmployeeRequest`, `EmployeeRequestHR`, `EmployeeRequestManagement` | `/EmployeeRequest.aspx` | Multi-category requests with Manager → HR → Management flow |
| Profile Update Requests | `ProfileChange` | `/ProfileChangeReview.aspx` | HR review of self-service official profile changes |
| Discipline & Non-Conformance | `EmployeeDiscipline` | `/EmployeeDiscipline.aspx` | Cases, investigation, findings, actions |
| Employee Performance | `PerformanceMaster` | `/PerformanceMaster.aspx` | Performance cycles; can show discipline findings marked for performance |
| Employee Training | `TrainingMaster` | `/TrainingMaster.aspx` | Training and development records |
| Expense Process | `ExpenseMaster` | `/ExpenseMaster.aspx` | Expense claims |
| Recruitment Process | `RecruitmentMaster` | `/RecruitmentMaster.aspx` | Hiring / recruitment records |
| Employee Directory | `EmployeeReport` | `/EmployeeReport.aspx` | Search, view, export internal directory |

### 3.5 Master data

| Module | Form key | Page | Description |
|--------|----------|------|-------------|
| Employee Master | `EmployeeMaster` | `/EmployeeMaster.aspx` | Full employee records (including Biometric ID and Identification). Saving **one’s own** linked record starts the profile approval workflow instead of writing official values immediately |
| Position Master | `PositionMaster` | `/PositionMaster.aspx` | Positions, assignments, reporting structure |
| Position Hierarchy | `PositionHierarchy` | `/PositionHierarchy.aspx` | Hierarchical view of positions |

### 3.6 LMS administration

| Module | Form key | Page | Description |
|--------|----------|------|-------------|
| LMS Document Setup | `LmsDocumentSetup` | `/LmsDocumentSetup.aspx` | Publish and manage knowledge library documents |

### 3.7 Organization setup (reference data)

Examples (all under category **Organization Setup**): Division, Business Segment/Unit, Workforce Segment, Unit, Wing, Gender, Religion, Nationality, Language, Bank / Bank Group, Currency, UoM, Cost Center, Skill, Legal Entity, Sales Team, Work Location Type, Work Arrangement, Extension, City, Province, Sales Group, Department, Region, Location, Software Link, Notification, Memorandum, Image Gallery, LMS Document Setup.

### 3.8 Employee setup

Grade, Employment Type, Designation Level, Title, Employment Status, Benefit, Benefit Entitlement, Expense Category, Leave Category, Blood Group, Worker Category, Job, Worker Location, Document Type.

### 3.9 Customer and product setup / masters

Customer and product setup forms (delivery, payment, groups, tax, brands, HS codes, etc.) plus transactional masters where registered: Customer, Contact, Product, Invoice.

### 3.10 Security

| Module | Form key | Page | Description |
|--------|----------|------|-------------|
| User Setup | `UserSetup` | `/UserSetup.aspx` | Create and maintain users; reset password (must-change), disable 2FA, clear lockout |
| User Rights Setup | `UserRightsSetup` | `/UserRightsSetup.aspx` | Assign form permissions |
| Audit Log Report | `AuditReport` | `/AuditReport.aspx` | Review audit entries |
| Forgot password | (public) | `/ForgotPassword.aspx` | Offline recovery via security questions or unused backup code |
| Two-factor challenge | (public after password) | `/Verify2FA.aspx` | TOTP or backup code after password succeeds |

---

## 4. User Roles and Permissions

### 4.1 Concepts

| Concept | Behaviour |
|---------|-----------|
| **Administrator** (`IsAdmin`) | Bypasses form permission checks |
| **Form rights** | Per user, per form key: CanRead, CanWrite, CanDelete, CanApprove, CanExport |
| **Linked employee** | `tblEmployee.UserID` → session `LinkedEmployeeID`; required for many self-service features |
| **Data scope** | OwnOnly / Restricted / All (department/location restrictions) for certain listings |
| **Fail-closed pages** | Unknown paths are denied except a small set of public/bootstrap paths |

### 4.2 Default rights for new users

Typically granted read access to: Home, User Profile, My Documents, Knowledge Library, Dashboard (implementation may re-apply defaults when saving rights).

### 4.3 Special access rules

| Feature | Access rule (functional) |
|---------|--------------------------|
| My Documents | Administrator **or** user linked to an employee; browsing **all** employees’ documents is admin-only |
| Knowledge Library | Admin, library read right, linked employee, or LMS setup read |
| Employee Requests | Admin, any of the three request form keys (read), or linked employee |
| Employee Request – HR actions | Admin or rights on `EmployeeRequestHR` |
| Employee Request – Management actions | Admin or rights on `EmployeeRequestManagement` |
| Discipline module | Module access via `EmployeeDiscipline` rights / service checks |
| Employee-related forms | Profile linkage **or** explicit CanRead (except pure User Profile rules) |

### 4.4 Manager identification (requests / discipline)

The system resolves an employee’s **manager** as:

1. Job **Reports-To employee** (`tblJob.ReportsToEmployeeID` via the employee’s job), if set  
2. Else **Permanent Responsible** employee on the employee record  

Position hierarchy (`ReportsToPositionID`) supports org structure and is **not** the same as request approval manager resolution.

### 4.5 Recommended role patterns (business)

| Business role | Suggested form rights (illustrative) |
|---------------|--------------------------------------|
| Employee | Home, UserProfile, AccountSettings, MyDocuments, LmsLibrary, LeaveMaster (as needed), EmployeeRequest, Notifications, Memorandums |
| Line manager | Above + ability to see pending requests for reports (system uses manager employee ID, not a separate “manager” form key) |
| HR | EmployeeMaster, **ProfileChange** (read/approve), LeaveMaster, EmployeeAttendance, EmployeeRequest + **EmployeeRequestHR**, EmployeeDiscipline, directory, setup as needed |
| Management approver | **EmployeeRequestManagement** (+ read EmployeeRequest as needed) |
| Admin | UserSetup, UserRightsSetup, AuditReport, full setup masters |

---

## 5. Business Rules and Workflows

### 5.1 Login and session

1. User enters username and password.  
2. System validates active user and password hash. Failed attempts increment a lockout counter (generic “invalid credentials” message; lockout after repeated failures).  
3. If **Must change password** is set, the user is sent to Account Settings before other pages.  
4. If **two-factor authentication** is enabled and confirmed, password success is not enough: the user must enter a current authenticator code or an unused backup code (`Verify2FA`).  
5. Session stores user identity, admin flag, linked employee (if any), and a security stamp. Password/2FA changes invalidate other sessions on the next request.  
6. Unlinked non-admin users may see a profile sync warning on Home.  
7. Session timeout is long-lived (configured at application level; see Technical Documentation).  
8. Forgot password is **offline**: security questions and/or backup codes. The application does not send email or SMS.

### 5.1.1 Account Settings (own user account)

Signed-in users manage **account** data (not the official employee profile) on Account Settings:

- Display name (username is read-only)  
- Change password (current password + policy)  
- Two recovery questions (current password required)  
- Enable/confirm TOTP 2FA with an authenticator app (offline); save backup codes once  
- Disable 2FA with current password  

Administrators can reset password (forces must-change), disable 2FA, and clear lockout on User Setup. TOTP is RFC 6238 (6 digits, 30 seconds). Keep server and phones time-synced (NTP).

### 5.2 Employee Request workflow (3 levels)

**Categories:** Mobile Phone, Health Claim, Travelling, Employee Loan, Vehicle, Fuel, Other.

**Statuses:**

| Status | Meaning |
|--------|---------|
| Draft | Editable by requester; not in approval |
| Returned for Correction | Returned by an approver; editable then resubmit |
| Pending Manager Approval | Awaiting manager |
| Pending HR Approval | Awaiting HR |
| Pending Management Approval | Awaiting management |
| Approved | Fully approved |
| Rejected | Rejected (comments required) |

**Happy path:**

```
Draft → Submit → Pending Manager → Approve → Pending HR
     → Approve → Pending Management → Approve → Approved
```

**Alternate paths:** Reject at any approval stage → Rejected; Return for correction → Returned for Correction → edit → resubmit.

**Rules:**

- Submit assigns/resolves manager and moves to Pending Manager Approval.  
- Only Draft / Returned for Correction can be edited or (re)submitted.  
- Manager acts on Pending Manager Approval.  
- HR acts when user has HR form rights and status is Pending HR Approval.  
- Management acts when user has Management form rights and status is Pending Management Approval.  
- Attachments and action history are retained.  
- In-app alerts notify relevant parties of actions.

### 5.3 Leave Management

- Leave applications store category, type (e.g. Planned / Unplanned / Future Unplanned), dates, days, reason, and temporary/permanent responsible employees.  
- Categories are maintained via Leave Category setup (seeded examples: Casual, Annual, Maternity, Sick, Leave without pay).  
- Listings respect data access scope; self-service defaults toward the linked employee.  
- **Note:** Leave is managed as application records; a separate multi-level approval state machine like Employee Requests is not the same pattern in this module.

### 5.4 Discipline & Non-Conformance

**Case statuses:** Open → Under Investigation → Action Required → Resolved → Closed.

**Rules:**

- Cases capture investigation notes, corrective/disciplinary actions, responsible persons, final decision, and management remarks.  
- Findings can be flagged **Include in Performance**.  
- Performance module can surface findings marked for performance.  
- Attachments and audit trail support case history.  
- Manager name resolution uses the same job Reports-To / Permanent Responsible logic as requests.

### 5.5 Attendance

- Attendance data is **read from an external attendance database** (not authored in HRMS).  
- HRMS enriches rows by matching attendance employee code to Employee Master **Biometric ID**.  
- Views include Daily Attendance, Monthly Summary, Late Arrival, Early Departure, Absentee, Leave-related, and Punch Detail style reports (UI view modes).  
- Unmatched biometric codes may still display attendance rows without full HRMS enrichment.

### 5.6 Knowledge Library (LMS)

- Documents categorized (e.g. General, Department, System Manual, SOP, Policy, Reference).  
- Access scopes: Organization, Department, Job, Restricted (with explicit grants).  
- Library consumers browse/download per rights; publishers use LMS Document Setup.

### 5.7 Employee Master and Biometric ID

- Biometric ID on the employee record is the bridge to attendance employee codes.  
- Incorrect or missing Biometric ID causes incomplete name/department enrichment on attendance screens.  
- Identification tab stores CNIC and related documents on `tblEmployeeIdentification`.  
- Profile-only users see HR-managed fields locked (salary, work agreement, attendance method, org assignment, and similar).

### 5.8 Official profile update workflow (HR approval)

When a user saves **their own** linked employee profile (My Profile / Employee Master on self), the official record is **not** updated immediately.

**Statuses:** Draft (session confirm page) → **Pending HR Approval** → **Approved** or **Rejected**.

**Happy path:**

```
Edit own profile (one or many fields) → Confirm proposed vs current
  → Submit (PCR-#####) → Official values unchanged
  → HR reviews all fields → Approve → All proposed fields applied in one transaction
```

**Reject path:** HR requires comments. No proposed field is written. Previous approved values remain.

**Rules:**

- Applies to any signed-in user updating the employee record linked to their account (including HR staff updating themselves).  
- HR/admin editing **another** employee still saves official values immediately.  
- One pending request per employee at a time.  
- Submitter cannot approve or reject their own request.  
- One submission may include many fields (personal, contacts, addresses, identification, and other posted profile sections). HR sees every visible change. Approval applies all; failure rolls back the whole apply.  
- Notifications are **in-app only** (inbox + header bell), not email.  
- Reviewers: system admin, or User Rights on **Profile Update Requests** (read/approve), Employee Master approve/write, or Leave Management – HR Manager read.

### 5.9 In-app inbox

`tblAppInbox` stores messages per user (title, body, link, related type/id, read flag). Profile submit/approve/reject writes inbox rows for the employee and for HR reviewers. The header bell shows unread count.

---

## 6. Functional Behaviour and Validations

### 6.1 Cross-cutting

| Rule | Behaviour |
|------|-----------|
| Authentication required | Most pages redirect unauthenticated users to login |
| Authorization | Insufficient rights → access denied (e.g. Home with access-denied indicator) |
| Password policy | Enforced on change/reset (length and complexity via `PasswordHelper`) |
| 2FA | After password, TOTP or unused backup code when enabled |
| Own profile save | Queues HR request; official tables unchanged until approve |
| Admin | Full form access |
| Linked profile | Required for several self-service and employee-scoped actions |
| Uploads | Subject to max request size configured for the site |

### 6.2 Employee Requests

| Validation / behaviour | Detail |
|------------------------|--------|
| Edit allowed | Draft or Returned for Correction only |
| Submit | Requires resolvable manager path for progression |
| Reject | Requires comments |
| Stage actions | Enforced by status + role rights |
| Categories | Restricted to defined category codes |

### 6.3 Discipline

| Validation / behaviour | Detail |
|------------------------|--------|
| Case status | Must be one of defined statuses; invalid falls back to Open on save paths |
| Closed / Resolved | Closed timestamps/users recorded when first entering those statuses |
| Findings | Optional performance inclusion flag |

### 6.4 Leave

| Validation / behaviour | Detail |
|------------------------|--------|
| Category | Must exist in leave categories |
| Dates / days | Captured on application; UI validates required fields per form design |
| Scope | Users see applications allowed by data scope / linkage |

### 6.5 Attendance

| Validation / behaviour | Detail |
|------------------------|--------|
| Connectivity | Depends on AttendanceConnection availability |
| Filters | Date range, employee, department, status codes as exposed on UI |
| Pagination | Configurable page size |

---

## 7. Feature-by-Feature Descriptions

### 7.1 Home

Displays image gallery slides (from gallery setup) and permission-filtered process tiles. Warns if the account is not linked to an employee.

### 7.2 My Profile

Allows the linked employee to view the **approved** official profile. Writable personal, contact, address, identification, and related profile fields are submitted for **HR approval**. Official values change only after a different HR user approves. Profile-only users cannot edit HR-managed employment/compensation fields.

### 7.2.1 Account Settings

Own user account: password, recovery questions, TOTP 2FA, display name. Separate from Employee Master / My Profile HR data.

### 7.3 My Documents

Employee-facing document list for the linked employee; administrators may access broader document browsing.

### 7.4 Knowledge Library

Searchable/browsable library of published documents filtered by category and access scope.

### 7.5 Announcements and Memorandums

Read company communications; setup screens maintain content. Login may set popup-related session flags for memorandums/notifications.

### 7.6 Employee Master

HR maintains comprehensive employee data: personal, job, org placement, contacts, addresses, identification (CNIC and related), family, bank, education, documents, certificates, and **Biometric ID**. Saving another person’s record writes official data immediately. Saving the signed-in user’s **own** linked record follows §5.8.

### 7.6.1 Profile Update Requests

HR opens pending `PCR-#####` requests, compares current approved vs proposed values for every field in the request, and Approves (apply all in one transaction) or Rejects (comments required; apply none).

### 7.7 Position Master / Hierarchy

Defines positions and reporting structure for organizational design.

### 7.8 Employee Directory

Searchable internal directory with export capability when CanExport is granted.

### 7.9 Leave Management

Create and maintain leave applications against leave categories and types; assign responsible coverage employees.

### 7.10 Employee Attendance

Operational attendance visibility: KPIs and tabular views fed from the external attendance dashboard database, enriched with HRMS employee data via Biometric ID.

### 7.11 Employee Requests

End-to-end request lifecycle with attachments, comments, alerts, and three approval stages (Manager, HR, Management). HR and Management capabilities are controlled by dedicated form keys on the same page.

### 7.12 Discipline & Non-Conformance

Case management from opening through investigation, findings, corrective/disciplinary action, and closure; optional performance linkage.

### 7.13 Employee Performance

Records performance cycles and can incorporate discipline findings flagged for performance.

### 7.14 Training, Expenses, Recruitment

Dedicated process screens for training records, expense claims, and recruitment cases (rights-gated).

### 7.15 Quick Links

Presents configured software/process links (maintained via Software Link Setup).

### 7.16 Setup modules

Maintain lookup and master reference data that drives dropdowns and validations across the application.

### 7.17 Security administration

Create users, assign form rights and data scopes, and review audit logs. User Setup can force password change, disable 2FA, and clear lockout. Audit events include login success/failure, password change/recovery, and 2FA enable/disable, plus profile change submit/approve/reject.

### 7.18 Customer / Product / Invoice masters

Registered transactional and setup modules for commercial reference data where deployed and permitted.

---

## 8. Document Control

| Item | Value |
|------|--------|
| Source of truth | Live Web Forms application under the HRMS project |
| Companion documents | Technical Documentation; End-User Process Manual |
| Integration | Standalone files only — not embedded in the application |
| Updated | Account Settings (2FA/recovery), official profile HR approval, in-app inbox |

---
