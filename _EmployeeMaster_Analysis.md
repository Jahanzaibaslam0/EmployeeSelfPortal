# EmployeeMaster Extraction & Gap Analysis

**Sources analyzed**
| Role | Path |
|------|------|
| Razor UI (reference) | `D:\Project\HRMS\Pages\EmployeeMaster.cshtml` (~1372 lines) |
| Razor backend (reference) | `D:\Project\HRMS\Pages\EmployeeMaster.cshtml.cs` (~2179 lines) |
| WebForms UI (target) | `D:\Project\HRMS\EmployeeMaster.aspx` (~76 lines) |
| WebForms backend (target) | `D:\Project\HRMS\EmployeeMaster.aspx.cs` (~315 lines) |
| Client JS (Razor tabs/validation) | `wwwroot/js/app.js`, `wwwroot/js/field-validation.js` |

**Date:** 2026-08-20  
**Purpose:** Implementation guide to bring WebForms EmployeeMaster to parity with Razor.

---

## A. Tabs / Sections in the Razor UI

Razor uses **form sections** (not Bootstrap tabs) for the main employee form, plus **7 profile tabs** for child collections. List view is separate when `ShowForm == false`.

### A0. List view (when not editing)

| Element | Type | Notes |
|---------|------|-------|
| Stat chips | display | Total / Active / Inactive counts |
| Excel panel | partial `_MasterExcelPanel` | Export + Import (full-access only) |
| `txtSearch` | text | Client filter via `searchTable()` |
| Grid columns | table | #, Emp Code, Full Name, Legal Entity, Department, Employment Type, Employment Status, Designation, Mobile, Email, Joined, Status, Actions |
| Actions | links/forms | ID Card, Edit/View, hard Delete (full access) |
| `+ New Employee` | link | `?newEmployee=true` (full access) |

### A1. Section: Primary Identifier

| Label | name | Control | Required | MaxLength / constraints | Default |
|-------|------|---------|----------|-------------------------|---------|
| Profile Picture | `ProfilePhoto` | file | No | JPG/PNG/WEBP, max 5 MB | — |
| Remove current photo | `RemovePhoto` | checkbox | No | value=`true` | unchecked |
| (hidden) Photo path | `PhotoPath` | hidden | — | — | current path |
| Employee ID | `EmployeeCode` | text | **Yes** | 20 | — |
| Employee Status | `Status` | select | **Yes** | Active / Inactive | `Active` |
| First Name | `FirstName` | text | **Yes** | 100 | — |
| Last Name | `LastName` | text | **Yes** | 100 | — |
| Father's / Husband's Name | `FathersHusbandsName` | text | No | 150 | — |
| Display Name | `DisplayName` | text | No | 200 | — |
| National ID Number | `NationalIDNumber` | text | No | 50 | — |
| Date of Birth | `DateOfBirth` | date | No | — | — |
| Age | *(no name)* | text readonly | No | computed | from DOB |
| Gender | `GenderID` | select | **Yes** | lookup | — |
| Language | `LanguageID` | select | No | lookup | — |

Hidden form fields: `EmployeeID`, `EditMode`, `ContactsJson`, `AddressesJson`, `FamilyMembersJson`, `BanksJson`, `EducationJson`.

### A2. Section: Demographic Information

| Label | name | Control | Required | MaxLength | Default |
|-------|------|---------|----------|-----------|---------|
| Nationality | `NationalityID` | select | No | lookup | — |
| Domicile | `Domicile` | text | No | 150 | — |
| Religion | `ReligionID` | select | No | lookup | — |
| Marital Status | `MaritalStatus` | select | No | hardcoded: Single, Married, Divorced, Widowed, Separated | — |
| Blood Group | `BloodGroupID` | select | No | lookup | — |

### A3. Section: Organization Assignment

| Label | name | Control | Required | Notes |
|-------|------|---------|----------|-------|
| Legal Entity | `LegalEntityID` | select | No | |
| Business Unit | `BusinessUnitID` | select | No | |
| Division | `DivisionID` | select | No | |
| Department | `DepartmentID` | select | **Yes** | from `Departments` |
| Designation | `Designation` | text | **Yes** | maxlength 100 |
| Section / Team | `SalesTeamID` | select | No | |
| Cost Center | `CostCenterID` | select | No | |
| Region | `RegionID` | select | No | |
| Worker Location | `WorkerLocationID` | select | No | |
| Location | `LocationID` | select | No | |
| Extension | `ExtensionID` | select | No | |
| Employment Status | `EmploymentStatusID` | select | No | |
| Employment Type | `EmploymentTypeID` | select | No | |
| Job | `JobID` | select | No | |
| City | `CityID` | select | No | |
| Province | `ProvinceID` | select | No | |
| Sales Group | `SalesGroupID` | select | No | |
| Grade | `GradeID` | select | No | |
| Basic Salary | `BasicSalary` | number | **Yes** | min=0, step=0.01 |

