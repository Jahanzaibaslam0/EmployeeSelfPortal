/*====================================================================================================
  HRMSDB – COMPLETE DATABASE REGENERATION SCRIPT
  -------------------------------------------------------------------------------------------------
  Purpose : Recreate the full HRMS database on a new server so the existing project works unchanged.
  This file is the single entry-point script for deployment.

  HOW TO RUN (pick one)
  ---------------------
  A) sqlcmd (recommended – pulls sibling schema scripts automatically):
       cd Database
       sqlcmd -S YourServerName -E -b -i HRMSDB_FullRegeneration.sql

  B) SSMS:
       1. Open this file
       2. Query menu → SQLCMD Mode (required for :r includes)
       3. Connect to the target instance → Execute

  C) Fully inlined single-file copy (no :r — one file for another server):
       powershell -ExecutionPolicy Bypass -File .\Build_HRMSDB_FullRegeneration.ps1
       Then run: HRMSDB_FullRegeneration_Standalone.sql

  Sources (unchanged originals in this folder):
       Script.sql | UserSecurity_Script.sql | LocalDB_MissingTables.sql
  Plus this file’s sections for DB create, PhotoPath patch, AppForm seed, admin placeholder.

  Login after first app start: admin / Admin@123
====================================================================================================*/

SET NOCOUNT ON;
GO

/*--------------------------------------------------------------------------------------------------
  SECTION 1 – DATABASE CREATION (portable default file paths)
--------------------------------------------------------------------------------------------------*/
USE master;
GO

IF DB_ID(N'HRMSDB') IS NULL
BEGIN
    CREATE DATABASE HRMSDB;
    PRINT N'Created database HRMSDB using default data/log paths.';
END
ELSE
    PRINT N'Database HRMSDB already exists – continuing with schema objects.';
GO

ALTER DATABASE HRMSDB SET RECOVERY SIMPLE;
GO

USE HRMSDB;
GO

/*--------------------------------------------------------------------------------------------------
  SECTION 2 – CORE SCHEMA (Script.sql)
  Script.sql also contains CREATE DATABASE; IF NOT EXISTS makes that a no-op.
--------------------------------------------------------------------------------------------------*/
PRINT N'========== SECTION 2: CORE SCHEMA (Script.sql) ==========';
GO
:r Script.sql
GO

/*--------------------------------------------------------------------------------------------------
  SECTION 3 – USER SECURITY
--------------------------------------------------------------------------------------------------*/
PRINT N'========== SECTION 3: USER SECURITY (UserSecurity_Script.sql) ==========';
GO
:r UserSecurity_Script.sql
GO

/*--------------------------------------------------------------------------------------------------
  SECTION 4 – EXTENDED TABLES / INDEXES / STORED PROCEDURES
--------------------------------------------------------------------------------------------------*/
PRINT N'========== SECTION 4: EXTENDED OBJECTS (LocalDB_MissingTables.sql) ==========';
GO
:r LocalDB_MissingTables.sql
GO

USE HRMSDB;
GO

/*--------------------------------------------------------------------------------------------------
  SECTION 5 – APP-REQUIRED COLUMN PATCHES (existing project dependencies)
--------------------------------------------------------------------------------------------------*/
PRINT N'========== SECTION 5: APP-REQUIRED COLUMN PATCHES ==========';
GO

IF OBJECT_ID(N'tblEmployee', N'U') IS NOT NULL
   AND COL_LENGTH(N'tblEmployee', N'PhotoPath') IS NULL
    ALTER TABLE tblEmployee ADD PhotoPath NVARCHAR(500) NULL;
GO

IF OBJECT_ID(N'tblEmployeeContact', N'U') IS NOT NULL AND COL_LENGTH(N'tblEmployeeContact', N'CreatedByUserID') IS NULL
    ALTER TABLE tblEmployeeContact ADD CreatedByUserID INT NULL;
IF OBJECT_ID(N'tblEmployeeAddress', N'U') IS NOT NULL AND COL_LENGTH(N'tblEmployeeAddress', N'CreatedByUserID') IS NULL
    ALTER TABLE tblEmployeeAddress ADD CreatedByUserID INT NULL;
