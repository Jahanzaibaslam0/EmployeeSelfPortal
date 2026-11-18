/*==============================================================================
  Employee Request Management – schema + form seed (safe to re-run)
==============================================================================*/
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.tblEmployeeRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblEmployeeRequest (
        RequestID              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RequestNumber          NVARCHAR(30)  NOT NULL,
        EmployeeID             INT NOT NULL,
        RequestCategory        NVARCHAR(50)  NOT NULL,
        Status                 NVARCHAR(50)  NOT NULL
            CONSTRAINT DF_tblEmployeeRequest_Status DEFAULT (N'Draft'),
        Subject                NVARCHAR(200) NOT NULL,
        Description            NVARCHAR(2000) NULL,
        Amount                 DECIMAL(18,2) NULL,
        StartDate              DATE NULL,
        EndDate                DATE NULL,
        DetailsJson            NVARCHAR(MAX) NULL,
        ManagerEmployeeID      INT NULL,
        SubmittedOn            DATETIME NULL,
        CompletedOn            DATETIME NULL,
        IsActive               BIT NOT NULL
            CONSTRAINT DF_tblEmployeeRequest_IsActive DEFAULT (1),
        CreatedOn              DATETIME NOT NULL
            CONSTRAINT DF_tblEmployeeRequest_CreatedOn DEFAULT (GETDATE()),
        ModifiedOn             DATETIME NULL,
        CreatedByUserID        INT NULL,
        ModifiedByUserID       INT NULL
    );
    CREATE UNIQUE INDEX UX_tblEmployeeRequest_Number ON dbo.tblEmployeeRequest(RequestNumber);
    CREATE INDEX IX_tblEmployeeRequest_Employee ON dbo.tblEmployeeRequest(EmployeeID, Status);
    CREATE INDEX IX_tblEmployeeRequest_Manager ON dbo.tblEmployeeRequest(ManagerEmployeeID, Status);
END
GO

IF OBJECT_ID(N'dbo.tblEmployeeRequestAttachment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblEmployeeRequestAttachment (
        AttachmentID     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RequestID        INT NOT NULL REFERENCES dbo.tblEmployeeRequest(RequestID),
        DocumentPath     NVARCHAR(500) NOT NULL,
        OriginalFileName NVARCHAR(255) NULL,
        UploadedOn       DATETIME NOT NULL
            CONSTRAINT DF_tblEmpReqAtt_UploadedOn DEFAULT (GETDATE()),
        UploadedByUserID INT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.tblEmployeeRequestAction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblEmployeeRequestAction (
        ActionID           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RequestID          INT NOT NULL REFERENCES dbo.tblEmployeeRequest(RequestID),
        ActionStage        NVARCHAR(50)  NOT NULL,  -- Employee, Manager, HR, Management
        ActionType         NVARCHAR(50)  NOT NULL,  -- Submit, Approve, Reject, Return, SaveDraft
        Comments           NVARCHAR(2000) NULL,
        ActionByUserID     INT NULL,
        ActionByEmployeeID INT NULL,
        ActionByName       NVARCHAR(200) NULL,
        ActionOn           DATETIME NOT NULL
            CONSTRAINT DF_tblEmpReqAction_On DEFAULT (GETDATE())
    );
    CREATE INDEX IX_tblEmployeeRequestAction_Request ON dbo.tblEmployeeRequestAction(RequestID, ActionOn);
END
GO

IF OBJECT_ID(N'dbo.tblEmployeeRequestAlert', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblEmployeeRequestAlert (
        AlertID      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserID       INT NOT NULL,
        RequestID    INT NULL,
        Title        NVARCHAR(200) NOT NULL,
        Message      NVARCHAR(1000) NULL,
        LinkUrl      NVARCHAR(300) NULL,
        IsRead       BIT NOT NULL CONSTRAINT DF_tblEmpReqAlert_IsRead DEFAULT (0),
        CreatedOn    DATETIME NOT NULL CONSTRAINT DF_tblEmpReqAlert_CreatedOn DEFAULT (GETDATE())
    );
    CREATE INDEX IX_tblEmployeeRequestAlert_User ON dbo.tblEmployeeRequestAlert(UserID, IsRead, CreatedOn DESC);
END
GO

IF OBJECT_ID(N'dbo.tblAppForm', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.tblAppForm WHERE FormKey = N'EmployeeRequest')
        INSERT INTO dbo.tblAppForm (FormKey, FormName, PagePath, Category, SortOrder, IsActive, CreatedOn)
        VALUES (N'EmployeeRequest', N'Employee Requests', N'/EmployeeRequest.aspx', N'Transaction', 18, 1, GETDATE());

    IF NOT EXISTS (SELECT 1 FROM dbo.tblAppForm WHERE FormKey = N'EmployeeRequestHR')
        INSERT INTO dbo.tblAppForm (FormKey, FormName, PagePath, Category, SortOrder, IsActive, CreatedOn)
        VALUES (N'EmployeeRequestHR', N'Employee Request – HR Approval', N'/EmployeeRequest.aspx', N'Transaction', 19, 1, GETDATE());

    IF NOT EXISTS (SELECT 1 FROM dbo.tblAppForm WHERE FormKey = N'EmployeeRequestManagement')
        INSERT INTO dbo.tblAppForm (FormKey, FormName, PagePath, Category, SortOrder, IsActive, CreatedOn)
        VALUES (N'EmployeeRequestManagement', N'Employee Request – Management Approval', N'/EmployeeRequest.aspx', N'Transaction', 20, 1, GETDATE());
END
GO

PRINT N'Employee Request Management schema completed.';
GO