### A4. Section: Additional Employment

| Label | name | Control | Required | Default |
|-------|------|---------|----------|---------|
| User | `UserID` | select | No | — |
| Temporary Responsible | `TemporaryResponsibleEmployeeID` | select | No | employee lookup |
| Permanent Responsible | `PermanentResponsibleEmployeeID` | select | No | employee lookup |
| Worker Category | `WorkerCategoryID` | select | No | — |
| Workforce Segment | `WorkforceSegmentID` | select | No | — |

### A5. Section: Worker Joining & Tenure

| Label | name | Control | Required | Notes |
|-------|------|---------|----------|-------|
| Joining Date | `DateOfJoining` | date | **Yes** | drives Total Tenure |
| Employment Start Date | `EmploymentStartDate` | date | No | drives role tenure / probation |
| Probation Period (Days) | `ProbationPeriodDays` | number | No | min=0 |
| Probation End Date | `ProbationEndDate` | date | No | auto = start + days |
| Confirmation Date | `ConfirmationDate` | date | No | |
| Total Tenure | *(readonly)* | text | No | computed |
| Current Role Tenure | *(readonly)* | text | No | computed |

### A6. Section: Compensation & Benefits

| Label | name | Control | Required | Notes |
|-------|------|---------|----------|-------|
| Benefit Entitlement | `BenefitEntitlementID` | select | No | live preview via AJAX |
| Benefits preview panel | — | display | — | `GET /BenefitEntitlementSetup?handler=GetBenefits` |

### A7. Profile tab: Employee Contact (`contactTab`)

Dynamic rows (`#contactTable`). Per-row fields (JS classes, serialized to `ContactsJson`):

| Column | Property | Control | MaxLength | Notes |
|--------|----------|---------|-----------|-------|
| Type | `ContactType` | select | — | PersonalEmail, OfficialEmail, PersonalMobile, OfficialMobile, WhatsApp, Emergency, PowerBI ID |
| Name | `ContactName` | text | 100 | |
| Relationship | `Relationship` | text | 50 | |
| Value | `ContactValue` | text | 255 | email/phone validated by type |
| Primary | `IsPrimary` | checkbox | — | |
| Action | — | button | — | remove row |

Default empty row: `ContactType=OfficialEmail`, `IsPrimary=true`.  
Separate save: `POST ?handler=SaveContacts`.

### A8. Profile tab: Address (`addressTab`)

| Column | Property | Control | MaxLength | Default |
|--------|----------|---------|-----------|---------|
| Type | `AddressType` | select | — | Current / Permanent / Temporary / Other |
| Address | `AddressLine` | textarea | — | required to persist row |
| City | `City` | text | 100 | |
| Province/State | `ProvinceState` | text | 100 | |
| Postal Code | `PostalCode` | text | 10 | |
| Primary | `IsPrimary` | checkbox | — | |

Default empty row: `AddressType=Current`, `IsPrimary=true`.  
Handler: `SaveAddresses`.

### A9. Profile tab: Family Member (`familyTab`)

| Column | Property | Control | MaxLength |
|--------|----------|---------|-----------|
| Name | `MemberName` | text | 150 |
| Relationship | `Relationship` | text | 50 |
| Gender | `Gender` | select | Male/Female/Other |
| Date of Birth | `DateOfBirth` | date | |
| Contact Number | `ContactNumber` | text | 20 (phone validate) |
| Dependent | `IsDependent` | checkbox | |

Handler: `SaveFamilyMembers`.

### A10. Profile tab: Education (`educationTab`)

| Column | Property | Control | MaxLength / options |
|--------|----------|---------|---------------------|
| Highest Qualification | `HighestQualification` | select | Matric/O-Level, Intermediate/A-Level, Diploma, Certificate, Bachelor, Master, MPhil, PhD, Other |
| Degree/Certificate | `DegreeCertificate` | text | 150 |
| Specialization | `Specialization` | text | 150 |
| Institution | `Institution` | text | 200 |
| Year of Passing | `YearOfPassing` | number | 1950–2100 |
| Grade/CGPA | `GradeCGPA` | text | 20 |