IF OBJECT_ID(N'tblEmployeeFamilyMember', N'U') IS NOT NULL AND COL_LENGTH(N'tblEmployeeFamilyMember', N'CreatedByUserID') IS NULL
    ALTER TABLE tblEmployeeFamilyMember ADD CreatedByUserID INT NULL;
IF OBJECT_ID(N'tblEmployeeBank', N'U') IS NOT NULL AND COL_LENGTH(N'tblEmployeeBank', N'CreatedByUserID') IS NULL
    ALTER TABLE tblEmployeeBank ADD CreatedByUserID INT NULL;
IF OBJECT_ID(N'tblEmployeeEducation', N'U') IS NOT NULL AND COL_LENGTH(N'tblEmployeeEducation', N'CreatedByUserID') IS NULL
    ALTER TABLE tblEmployeeEducation ADD CreatedByUserID INT NULL;
IF OBJECT_ID(N'tblEmployeeCertificate', N'U') IS NOT NULL AND COL_LENGTH(N'tblEmployeeCertificate', N'CreatedByUserID') IS NULL
    ALTER TABLE tblEmployeeCertificate ADD CreatedByUserID INT NULL;
IF OBJECT_ID(N'tblEmployeeDocument', N'U') IS NOT NULL AND COL_LENGTH(N'tblEmployeeDocument', N'CreatedByUserID') IS NULL
    ALTER TABLE tblEmployeeDocument ADD CreatedByUserID INT NULL;
GO

/*--------------------------------------------------------------------------------------------------
  SECTION 6 – SEED: APPLICATION FORMS (mirrors AppForms.All / StartupMigrations.Seed)
--------------------------------------------------------------------------------------------------*/
PRINT N'========== SECTION 6: SEED DATA – tblAppForm ==========';
GO

