-- Two-level leave approval (Department Head → HR Manager). Safe to re-run.

IF COL_LENGTH(N'tblDepartment', N'DepartmentManagerID') IS NULL
    ALTER TABLE tblDepartment ADD DepartmentManagerID INT NULL;
GO

IF OBJECT_ID(N'dbo.tblLeaveApplication', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.tblLeaveApplication', N'Status') IS NULL
    ALTER TABLE dbo.tblLeaveApplication ADD Status NVARCHAR(50) NOT NULL CONSTRAINT DF_tblLeaveApp_Status DEFAULT (N'Approved');
GO

IF OBJECT_ID(N'dbo.tblLeaveApplication', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.tblLeaveApplication', N'DepartmentHeadEmployeeID') IS NULL
    ALTER TABLE dbo.tblLeaveApplication ADD DepartmentHeadEmployeeID INT NULL;
GO

IF OBJECT_ID(N'dbo.tblLeaveApprovalAction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblLeaveApprovalAction (
        ActionID         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        LeaveID          INT NOT NULL,
        Stage            NVARCHAR(50) NOT NULL,
        Action           NVARCHAR(50) NOT NULL,
        ActorUserID      INT NULL,
        ActorEmployeeID  INT NULL,
        Comments         NVARCHAR(1000) NULL,
        ActionOn         DATETIME NOT NULL CONSTRAINT DF_tblLeaveAct_On DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.tblLeaveAlert', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblLeaveAlert (
        AlertID   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserID    INT NOT NULL,
        LeaveID   INT NOT NULL,
        Title     NVARCHAR(200) NOT NULL,
        Message   NVARCHAR(1000) NULL,
        LinkUrl   NVARCHAR(300) NULL,
        IsRead    BIT NOT NULL CONSTRAINT DF_tblLeaveAlert_Read DEFAULT (0),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblLeaveAlert_Created DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.tblAppForm', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.tblAppForm WHERE FormKey = N'LeaveHR')
    INSERT INTO dbo.tblAppForm (FormKey, FormName, PagePath, Category, SortOrder, IsActive, CreatedOn)
    VALUES (N'LeaveHR', N'Leave Management - HR Manager', N'/LeaveMaster.aspx', N'Transaction', 16, 1, GETDATE());
GO

PRINT N'Leave approval patch completed.';
GO