Handler: `SaveEducation`.

### A11. Profile tab: Certificate (`certificateTab`)

| Column | Property | Control | Notes |
|--------|----------|---------|-------|
| Certification Name | `CertificationName` | text | |
| Certification Body | `CertificationBody` | text | |
| Certificate No | `CertificateNumber` | text | |
| Issue Date | `IssueDate` | date | |
| Expiry Date | `ExpiryDate` | date | |
| Renewal Required | `RenewalRequired` | checkbox | |
| Certificate Copy | file `CertCopy_{i}` + `CertificateCopyPath` | file / link | uploads to `/uploads/employee-certificates/` |

Handler: `SaveCertificates` (multipart).

### A12. Profile tab: Documents (`documentTab`)

| Column | Property | Control | Notes |
|--------|----------|---------|-------|
| Document Type | `DocumentTypeID` | select | from `DocumentTypes` |
| Document No | `DocumentNumber` | text | |
| Issue Date | `IssueDate` | date | |
| Expiry Date | `ExpiryDate` | date | |
| Remarks | `Remarks` | text | |
| Upload / View | file `DocFile_{i}` + `DocumentPath` / `OriginalFileName` | file | `/uploads/employee-documents/` |
| Verified | `VerificationStatus` | select/status | Pending / Verified (+ VerifiedOn / VerifiedByUserID) |

Handler: `SaveDocuments` (multipart).

### A13. Profile tab: Bank Information (`bankTab`)

| Column | Property | Control | Default |
|--------|----------|---------|---------|
| Bank | `BankID` | select | from `Banks` |
| Bank Code | `BankCode` | text | often auto-filled from bank |
| Location Name | `LocationName` | text | |
| Bank Group | `BankGroupID` | select | from `BankGroups` |
| IBAN No | `IBAN` | text | |
| Swift/BIC | `SwiftBICCode` | text | |
| Currency Code | `CurrencyCode` | select | from `Currencies` |
| Verification | `AccountVerificationStatus` | select | **Pending** |
| Primary | `IsPrimary` | checkbox | |

Handler: `SaveBanks`. Row persisted only if `BankID > 0`.

### A14. Form footer

- Submit: “Save Employee” / “Update Employee” / “Update My Profile” (if `CanEditCurrentRecord`)
- Back to List (if `HasFullEmployeeAccess`)
- ID Card preview link when editing

---

## B. Backend model properties

### B1. `EmployeeViewModel` (list row)

| Property | Type |
|----------|------|
| `EmployeeID` | `int` |
| `EmployeeCode` | `string` |
| `FullName` | `string` |
| `DepartmentName` | `string` |
| `LegalEntityName` | `string` |
| `EmploymentType` | `string` |
| `EmploymentStatus` | `string` |
| `Designation` | `string` |
| `Phone` | `string` |
| `Email` | `string` |
| `DateOfJoining` | `DateTime?` |
| `BasicSalary` | `decimal` |
| `Status` | `string` (default `"Active"`) |

### B2. `DepartmentItem`

| Property | Type |
|----------|------|
| `DepartmentID` | `int` |
| `DepartmentName` | `string` |

### B3. `LookupItem` / `CurrencyLookupItem`

| Class | Properties |
|-------|------------|
| `LookupItem` | `int Id`, `string Name` |
| `CurrencyLookupItem` | `string Code`, `string Name` |

### B4. `EmployeeInput` (main form)

