/*==============================================================================
  Production patch: Employee Master child tables (Contact / Address / Family / etc.)
  Safe to re-run.
==============================================================================*/

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.tblEmployeeContact', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblEmployeeContact (
        ContactID        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmployeeID       INT NOT NULL,
        ContactType      NVARCHAR(50)  NOT NULL,
        ContactName      NVARCHAR(100) NULL,
        Relationship     NVARCHAR(50)  NULL,
        ContactValue     NVARCHAR(255) NULL,
        IsPrimary        BIT NOT NULL CONSTRAINT DF_EmpContact_IsPrimary DEFAULT (0),
        SortOrder        INT NOT NULL CONSTRAINT DF_EmpContact_SortOrder DEFAULT (1),
        CreatedOn        DATETIME NOT NULL CONSTRAINT DF_EmpContact_CreatedOn DEFAULT (GETDATE()),
        ModifiedOn       DATETIME NULL,
        CreatedByUserID  INT NULL,
        ModifiedByUserID INT NULL
    );
END
GO

IF COL_LENGTH(N'dbo.tblEmployeeContact', N'ContactName') IS NULL
    ALTER TABLE dbo.tblEmployeeContact ADD ContactName NVARCHAR(100) NULL;
IF COL_LENGTH(N'dbo.tblEmployeeContact', N'Relationship') IS NULL
    ALTER TABLE dbo.tblEmployeeContact ADD Relationship NVARCHAR(50) NULL;
IF COL_LENGTH(N'dbo.tblEmployeeContact', N'SortOrder') IS NULL
    ALTER TABLE dbo.tblEmployeeContact ADD SortOrder INT NOT NULL CONSTRAINT DF_EmpContact_SortOrder2 DEFAULT (1);
IF COL_LENGTH(N'dbo.tblEmployeeContact', N'CreatedOn') IS NULL
    ALTER TABLE dbo.tblEmployeeContact ADD CreatedOn DATETIME NOT NULL CONSTRAINT DF_EmpContact_CreatedOn2 DEFAULT (GETDATE());
IF COL_LENGTH(N'dbo.tblEmployeeContact', N'ModifiedOn') IS NULL
    ALTER TABLE dbo.tblEmployeeContact ADD ModifiedOn DATETIME NULL;
IF COL_LENGTH(N'dbo.tblEmployeeContact', N'CreatedByUserID') IS NULL
    ALTER TABLE dbo.tblEmployeeContact ADD CreatedByUserID INT NULL;
IF COL_LENGTH(N'dbo.tblEmployeeContact', N'ModifiedByUserID') IS NULL
    ALTER TABLE dbo.tblEmployeeContact ADD ModifiedByUserID INT NULL;
GO

IF OBJECT_ID(N'dbo.tblEmployeeAddress', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tblEmployeeAddress', N'CreatedByUserID') IS NULL ALTER TABLE dbo.tblEmployeeAddress ADD CreatedByUserID INT NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeAddress', N'ModifiedByUserID') IS NULL ALTER TABLE dbo.tblEmployeeAddress ADD ModifiedByUserID INT NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeAddress', N'ModifiedOn') IS NULL ALTER TABLE dbo.tblEmployeeAddress ADD ModifiedOn DATETIME NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeAddress', N'CreatedOn') IS NULL ALTER TABLE dbo.tblEmployeeAddress ADD CreatedOn DATETIME NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeAddress', N'SortOrder') IS NULL ALTER TABLE dbo.tblEmployeeAddress ADD SortOrder INT NOT NULL CONSTRAINT DF_EmpAddress_SortOrder DEFAULT (1);
END
GO

IF OBJECT_ID(N'dbo.tblEmployeeFamilyMember', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tblEmployeeFamilyMember', N'CreatedByUserID') IS NULL ALTER TABLE dbo.tblEmployeeFamilyMember ADD CreatedByUserID INT NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeFamilyMember', N'ModifiedByUserID') IS NULL ALTER TABLE dbo.tblEmployeeFamilyMember ADD ModifiedByUserID INT NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeFamilyMember', N'ModifiedOn') IS NULL ALTER TABLE dbo.tblEmployeeFamilyMember ADD ModifiedOn DATETIME NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeFamilyMember', N'CreatedOn') IS NULL ALTER TABLE dbo.tblEmployeeFamilyMember ADD CreatedOn DATETIME NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeFamilyMember', N'SortOrder') IS NULL ALTER TABLE dbo.tblEmployeeFamilyMember ADD SortOrder INT NOT NULL CONSTRAINT DF_EmpFamily_SortOrder DEFAULT (1);
END
GO

IF OBJECT_ID(N'dbo.tblEmployeeBank', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tblEmployeeBank', N'CreatedByUserID') IS NULL ALTER TABLE dbo.tblEmployeeBank ADD CreatedByUserID INT NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeBank', N'ModifiedByUserID') IS NULL ALTER TABLE dbo.tblEmployeeBank ADD ModifiedByUserID INT NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeBank', N'ModifiedOn') IS NULL ALTER TABLE dbo.tblEmployeeBank ADD ModifiedOn DATETIME NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeBank', N'CreatedOn') IS NULL ALTER TABLE dbo.tblEmployeeBank ADD CreatedOn DATETIME NULL;
END
GO

IF OBJECT_ID(N'dbo.tblEmployeeEducation', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tblEmployeeEducation', N'CreatedByUserID') IS NULL ALTER TABLE dbo.tblEmployeeEducation ADD CreatedByUserID INT NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeEducation', N'ModifiedByUserID') IS NULL ALTER TABLE dbo.tblEmployeeEducation ADD ModifiedByUserID INT NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeEducation', N'ModifiedOn') IS NULL ALTER TABLE dbo.tblEmployeeEducation ADD ModifiedOn DATETIME NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeEducation', N'CreatedOn') IS NULL ALTER TABLE dbo.tblEmployeeEducation ADD CreatedOn DATETIME NULL;
END
GO

IF OBJECT_ID(N'dbo.tblEmployeeCertificate', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tblEmployeeCertificate', N'CreatedByUserID') IS NULL ALTER TABLE dbo.tblEmployeeCertificate ADD CreatedByUserID INT NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeCertificate', N'ModifiedByUserID') IS NULL ALTER TABLE dbo.tblEmployeeCertificate ADD ModifiedByUserID INT NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeCertificate', N'ModifiedOn') IS NULL ALTER TABLE dbo.tblEmployeeCertificate ADD ModifiedOn DATETIME NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeCertificate', N'CreatedOn') IS NULL ALTER TABLE dbo.tblEmployeeCertificate ADD CreatedOn DATETIME NULL;
END
GO

IF OBJECT_ID(N'dbo.tblEmployeeDocument', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tblEmployeeDocument', N'CreatedByUserID') IS NULL ALTER TABLE dbo.tblEmployeeDocument ADD CreatedByUserID INT NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeDocument', N'ModifiedByUserID') IS NULL ALTER TABLE dbo.tblEmployeeDocument ADD ModifiedByUserID INT NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeDocument', N'ModifiedOn') IS NULL ALTER TABLE dbo.tblEmployeeDocument ADD ModifiedOn DATETIME NULL;
    IF COL_LENGTH(N'dbo.tblEmployeeDocument', N'CreatedOn') IS NULL ALTER TABLE dbo.tblEmployeeDocument ADD CreatedOn DATETIME NULL;
END
GO

PRINT N'Employee child-table patch completed.';
PRINT N'Grant INSERT/UPDATE/DELETE on these tables to the IIS app SQL login if saves still fail with permission errors.';
GO
