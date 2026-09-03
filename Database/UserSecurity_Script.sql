-- =============================================
-- HRMS – User Security Script
-- Creates login / rights / data-scope tables for a new database.
-- Run against the target database (e.g. USE YourDatabase; GO).
-- Compatible with app migrations in Program.cs.
-- =============================================

SET NOCOUNT ON;
GO

-- =============================================
-- 1. tblUser – application login accounts
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblUser' AND type = 'U')
BEGIN
    CREATE TABLE tblUser (
        UserID            INT IDENTITY(1,1) PRIMARY KEY,
        UserCode          NVARCHAR(20)  NULL,
        Username          NVARCHAR(50)  NOT NULL UNIQUE,
        PasswordHash      NVARCHAR(200) NOT NULL,
        FullName          NVARCHAR(100) NOT NULL,
        Email             NVARCHAR(100) NULL,
        IsActive          BIT           NOT NULL DEFAULT 1,
        IsAdmin           BIT           NOT NULL DEFAULT 0,
        CreatedOn         DATETIME      NOT NULL DEFAULT GETDATE(),
        ModifiedOn        DATETIME      NULL,
        CreatedByUserID   INT           NULL,
        ModifiedByUserID  INT           NULL
    );
END
GO

-- Ensure audit columns exist on older databases
IF COL_LENGTH('tblUser', 'CreatedByUserID') IS NULL
    ALTER TABLE tblUser ADD CreatedByUserID INT NULL;
IF COL_LENGTH('tblUser', 'ModifiedByUserID') IS NULL
    ALTER TABLE tblUser ADD ModifiedByUserID INT NULL;
GO

-- =============================================
-- 2. tblAppForm – registry of permission-controlled forms
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblAppForm' AND type = 'U')
BEGIN
    CREATE TABLE tblAppForm (
        FormID            INT IDENTITY(1,1) PRIMARY KEY,
        FormKey           NVARCHAR(80)  NOT NULL UNIQUE,
        FormName          NVARCHAR(150) NOT NULL,
        PagePath          NVARCHAR(200) NOT NULL,
        Category          NVARCHAR(80)  NOT NULL,
        SortOrder         INT           NOT NULL DEFAULT 0,
        IsActive          BIT           NOT NULL DEFAULT 1,
        CreatedOn         DATETIME      NOT NULL DEFAULT GETDATE(),
        ModifiedOn        DATETIME      NULL,
        CreatedByUserID   INT           NULL,
        ModifiedByUserID  INT           NULL
    );
END
GO

IF COL_LENGTH('tblAppForm', 'CreatedByUserID') IS NULL
    ALTER TABLE tblAppForm ADD CreatedByUserID INT NULL;
IF COL_LENGTH('tblAppForm', 'ModifiedByUserID') IS NULL
    ALTER TABLE tblAppForm ADD ModifiedByUserID INT NULL;
IF COL_LENGTH('tblAppForm', 'CreatedOn') IS NULL
    ALTER TABLE tblAppForm ADD CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblAppForm_CreatedOn DEFAULT GETDATE();
IF COL_LENGTH('tblAppForm', 'ModifiedOn') IS NULL
    ALTER TABLE tblAppForm ADD ModifiedOn DATETIME NULL;
GO

-- =============================================
-- 3. tblUserPermission – per-user form rights
-- FormKey matches tblAppForm.FormKey
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblUserPermission' AND type = 'U')
BEGIN
    CREATE TABLE tblUserPermission (
        UserPermissionID  INT IDENTITY(1,1) PRIMARY KEY,
        UserID            INT           NOT NULL,
        FormKey           NVARCHAR(80)  NOT NULL,
        CanRead           BIT           NOT NULL DEFAULT 0,
        CanWrite          BIT           NOT NULL DEFAULT 0,
        CanDelete         BIT           NOT NULL DEFAULT 0,
        CanApprove        BIT           NOT NULL DEFAULT 0,
        CanExport         BIT           NOT NULL DEFAULT 0,
        CreatedOn         DATETIME      NOT NULL DEFAULT GETDATE(),
        ModifiedOn        DATETIME      NULL,
        CreatedByUserID   INT           NULL,
        ModifiedByUserID  INT           NULL,
        CONSTRAINT UQ_UserForm UNIQUE (UserID, FormKey)
    );
END
GO

IF COL_LENGTH('tblUserPermission', 'CanApprove') IS NULL
    ALTER TABLE tblUserPermission ADD CanApprove BIT NOT NULL CONSTRAINT DF_tblUserPermission_CanApprove DEFAULT 0;
IF COL_LENGTH('tblUserPermission', 'CanExport') IS NULL
    ALTER TABLE tblUserPermission ADD CanExport BIT NOT NULL CONSTRAINT DF_tblUserPermission_CanExport DEFAULT 0;
IF COL_LENGTH('tblUserPermission', 'CreatedByUserID') IS NULL
    ALTER TABLE tblUserPermission ADD CreatedByUserID INT NULL;
IF COL_LENGTH('tblUserPermission', 'ModifiedByUserID') IS NULL
    ALTER TABLE tblUserPermission ADD ModifiedByUserID INT NULL;