| Property | Type | Notes |
|----------|------|-------|
| `EmployeeID` | `int` | |
| `EmployeeCode` | `string` | |
| `FirstName` | `string` | |
| `LastName` | `string` | |
| `FathersHusbandsName` | `string` | |
| `DisplayName` | `string` | |
| `NationalIDNumber` | `string` | |
| `Gender` | `string` | denormalized name from GenderID |
| `GenderID` | `int` | |
| `DateOfBirth` | `string` | `yyyy-MM-dd` |
| `MaritalStatus` | `string` | |
| `DepartmentID` | `int` | |
| `DivisionID` | `int` | |
| `NationalityID` | `int` | |
| `ReligionID` | `int` | |
| `LanguageID` | `int` | |
| `WorkerCategoryID` | `int` | |
| `EmploymentTypeID` | `int` | |
| `EmploymentStatusID` | `int` | |
| `WorkforceSegmentID` | `int` | |
| `LegalEntityID` | `int` | |
| `BusinessUnitID` | `int` | |
| `SalesTeamID` | `int` | |
| `CostCenterID` | `int` | |
| `RegionID` | `int` | |
| `LocationID` | `int` | |
| `JobID` | `int` | |
| `WorkerLocationID` | `int` | |
| `CityID` | `int` | |
| `ProvinceID` | `int` | |
| `SalesGroupID` | `int` | |
| `GradeID` | `int` | |
| `ExtensionID` | `int` | |
| `Domicile` | `string` | |
| `BloodGroupID` | `int` | |
| `BenefitEntitlementID` | `int` | |
| `UserID` | `int` | |
| `TemporaryResponsibleEmployeeID` | `int` | |
| `PermanentResponsibleEmployeeID` | `int` | |
| `Designation` | `string` | |
| `DateOfJoining` | `string` | |
| `EmploymentStartDate` | `string` | |
| `ProbationPeriodDays` | `string` | |
| `ProbationEndDate` | `string` | |
| `ConfirmationDate` | `string` | |
| `BasicSalary` | `string` | parsed to decimal on save |
| `Status` | `string` | default Active |
| `PhotoPath` | `string` | |
| `TotalTenureDisplay` | computed | |
| `CurrentRoleTenureDisplay` | computed | |
| `AgeDisplay` | computed | |

### B5. Child input models

**`EmployeeContactInput`:** `ContactType`, `ContactName`, `Relationship`, `ContactValue` (`string`); `IsPrimary` (`bool`)

**`EmployeeAddressInput`:** `AddressType`, `AddressLine`, `City`, `ProvinceState`, `PostalCode` (`string`); `IsPrimary` (`bool`)

**`EmployeeFamilyMemberInput`:** `MemberName`, `Relationship`, `Gender`, `DateOfBirth`, `ContactNumber` (`string`); `IsDependent` (`bool`)

**`EmployeeBankInput`:** `BankID`, `BankGroupID` (`int`); `BankCode`, `LocationName`, `IBAN`, `SwiftBICCode`, `CurrencyCode`, `AccountVerificationStatus` (`string`, default Pending); `IsPrimary` (`bool`)

**`EmployeeEducationInput`:** `HighestQualification`, `DegreeCertificate`, `Specialization`, `Institution`, `YearOfPassing`, `GradeCGPA` (`string`)

**`EmployeeCertificateInput`:** `CertificationName`, `CertificationBody`, `CertificateNumber`, `IssueDate`, `ExpiryDate`, `CertificateCopyPath` (`string`); `RenewalRequired` (`bool`)

**`EmployeeDocumentInput`:** `DocumentTypeID` (`int`); `DocumentTypeName`, `DocumentNumber`, `IssueDate`, `ExpiryDate`, `Remarks`, `DocumentPath`, `OriginalFileName`, `VerificationStatus` (`string`, default Pending)

### B6. Page model state flags / collections

`Employees`, `Departments`, all lookup lists (see §C), `Input`, `ContactRecords`, `AddressRecords`, `FamilyRecords`, `BankRecords`, `EducationRecords`, `CertificateRecords`, `DocumentRecords`, `DocumentTypes`, `EditMode`, `ShowForm`, `AlertMessage`, `AlertType`, `HasFullEmployeeAccess`, `IsProfileOnlyMode`, `CanEditCurrentRecord`.

---

## C. Lookups / dropdowns

Generic loader: `LoadLookup(table, idColumn, nameColumn)` → `SELECT Id, Name FROM table WHERE IsActive = 1 ORDER BY Name`.

