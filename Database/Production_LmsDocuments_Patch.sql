/*==============================================================================
  LMS Knowledge Documents – schema + form seed (safe to re-run)
==============================================================================*/
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.tblLmsDocument', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblLmsDocument (
        DocumentID        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Title             NVARCHAR(200) NOT NULL,
        Description       NVARCHAR(1000) NULL,
        Category          NVARCHAR(50)  NOT NULL,  -- General, Department, SystemManual, SOP, Policy, Reference
        AccessScope       NVARCHAR(50)  NOT NULL
            CONSTRAINT DF_tblLmsDocument_AccessScope DEFAULT (N'Organization'),
            -- Organization | Department | Job | Restricted
        DepartmentID      INT NULL,
        JobID             INT NULL,
        DocumentPath      NVARCHAR(500) NULL,
        OriginalFileName  NVARCHAR(255) NULL,
        VersionLabel      NVARCHAR(50)  NULL,
        EffectiveDate     DATE NULL,
        ExpiryDate        DATE NULL,
        IsActive          BIT NOT NULL
            CONSTRAINT DF_tblLmsDocument_IsActive DEFAULT (1),
        CreatedOn         DATETIME NOT NULL
            CONSTRAINT DF_tblLmsDocument_CreatedOn DEFAULT (GETDATE()),
        ModifiedOn        DATETIME NULL,
        CreatedByUserID   INT NULL,
        ModifiedByUserID  INT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.tblLmsDocumentAccess', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblLmsDocumentAccess (
        AccessID          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DocumentID        INT NOT NULL
            REFERENCES dbo.tblLmsDocument(DocumentID),
        GrantType         NVARCHAR(50) NOT NULL, -- Employee | Department | Job
        EmployeeID        INT NULL,
        DepartmentID      INT NULL,
        JobID             INT NULL,
        IsActive          BIT NOT NULL
            CONSTRAINT DF_tblLmsDocumentAccess_IsActive DEFAULT (1),
        CreatedOn         DATETIME NOT NULL
            CONSTRAINT DF_tblLmsDocumentAccess_CreatedOn DEFAULT (GETDATE()),
        CreatedByUserID   INT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.tblAppForm', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.tblAppForm WHERE FormKey = N'LmsLibrary')
        INSERT INTO dbo.tblAppForm (FormKey, FormName, PagePath, Category, SortOrder, IsActive, CreatedOn)
        VALUES (N'LmsLibrary', N'Knowledge Library', N'/LmsLibrary.aspx', N'Transaction', 21, 1, GETDATE());

    IF NOT EXISTS (SELECT 1 FROM dbo.tblAppForm WHERE FormKey = N'LmsDocumentSetup')
        INSERT INTO dbo.tblAppForm (FormKey, FormName, PagePath, Category, SortOrder, IsActive, CreatedOn)
        VALUES (N'LmsDocumentSetup', N'LMS Document Setup', N'/LmsDocumentSetup.aspx', N'Organization Setup', 41, 1, GETDATE());
END
GO

PRINT N'tblLmsDocument / tblLmsDocumentAccess / AppForm seed completed.';
GO