IF OBJECT_ID(N'tblAppForm', N'U') IS NOT NULL
BEGIN
    ;WITH Forms(FormKey, FormName, PagePath, Category, SortOrder) AS (
        SELECT * FROM (VALUES
            (N'Home', N'Home', N'/Home.aspx', N'Transactions', 0),
            (N'Dashboard', N'HRMS Dashboard', N'/Dashboard.aspx', N'Transactions', 1),
            (N'UserProfile', N'My Profile', N'/UserProfile.aspx', N'Transactions', 2),
            (N'MyDocuments', N'My Documents', N'/MyDocuments.aspx', N'Transactions', 3),
            (N'EmployeeMaster', N'Employee Master', N'/EmployeeMaster.aspx', N'Transactions', 4),
            (N'PositionMaster', N'Position Master', N'/PositionMaster.aspx', N'Transactions', 4),
            (N'PositionHierarchy', N'Position Hierarchy', N'/PositionHierarchy.aspx', N'Transactions', 5),
            (N'EmployeeReport', N'Internal Employee Directory', N'/EmployeeReport.aspx', N'Transactions', 6),
            (N'QuickLinks', N'Quick Links', N'/QuickLinks.aspx', N'Transactions', 7),
            (N'Notifications', N'Notifications', N'/Notifications.aspx', N'Transactions', 8),
            (N'Memorandums', N'Memorandums', N'/Memorandums.aspx', N'Transactions', 9),
            (N'ExpenseMaster', N'Expense Process', N'/ExpenseMaster.aspx', N'Transactions', 10),
            (N'PerformanceMaster', N'Employee Performance', N'/PerformanceMaster.aspx', N'Transactions', 11),
            (N'TrainingMaster', N'Employee Training', N'/TrainingMaster.aspx', N'Transactions', 12),
            (N'RecruitmentMaster', N'Recruitment Process', N'/RecruitmentMaster.aspx', N'Transactions', 13),
            (N'LeaveMaster', N'Leave Management', N'/LeaveMaster.aspx', N'Transactions', 14),
            (N'CustomerMaster', N'Customer Master', N'/CustomerMaster.aspx', N'Transactions', 15),
            (N'ContactMaster', N'Contact Master', N'/ContactMaster.aspx', N'Transactions', 16),
            (N'ProductMaster', N'Product Master', N'/ProductMaster.aspx', N'Transactions', 17),
            (N'InvoiceMaster', N'Invoice Master', N'/InvoiceMaster.aspx', N'Transactions', 18),
            (N'DivisionSetup', N'Division Setup', N'/DivisionSetup.aspx', N'Organization Setup', 10),
            (N'BusinessSegmentSetup', N'Business Segment Setup', N'/BusinessSegmentSetup.aspx', N'Organization Setup', 11),
            (N'BusinessUnitSetup', N'Business Unit Setup', N'/BusinessUnitSetup.aspx', N'Organization Setup', 12),
            (N'WorkforceSegmentSetup', N'Workforce Segment Setup', N'/WorkforceSegmentSetup.aspx', N'Organization Setup', 13),
            (N'UnitSetup', N'Unit Setup', N'/UnitSetup.aspx', N'Organization Setup', 14),
            (N'WingSetup', N'Wing Setup', N'/WingSetup.aspx', N'Organization Setup', 15),
            (N'GenderSetup', N'Gender Setup', N'/GenderSetup.aspx', N'Organization Setup', 16),
            (N'ReligionSetup', N'Religion Setup', N'/ReligionSetup.aspx', N'Organization Setup', 17),
            (N'NationalitySetup', N'Nationality Setup', N'/NationalitySetup.aspx', N'Organization Setup', 18),
            (N'LanguageSetup', N'Language Setup', N'/LanguageSetup.aspx', N'Organization Setup', 19),
            (N'BankSetup', N'Bank Master Setup', N'/BankSetup.aspx', N'Organization Setup', 20),
            (N'BankGroupSetup', N'Bank Group Setup', N'/BankGroupSetup.aspx', N'Organization Setup', 21),
            (N'CurrencySetup', N'Currency Setup', N'/CurrencySetup.aspx', N'Organization Setup', 22),
            (N'UnitOfMeasureSetup', N'Unit of Measure Setup', N'/UnitOfMeasureSetup.aspx', N'Organization Setup', 23),
            (N'CostCenterSetup', N'Cost Center Setup', N'/CostCenterSetup.aspx', N'Organization Setup', 24),
            (N'SkillSetup', N'Skill Setup', N'/SkillSetup.aspx', N'Organization Setup', 25),
            (N'LegalEntitySetup', N'Legal Entity Setup', N'/LegalEntitySetup.aspx', N'Organization Setup', 26),
            (N'SalesTeamSetup', N'Sales Team Setup', N'/SalesTeamSetup.aspx', N'Organization Setup', 27),
            (N'WorkLocationTypeSetup', N'Work Location Type Setup', N'/WorkLocationTypeSetup.aspx', N'Organization Setup', 28),
            (N'WorkArrangementSetup', N'Work Arrangement Setup', N'/WorkArrangementSetup.aspx', N'Organization Setup', 29),
            (N'ExtensionSetup', N'Extension Master Setup', N'/ExtensionSetup.aspx', N'Organization Setup', 30),
            (N'CitySetup', N'City Setup', N'/CitySetup.aspx', N'Organization Setup', 31),
            (N'ProvinceSetup', N'Province Setup', N'/ProvinceSetup.aspx', N'Organization Setup', 32),
            (N'SalesGroupSetup', N'Sales Group Setup', N'/SalesGroupSetup.aspx', N'Organization Setup', 33),
            (N'DepartmentSetup', N'Department Setup', N'/DepartmentSetup.aspx', N'Organization Setup', 34),
            (N'RegionSetup', N'Region Setup', N'/RegionSetup.aspx', N'Organization Setup', 35),
            (N'LocationSetup', N'Location Setup', N'/LocationSetup.aspx', N'Organization Setup', 36),
            (N'SoftwareLinkSetup', N'Software Link Setup', N'/SoftwareLinkSetup.aspx', N'Organization Setup', 37),
            (N'NotificationSetup', N'Notification Setup', N'/NotificationSetup.aspx', N'Organization Setup', 38),
            (N'MemorandumSetup', N'Memorandum Setup', N'/MemorandumSetup.aspx', N'Organization Setup', 39),
            (N'ImageGallerySetup', N'Image Gallery Setup', N'/ImageGallerySetup.aspx', N'Organization Setup', 40),
            (N'GradeSetup', N'Grade Setup', N'/GradeSetup.aspx', N'Employee Setup', 33),
            (N'EmploymentTypeSetup', N'Employment Type Setup', N'/EmploymentTypeSetup.aspx', N'Employee Setup', 34),
            (N'DesignationLevelSetup', N'Designation Level Setup', N'/DesignationLevelSetup.aspx', N'Employee Setup', 35),
            (N'TitleSetup', N'Title Setup', N'/TitleSetup.aspx', N'Employee Setup', 36),
            (N'EmploymentStatusSetup', N'Employment Status Setup', N'/EmploymentStatusSetup.aspx', N'Employee Setup', 37),
            (N'BenefitSetup', N'Benefit Setup', N'/BenefitSetup.aspx', N'Employee Setup', 38),
            (N'BenefitEntitlementSetup', N'Benefit Entitlement Setup', N'/BenefitEntitlementSetup.aspx', N'Employee Setup', 39),
            (N'ExpenseCategorySetup', N'Expense Category Setup', N'/ExpenseCategorySetup.aspx', N'Employee Setup', 40),
            (N'LeaveCategorySetup', N'Leave Category Setup', N'/LeaveCategorySetup.aspx', N'Employee Setup', 41),
            (N'BloodGroupSetup', N'Blood Group Setup', N'/BloodGroupSetup.aspx', N'Employee Setup', 42),
            (N'WorkerCategorySetup', N'Worker Category Setup', N'/WorkerCategorySetup.aspx', N'Employee Setup', 43),
            (N'JobSetup', N'Job Setup', N'/JobSetup.aspx', N'Employee Setup', 44),
            (N'WorkerLocationSetup', N'Worker Location Setup', N'/WorkerLocationSetup.aspx', N'Employee Setup', 45),
            (N'DocumentTypeSetup', N'Document Type Setup', N'/DocumentTypeSetup.aspx', N'Employee Setup', 46),
            (N'ModeOfDeliverySetup', N'Mode of Delivery Setup', N'/ModeOfDeliverySetup.aspx', N'Customer Setup', 60),
            (N'MethodOfPaymentSetup', N'Method of Payment Setup', N'/MethodOfPaymentSetup.aspx', N'Customer Setup', 61),
            (N'CustomerGroupSetup', N'Customer Group Setup', N'/CustomerGroupSetup.aspx', N'Customer Setup', 62),
            (N'TermsOfPaymentSetup', N'Terms of Payment Setup', N'/TermsOfPaymentSetup.aspx', N'Customer Setup', 63),
            (N'CustomerClassSetup', N'Customer Class Setup', N'/CustomerClassSetup.aspx', N'Customer Setup', 64),
            (N'BillPreferenceSetup', N'Bill Preference Setup', N'/BillPreferenceSetup.aspx', N'Customer Setup', 65),
            (N'TaxGroupSetup', N'Tax Group Setup', N'/TaxGroupSetup.aspx', N'Customer Setup', 66),
            (N'FBRStatusSetup', N'FBR Status Setup', N'/FBRStatusSetup.aspx', N'Customer Setup', 67),
            (N'ProductNatureSetup', N'Product Nature Setup', N'/ProductNatureSetup.aspx', N'Product Setup', 70),
            (N'InventoryTypeSetup', N'Inventory Type Setup', N'/InventoryTypeSetup.aspx', N'Product Setup', 71),
            (N'ItemRegisteredSetup', N'Item Registered Setup', N'/ItemRegisteredSetup.aspx', N'Product Setup', 72),
            (N'BrandCodeSetup', N'Brand Code Setup', N'/BrandCodeSetup.aspx', N'Product Setup', 73),
            (N'BrandGroupSetup', N'Brand Group Setup', N'/BrandGroupSetup.aspx', N'Product Setup', 74),
            (N'ProductGroupSetup', N'Product Group Setup', N'/ProductGroupSetup.aspx', N'Product Setup', 75),
            (N'ProductSalesGroupSetup', N'Sales Group Setup', N'/ProductSalesGroupSetup.aspx', N'Product Setup', 76),
            (N'ItemGroupSetup', N'Item Group Setup', N'/ItemGroupSetup.aspx', N'Product Setup', 77),
            (N'SalesCategorySetup', N'Sales Category Setup', N'/SalesCategorySetup.aspx', N'Product Setup', 78),
            (N'ProductDivisionSetup', N'Division Setup', N'/ProductDivisionSetup.aspx', N'Product Setup', 79),
            (N'ProductTeamSetup', N'Team Setup', N'/ProductTeamSetup.aspx', N'Product Setup', 80),
            (N'HSCodeSetup', N'HS Code Setup', N'/HSCodeSetup.aspx', N'Product Setup', 81),
            (N'UserSetup', N'User Setup', N'/UserSetup.aspx', N'Security', 50),
            (N'UserRightsSetup', N'User Rights Setup', N'/UserRightsSetup.aspx', N'Security', 51),
            (N'AuditReport', N'Audit Log Report', N'/AuditReport.aspx', N'Security', 52)
        ) v(FormKey, FormName, PagePath, Category, SortOrder)
    )
    INSERT INTO tblAppForm (FormKey, FormName, PagePath, Category, SortOrder, IsActive, CreatedOn)
    SELECT f.FormKey, f.FormName, f.PagePath, f.Category, f.SortOrder, 1, GETDATE()
    FROM Forms f
    WHERE NOT EXISTS (SELECT 1 FROM tblAppForm a WHERE a.FormKey = f.FormKey);