| UI field | Page property | Method | Table | ID column | Name column / display |
|----------|---------------|--------|-------|-----------|------------------------|
| Gender | `Genders` | `LoadLookup` | `tblGender` | `GenderID` | `GenderName` |
| Nationality | `Nationalities` | `LoadLookup` | `tblNationality` | `NationalityID` | `NationalityName` |
| Religion | `Religions` | `LoadLookup` | `tblReligion` | `ReligionID` | `ReligionName` |
| Language | `Languages` | `LoadLookup` | `tblLanguage` | `LanguageID` | `LanguageName` |
| Worker Category | `WorkerCategories` | `LoadLookup` | `tblWorkerCategory` | `WorkerCategoryID` | `WorkerCategoryName` |
| Employment Type | `EmploymentTypes` | `LoadLookup` | `tblEmploymentType` | `EmploymentTypeID` | `EmploymentTypeName` |
| Employment Status | `EmploymentStatuses` | `LoadLookup` | `tblEmploymentStatus` | `EmploymentStatusID` | `EmploymentStatusName` |
| Workforce Segment | `WorkforceSegments` | `LoadLookup` | `tblWorkforceSegment` | `WorkforceSegmentID` | `WorkforceSegmentName` |
| Legal Entity | `LegalEntities` | `LoadLookup` | `tblLegalEntity` | `LegalEntityID` | `LegalEntityName` |
| Business Unit | `BusinessUnits` | `LoadLookup` | `tblBusinessUnit` | `BusinessUnitID` | `BusinessUnitName` |
| Division | `Divisions` | `LoadLookup` | `tblDivision` | `DivisionID` | `DivisionName` |
| Section/Team | `SalesTeams` | `LoadLookup` | `tblSalesTeam` | `SalesTeamID` | `SalesTeamName` |
| Cost Center | `CostCenters` | `LoadLookup` | `tblCostCenter` | `CostCenterID` | `CostCenterName` |
| Region | `Regions` | `LoadLookup` | `tblRegion` | `RegionID` | `RegionName` |
| Location | `Locations` | `LoadLookup` | `tblLocation` | `LocationID` | `LocationName` |
| City | `Cities` | `LoadLookup` | `tblCity` | `CityID` | `CityName` |
| Province | `Provinces` | `LoadLookup` | `tblProvince` | `ProvinceID` | `ProvinceName` |
| Sales Group | `SalesGroups` | `LoadLookup` | `tblSalesGroup` | `SalesGroupID` | `SalesGroupName` |
| Grade | `Grades` | `LoadLookup` | `tblGrade` | `GradeID` | `GradeName` |
| Blood Group | `BloodGroups` | `LoadLookup` | `tblBloodGroup` | `BloodGroupID` | `BloodGroupName` |
| Benefit Entitlement | `BenefitEntitlements` | `LoadLookup` | `tblBenefitEntitlement` | `BenefitEntitlementID` | `BenefitEntitlementName` |
| Bank Group | `BankGroups` | `LoadLookup` | `tblBankGroup` | `BankGroupID` | `BankGroupName` |
| Document Type | `DocumentTypes` | `LoadLookup` | `tblDocumentType` | `DocumentTypeID` | `DocumentTypeName` |
| Department | `Departments` | `LoadDepartments` | `tblDepartment` | `DepartmentID` | `DepartmentName` (`ISNULL(IsActive,1)=1`) |
| Job | `Jobs` | `LoadJobLookup` | `tblJob` | `JobID` | `{JobCode} – {JobTitle}` |
| Extension | `Extensions` | `LoadExtensionLookup` | `tblExtension` | `ExtensionID` | `{ExtensionCode} – {ExtensionName}` |
| Worker Location | `WorkerLocations` | `LoadWorkerLocationLookup` | `tblWorkerLocation` + `tblEmployee` + `tblLocation` | `WorkerLocationID` | `{EmployeeCode} – {PrimaryLocationName}` |
| User | `Users` | `LoadUserLookup` | `tblUser` | `UserID` | `{UserCode} – {FullName} ({Username})` or username–fullnameName |
| Temp/Perm Responsible | `EmployeeLookups` | `LoadEmployeeLookup` | `tblEmployee` (Active + data scope) | `EmployeeID` | `{Code} – {Name}` (excludes current) |
| Bank | `Banks` | `LoadBankLookup` | `tblBankMaster` | `BankID` | `{BankName} ({BankCode} - {LocationName})` |
| Currency | `Currencies` | `LoadCurrencyLookup` | `tblCurrency` | `CurrencyCode` | `CurrencyName` |
| Marital Status | hardcoded | — | — | — | string list |
| Contact Type / Address Type / Qualification | hardcoded in JS | — | — | — | see §A |

Benefit preview (not a local lookup): `fetch('/BenefitEntitlementSetup?handler=GetBenefits&entitlementId=...')`.

---

## D. Validations and business rules

### D1. Client-side (`validateForm` / `prepareEmployeePayload` in `app.js`)