IF COL_LENGTH('tblUserPermission', 'CreatedOn') IS NULL
    ALTER TABLE tblUserPermission ADD CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblUserPermission_CreatedOn DEFAULT GETDATE();
IF COL_LENGTH('tblUserPermission', 'ModifiedOn') IS NULL
    ALTER TABLE tblUserPermission ADD ModifiedOn DATETIME NULL;
GO

-- =============================================
-- 4. tblUserDataScope – row-level data access (1 row per user)
-- ScopeMode: OwnOnly | Assigned | All
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblUserDataScope' AND type = 'U')
BEGIN
    CREATE TABLE tblUserDataScope (
        UserDataScopeID             INT IDENTITY(1,1) PRIMARY KEY,
        UserID                      INT           NOT NULL UNIQUE,
        ScopeMode                   NVARCHAR(20)  NOT NULL DEFAULT 'OwnOnly',
        IncludeUnassignedDepartment BIT           NOT NULL DEFAULT 0,
        IncludeUnassignedLocation   BIT           NOT NULL DEFAULT 0,
        IsActive                    BIT           NOT NULL DEFAULT 1,
        CreatedOn                   DATETIME      NOT NULL DEFAULT GETDATE(),
        ModifiedOn                  DATETIME      NULL,
        CreatedByUserID             INT           NULL,
        ModifiedByUserID            INT           NULL
    );
END
GO

-- =============================================
-- 5. tblUserDepartmentScope – departments allowed when ScopeMode = Assigned
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblUserDepartmentScope' AND type = 'U')
BEGIN
    CREATE TABLE tblUserDepartmentScope (
        UserDepartmentScopeID INT IDENTITY(1,1) PRIMARY KEY,
        UserID                INT NOT NULL,
        DepartmentID          INT NOT NULL,
        CreatedOn             DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT UQ_UserDepartmentScope UNIQUE (UserID, DepartmentID)
    );
END
GO

-- =============================================
-- 6. tblUserLocationScope – locations allowed when ScopeMode = Assigned
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblUserLocationScope' AND type = 'U')
BEGIN
    CREATE TABLE tblUserLocationScope (
        UserLocationScopeID INT IDENTITY(1,1) PRIMARY KEY,
        UserID              INT NOT NULL,
        LocationID          INT NOT NULL,
        CreatedOn           DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT UQ_UserLocationScope UNIQUE (UserID, LocationID)
    );
END
GO

-- =============================================
-- 7. Optional FKs to tblUser (safe if already present)
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserPermission_User')
    AND OBJECT_ID('tblUserPermission') IS NOT NULL
    AND OBJECT_ID('tblUser') IS NOT NULL
    ALTER TABLE tblUserPermission
        ADD CONSTRAINT FK_UserPermission_User
        FOREIGN KEY (UserID) REFERENCES tblUser(UserID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserDataScope_User')
    AND OBJECT_ID('tblUserDataScope') IS NOT NULL
    AND OBJECT_ID('tblUser') IS NOT NULL
    ALTER TABLE tblUserDataScope
        ADD CONSTRAINT FK_UserDataScope_User
        FOREIGN KEY (UserID) REFERENCES tblUser(UserID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserDeptScope_User')
    AND OBJECT_ID('tblUserDepartmentScope') IS NOT NULL
    AND OBJECT_ID('tblUser') IS NOT NULL
    ALTER TABLE tblUserDepartmentScope
        ADD CONSTRAINT FK_UserDeptScope_User
        FOREIGN KEY (UserID) REFERENCES tblUser(UserID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserLocScope_User')
    AND OBJECT_ID('tblUserLocationScope') IS NOT NULL
    AND OBJECT_ID('tblUser') IS NOT NULL
    ALTER TABLE tblUserLocationScope
        ADD CONSTRAINT FK_UserLocScope_User
        FOREIGN KEY (UserID) REFERENCES tblUser(UserID);
GO

-- =============================================
-- 8. Employee ↔ User link (column on tblEmployee)
-- Only applied when tblEmployee already exists.
-- =============================================
IF OBJECT_ID('tblEmployee') IS NOT NULL
   AND COL_LENGTH('tblEmployee', 'UserID') IS NULL
    ALTER TABLE tblEmployee ADD UserID INT NULL;
GO

-- =============================================
-- 9. Default admin user
-- Password must be hashed with PasswordHelper (PBKDF2).
-- Prefer letting the HRMS app seed admin on first startup:
--   Username: admin
--   Password: Admin@123
--   UserCode: GB-US-00001
--
-- Do NOT insert a plain-text password into PasswordHash.
-- =============================================
-- Example (replace @Hash with a valid PasswordHelper hash before running):
--
-- IF NOT EXISTS (SELECT 1 FROM tblUser WHERE Username = 'admin')
-- BEGIN
--     INSERT INTO tblUser (UserCode, Username, PasswordHash, FullName, Email, IsActive, IsAdmin, CreatedOn)
--     VALUES ('GB-US-00001', 'admin', @Hash, 'System Administrator', 'admin@hrms.local', 1, 1, GETDATE());
-- END
-- GO

PRINT 'UserSecurity_Script.sql completed successfully.';
GO