END
GO

/*--------------------------------------------------------------------------------------------------
  SECTION 7 – SEED: ADMIN PLACEHOLDER + EMPLOYEE LINK + DATA SCOPE
  PasswordHash is finalized by AuthService.EnsureUserTableAndAdmin on first app start
  (admin / Admin@123 via PasswordHelper PBKDF2-SHA256).
--------------------------------------------------------------------------------------------------*/
PRINT N'========== SECTION 7: SEED DATA – ADMIN / LINKS ==========';
GO

IF OBJECT_ID(N'tblUser', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM tblUser WHERE Username = N'admin')
BEGIN
    INSERT INTO tblUser (UserCode, Username, PasswordHash, FullName, Email, IsActive, IsAdmin, CreatedOn)
    VALUES (N'GB-US-00001', N'admin', N'PENDING_APP_HASH', N'System Administrator', N'admin@hrms.local', 1, 1, GETDATE());
END
GO

IF OBJECT_ID(N'tblEmployee', N'U') IS NOT NULL
   AND OBJECT_ID(N'tblUser', N'U') IS NOT NULL
   AND COL_LENGTH(N'tblEmployee', N'UserID') IS NOT NULL
BEGIN
    DECLARE @AdminUserId INT = (SELECT TOP 1 UserID FROM tblUser WHERE Username = N'admin');
    DECLARE @EmpId INT = (SELECT TOP 1 EmployeeID FROM tblEmployee WHERE EmployeeCode = N'EMP-00001');
    IF @AdminUserId IS NOT NULL AND @EmpId IS NOT NULL
        UPDATE tblEmployee SET UserID = @AdminUserId
        WHERE EmployeeID = @EmpId AND (UserID IS NULL OR UserID = 0);