**Required:** EmployeeCode, FirstName, LastName, GenderID, DepartmentID, Designation, DateOfJoining, BasicSalary (≥ 0 numeric).

**Contacts:** `HrmsValidation.validateEmployeeContactRows` — email types must be valid email; phone types must be valid phone.

**Family:** phone validation on contact numbers when present.

**Photo:** client alert if > 5 MB; accept JPG/JPEG/PNG/WEBP.

### D2. Server-side (Razor)

| Rule | Where |
|------|--------|
| Page access | `CanAccessEmployeeMasterPage()` |
| Create | `CanCreateEmployee()` for `newEmployee` / insert / Excel import |
| Edit own vs others | `CanEditEmployee(id)`, `CanAccessEmployeeMasterRecord` |
| List visibility | `CanViewEmployeeList()`; else redirect to own profile / UserProfile |
| Profile-only mode | `IsProfileOnlyUser` + owns employee → HR fields locked in UI; `PreserveHrManagedFields` overwrites org/salary/status/dates from DB on save |
| Delete | `CanDeleteEmployee()` + `CanViewEmployee(deleteId)` |
| Excel export | `CanViewEmployeeList()` |
| Duplicate EmployeeCode | catch SQL 2627/2601 → “Duplicate Employee ID…” |
| Contact list | `InputValidators.TryValidateContactList` on SaveContacts |
| Family phones | `InputValidators.TryValidatePhone` on SaveFamilyMembers |
| Photo | server: max 5 MB; extensions jpg/jpeg/png/webp |
| Probation end | `ApplyTenureCalculations`: if start + days > 0 → set ProbationEndDate |
| Upsert by code | if `EmployeeID<=0` but code exists → treat as update |
| Child save guard | `EnsureCanEditEmployee` — must have ID; edit permission |
| Default child rows | OfficialEmail contact; Current address |
| Data scope | `DataAccessScopeService.GetEmployeeFilter("e")` on list + employee lookup |
| Hard delete | cascades child tables then `tblEmployee` (not soft-inactive) |
| Document verified | when status Verified → set `VerifiedOn`, `VerifiedByUserID` |

**Note:** Main `OnPost` does **not** re-check required fields server-side (relies on client + SQL/`DateTime.Parse`/`decimal.Parse` throwing).

### D3. ASPX current rules (much thinner)

- Required: Code, First Name, Department only (Last Name / Designation / DOJ / Salary optional).
- Delete = soft inactive (`Status='Inactive'`), permission via `CanCreateEmployee() || IsAdmin` (not `CanDeleteEmployee`).
- Partial profile access redirect to `editId=ownId` instead of UserProfile in some paths.
- No profile-only field lock, no child validation, no photo rules.

---

## E. CRUD / handlers

### E1. Razor handlers

| Handler | Purpose |
|---------|---------|
| `OnGet(editId?, newEmployee?)` | List or form; load lookups + children on edit |
| `OnGetExportExcel` | Excel export via `MasterExcelService` |
| `OnPostImportExcel(IFormFile)` | Excel import |
| `OnPost(...)` | Insert/Update employee core + photo; redirects to `editId` |
| `OnPostSaveContacts` | Replace contacts |
| `OnPostSaveAddresses` | Replace addresses |
| `OnPostSaveFamilyMembers` | Replace family |
| `OnPostSaveBanks` | Replace banks |
| `OnPostSaveEducation` | Replace education |
| `OnPostSaveCertificates` | Replace certificates + file uploads |
| `OnPostSaveDocuments` | Replace documents + file uploads |
| `OnPostDelete(deleteId)` | Hard delete employee + children |

**Child pattern:** DELETE all for EmployeeID → INSERT filtered rows (transaction). Certificates/docs also accept `Request.Form.Files["CertCopy_{i}"]` / `["DocFile_{i}"]`.

**Photo:** `SaveProfilePhotoFile` → `/uploads/employee-photos/emp_{id}_{timestamp}{ext}`; remove deletes file.

**Search/filter:** client-only on list (`searchTable`).

### E2. ASPX handlers (current)

| Handler | Purpose |
|---------|---------|
| `Page_Load` → `OnGet` | List or minimal form |
| `__handler=Save` → `OnPostSave` | Insert/Update 8 columns only |
| `__handler=Delete` → `OnPostDelete` | Soft inactive |

Missing: all child handlers, Excel, photo, benefit preview wiring.

---

## F. Database

### F1. Tables touched (Razor)

