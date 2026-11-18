/*==============================================================================
  Employee Discipline & Non-Conformance – schema + form seed (safe to re-run)
==============================================================================*/
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.tblEmployeeDiscipline', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblEmployeeDiscipline (
        DisciplineID              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CaseNumber                NVARCHAR(30)  NOT NULL,
        EmployeeID                INT NOT NULL,
        IncidentDate              DATE NULL,
        ReportedDate              DATE NULL,
        DisciplineType            NVARCHAR(100) NOT NULL,
        DisciplineCategory        NVARCHAR(100) NULL,
        Description               NVARCHAR(MAX) NULL,
        InvestigationDetails      NVARCHAR(MAX) NULL,
        InvestigationFindings     NVARCHAR(MAX) NULL,
        EmployeeExplanation       NVARCHAR(MAX) NULL,
        FindingsObservations      NVARCHAR(MAX) NULL,
        FindingRemarks            NVARCHAR(MAX) NULL,
        CorrectiveActions         NVARCHAR(MAX) NULL,
        DisciplinaryAction        NVARCHAR(200) NULL,
        ActionDate                DATE NULL,
        ActionResponsiblePerson   NVARCHAR(200) NULL,
        CaseStatus                NVARCHAR(50)  NOT NULL
            CONSTRAINT DF_tblEmployeeDiscipline_Status DEFAULT (N'Open'),
        FinalDecision             NVARCHAR(MAX) NULL,
        ManagementRemarks         NVARCHAR(MAX) NULL,
        IncludeInPerformance      BIT NOT NULL
            CONSTRAINT DF_tblEmployeeDiscipline_InclPerf DEFAULT (1),
        IsActive                  BIT NOT NULL
            CONSTRAINT DF_tblEmployeeDiscipline_IsActive DEFAULT (1),
        CreatedOn                 DATETIME NOT NULL
            CONSTRAINT DF_tblEmployeeDiscipline_CreatedOn DEFAULT (GETDATE()),
        ModifiedOn                DATETIME NULL,
        CreatedByUserID           INT NULL,
        ModifiedByUserID          INT NULL,
        ClosedOn                  DATETIME NULL,
        ClosedByUserID            INT NULL
    );
    CREATE UNIQUE INDEX UX_tblEmployeeDiscipline_CaseNumber ON dbo.tblEmployeeDiscipline(CaseNumber);
    CREATE INDEX IX_tblEmployeeDiscipline_Employee ON dbo.tblEmployeeDiscipline(EmployeeID, CaseStatus);
    CREATE INDEX IX_tblEmployeeDiscipline_Status ON dbo.tblEmployeeDiscipline(CaseStatus, IncidentDate);
END
GO

IF OBJECT_ID(N'dbo.tblEmployeeDisciplineAttachment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblEmployeeDisciplineAttachment (
        AttachmentID     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DisciplineID     INT NOT NULL REFERENCES dbo.tblEmployeeDiscipline(DisciplineID),
        DocumentPath     NVARCHAR(500) NOT NULL,
        OriginalFileName NVARCHAR(255) NULL,
        UploadedOn       DATETIME NOT NULL
            CONSTRAINT DF_tblEmpDiscAtt_UploadedOn DEFAULT (GETDATE()),
        UploadedByUserID INT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.tblEmployeeDisciplineAudit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblEmployeeDisciplineAudit (
        AuditID          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DisciplineID     INT NOT NULL REFERENCES dbo.tblEmployeeDiscipline(DisciplineID),
        ActionType       NVARCHAR(50)  NOT NULL,
        ActionSummary    NVARCHAR(500) NULL,
        Remarks          NVARCHAR(2000) NULL,
        ActionByUserID   INT NULL,
        ActionByName     NVARCHAR(200) NULL,
        ActionOn         DATETIME NOT NULL
            CONSTRAINT DF_tblEmpDiscAudit_On DEFAULT (GETDATE())
    );
    CREATE INDEX IX_tblEmployeeDisciplineAudit_Case ON dbo.tblEmployeeDisciplineAudit(DisciplineID, ActionOn);
END
GO

IF OBJECT_ID(N'dbo.tblEmployeeDisciplineFinding', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblEmployeeDisciplineFinding (
        FindingID        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmployeeID       INT NOT NULL,
        DisciplineID     INT NULL REFERENCES dbo.tblEmployeeDiscipline(DisciplineID),
        FindingSource    NVARCHAR(100) NOT NULL,
        FindingCategory  NVARCHAR(100) NULL,
        Title            NVARCHAR(200) NOT NULL,
        Remarks          NVARCHAR(MAX) NULL,
        FindingDate      DATE NULL,
        Severity         NVARCHAR(50) NULL,
        IncludeInPerformance BIT NOT NULL
            CONSTRAINT DF_tblEmpDiscFinding_InclPerf DEFAULT (1),
        IsActive         BIT NOT NULL
            CONSTRAINT DF_tblEmpDiscFinding_IsActive DEFAULT (1),
        CreatedOn        DATETIME NOT NULL
            CONSTRAINT DF_tblEmpDiscFinding_CreatedOn DEFAULT (GETDATE()),
        CreatedByUserID  INT NULL,
        CreatedByName    NVARCHAR(200) NULL,
        ModifiedOn       DATETIME NULL,
        ModifiedByUserID INT NULL
    );
    CREATE INDEX IX_tblEmployeeDisciplineFinding_Emp ON dbo.tblEmployeeDisciplineFinding(EmployeeID, IncludeInPerformance, IsActive);
END
GO

IF OBJECT_ID(N'dbo.tblAppForm', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.tblAppForm WHERE FormKey = N'EmployeeDiscipline')
        INSERT INTO dbo.tblAppForm (FormKey, FormName, PagePath, Category, SortOrder, IsActive, CreatedOn)
        VALUES (N'EmployeeDiscipline', N'Employee Discipline & Non-Conformance', N'/EmployeeDiscipline.aspx', N'Transaction', 25, 1, GETDATE());
END
GO

PRINT N'Employee Discipline & Non-Conformance patch completed.';
GO