END
GO

IF OBJECT_ID(N'tblUserDataScope', N'U') IS NOT NULL AND OBJECT_ID(N'tblUser', N'U') IS NOT NULL
BEGIN
    DECLARE @Uid INT = (SELECT TOP 1 UserID FROM tblUser WHERE Username = N'admin');
    IF @Uid IS NOT NULL AND NOT EXISTS (SELECT 1 FROM tblUserDataScope WHERE UserID = @Uid)
        INSERT INTO tblUserDataScope (UserID, ScopeMode, IncludeUnassignedDepartment, IncludeUnassignedLocation, IsActive, CreatedOn)
        VALUES (@Uid, N'All', 1, 1, 1, GETDATE());
END
GO

/*--------------------------------------------------------------------------------------------------
  SECTION 8 – VERIFICATION
--------------------------------------------------------------------------------------------------*/
PRINT N'========== SECTION 8: VERIFICATION ==========';
GO

SELECT
    (SELECT COUNT(*) FROM sys.tables WHERE name LIKE N'tbl%') AS TableCount,
    (SELECT COUNT(*) FROM sys.procedures WHERE name LIKE N'sp_%') AS ProcedureCount,
    (SELECT COUNT(*) FROM tblAppForm) AS AppFormCount,
    (SELECT COUNT(*) FROM tblUser WHERE Username = N'admin') AS AdminUserCount;

PRINT N'HRMSDB_FullRegeneration.sql completed successfully.';
PRINT N'Next: update Web.config connection string, start the app once, login admin / Admin@123.';
GO