| Table | Ops |
|-------|-----|
| `tblEmployee` | SELECT / INSERT / UPDATE / DELETE |
| `tblEmployeeContact` | SELECT / DELETE+INSERT / DELETE (cascade) |
| `tblEmployeeAddress` | same |
| `tblEmployeeFamilyMember` | same |
| `tblEmployeeBank` | same |
| `tblEmployeeEducation` | same |
| `tblEmployeeCertificate` | same |
| `tblEmployeeDocument` | same (+ join `tblDocumentType`) |
| Lookup masters | SELECT only (see §C) |
| `tblDepartment` | SELECT |
| `tblUser` | SELECT |
| `tblBankMaster` | SELECT |
| `tblCurrency` | SELECT |
| `tblJob`, `tblExtension`, `tblWorkerLocation` | SELECT |

### F2. Key SQL patterns (not every line)

- **List:** `tblEmployee` + INNER `tblDepartment` + LEFT employment type/status/legal entity + OUTER APPLY primary PersonalMobile / OfficialEmail + data-scope filter.
- **Load edit:** wide SELECT of all employee columns including PhotoPath + load children ordered by SortOrder.
- **Save core:** parameterized UPDATE/INSERT of ~45 columns; INSERT returns `SCOPE_IDENTITY()`.
- **Children:** `DELETE FROM child WHERE EmployeeID=@ID` then INSERT with SortOrder + CreatedOn/CreatedByUserID.
- **Hard delete:** delete all 7 child tables then employee.
- **Preserve HR fields (profile-only):** SELECT org/employment/salary/status columns and overwrite posted values.

### F3. Stored procedures

**None.** All ad-hoc SQL via `SqlCommand`.

### F4. ASPX DB scope (current)

Only `tblEmployee` + `tblDepartment`. Soft update Status on delete. No child tables.

---

## G. Gap analysis — Razor present, ASPX missing or incomplete

Legend: **MISSING** = not present; **PARTIAL** = present but incomplete vs Razor.

### G1. UI / form fields (count: **~45 main fields missing**)

ASPX form currently has only: EmployeeCode, FirstName, LastName, DepartmentID, Designation, DateOfJoining, BasicSalary, Status.

| Gap | Status |
|-----|--------|
| Profile photo upload / remove / preview / ID card link | **MISSING** |
| FathersHusbandsName, DisplayName, NationalIDNumber | **MISSING** |
| DateOfBirth + Age display | **MISSING** |
| GenderID (ASPX model has unused Gender string; not on form) | **MISSING** |
| LanguageID, NationalityID, ReligionID, MaritalStatus, BloodGroupID, Domicile | **MISSING** |
| LegalEntity, BusinessUnit, Division, SalesTeam, CostCenter, Region, Location, WorkerLocation, Extension | **MISSING** |
| EmploymentStatusID, EmploymentTypeID, JobID, CityID, ProvinceID, SalesGroupID, GradeID | **MISSING** |
| UserID, Temporary/Permanent Responsible, WorkerCategory, WorkforceSegment | **MISSING** |
| EmploymentStartDate, ProbationPeriodDays/End, ConfirmationDate, tenure displays | **MISSING** |
| BenefitEntitlementID + benefits preview | **MISSING** |
| Required markers for LastName, Gender, Designation, DOJ, Salary (ASPX weaker) | **PARTIAL** |
| Section titles / breadcrumb / profile-only banner | **MISSING** |
| Form `enctype=multipart/form-data` | **MISSING** |

### G2. Profile tabs / child collections (count: **7 tabs = MISSING**)

| Feature | Status |
|---------|--------|
| Contact multi-grid + SaveContacts | **MISSING** |
| Address multi-grid + SaveAddresses | **MISSING** |
| Family multi-grid + SaveFamilyMembers | **MISSING** |
| Education multi-grid + SaveEducation | **MISSING** |
| Certificate multi-grid + file upload + SaveCertificates | **MISSING** |
| Documents multi-grid + file upload + SaveDocuments | **MISSING** |
| Bank multi-grid + SaveBanks | **MISSING** |
| JSON payload / dynamic row JS (app.js patterns) | **MISSING** |
| Default OfficialEmail / Current address rows | **MISSING** |

### G3. List view (count: **~10 gaps**)

| Feature | Status |
|---------|--------|
| Stat chips (Total/Active/Inactive) | **MISSING** |
| Client search box | **MISSING** |
| Columns: Legal Entity, Employment Type/Status, Mobile, Email, Salary display | **MISSING** / **PARTIAL** (salary in model but not shown) |
| Employee ID Card action | **MISSING** |
| Hard Delete vs soft Inactive | **PARTIAL** (behavior differs) |
| Excel import/export panel | **MISSING** (service injected but unused) |
| “View” vs “Edit” label for limited users | **MISSING** |
| Empty-state “Add First Employee” | **MISSING** |

### G4. Lookups (count: **~28 lookup sources missing**)

ASPX loads only Departments. All other §C lookups are **MISSING**.

### G5. Backend / permissions / rules (count: **~15 gaps**)

| Feature | Status |
|---------|--------|
| `IsProfileOnlyMode` + HR field lock + `PreserveHrManagedFields` | **MISSING** |
| `HasFullEmployeeAccess` / `CanEditCurrentRecord` UI gating | **MISSING** / **PARTIAL** |
| `CanAccessEmployeeMasterRecord` / sync message routing | **PARTIAL** |
| `CanDeleteEmployee` (uses create/admin instead) | **PARTIAL** |
| Hard delete + child cascade | **MISSING** (soft inactive only) |
| Wide INSERT/UPDATE column set | **PARTIAL** (8 columns) |
| Photo save/delete filesystem | **MISSING** |
| Tenure/probation auto-calc | **MISSING** |
| Duplicate code handling message | **MISSING** |
| Contact/phone validators | **MISSING** |
| Anti-forgery / separate section posts | **MISSING** (WebForms uses `__handler`) |
| Redirect-after-save to `editId` (stay on form) | **PARTIAL** (returns to list) |
| Upsert-by-EmployeeCode when ID=0 | **MISSING** |
| Gender name resolution from GenderID | **MISSING** |
| Data scope on list | **PRESENT** (good) |

### G6. Models (ASPX)

`EmployeeFormInput` missing ~40 properties vs `EmployeeInput`.  
`EmployeeViewModel` missing LegalEntityName, EmploymentType, EmploymentStatus.  
No child input classes at all.

---

## Gap summary totals (for implementation planning)

| Category | Approx. gap items |
|----------|-------------------|
| Main form fields / UI sections | **~45** |
| Profile tabs + child CRUD | **7 tabs / 7 handlers** |
| Lookups | **~28** |
| List/Excel/Card features | **~10** |
| Permission / profile-only / photo / validation behaviors | **~15** |
| **Estimated discrete gaps** | **~100+** |

### Main missing areas (priority for port)

1. **Full employee schema save/load** — all org, demographic, employment, tenure, benefit, photo columns on `tblEmployee`.
2. **Seven child collections** — contacts, addresses, family, education, certificates (+files), documents (+files), banks — with replace-all SQL and dedicated post handlers.
3. **Lookup infrastructure** — LoadLookup + specialized job/extension/worker-location/user/bank/currency/employee lookups.
4. **Profile-only mode** — lock HR fields client-side; preserve server-side on save.
5. **List richness** — search, stats, phone/email/legal entity columns, ID card link, Excel import/export, delete semantics decision (hard vs soft).
6. **Validation parity** — required set (code, names, gender, dept, designation, DOJ, salary), contact/phone validators, photo limits.
7. **UX** — multipart form, stay-on-edit after save, sectioned layout matching Razor.

### Already roughly present in ASPX

- Page gate via `EmployeeProfileAccessService.CanAccessEmployeeMasterPage`
- Create/edit permission checks on save
- List data-scope filter
- Basic Code/Name/Dept/Desig/DOJ/Salary/Status CRUD skeleton
- Soft-inactive delete (intentional difference — confirm product preference vs Razor hard delete)

---

## Implementation notes

- Prefer porting Razor `SaveEmployeeCore` + `ReplaceEmployee*` methods almost verbatim into WebForms code-behind (or shared service).
- Reuse/adapt `wwwroot/js/app.js` tab row builders if the ASPX page can host the same table IDs and hidden JSON fields.
- Confirm whether WebForms should keep **soft delete** or match Razor **hard delete**.
- `MasterExcelService` is already constructed in ASPX but unused — wire Export/Import like `_MasterExcelPanel`.
- Benefit preview depends on Razor page `BenefitEntitlementSetup`; for WebForms either call same API if dual-hosted, or add an ASHX/WebMethod equivalent.
)
