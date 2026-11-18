/*==============================================================================
  Leave Application tables + seed categories (safe to re-run)
==============================================================================*/
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.tblLeaveCategory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblLeaveCategory (
        LeaveCategoryID   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        LeaveCategoryName NVARCHAR(150) NOT NULL,
        IsActive          BIT NOT NULL CONSTRAINT DF_tblLeaveCategory_IsActive DEFAULT (1),
        CreatedOn         DATETIME NOT NULL CONSTRAINT DF_tblLeaveCategory_CreatedOn DEFAULT (GETDATE()),
        ModifiedOn        DATETIME NULL,
        CreatedByUserID   INT NULL,
        ModifiedByUserID  INT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.tblLeaveApplication', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblLeaveApplication (
        LeaveID                        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ApplyingDate                   DATE NOT NULL,
        EmployeeID                     INT NOT NULL,
        LeaveType                      NVARCHAR(50) NOT NULL,
        IsFutureUnplannedLeave         BIT NOT NULL CONSTRAINT DF_tblLeaveApp_Future DEFAULT (0),
        LeaveCategoryID                INT NULL,
        LeaveFromDate                  DATE NOT NULL,
        LeaveToDate                    DATE NOT NULL,
        NumberOfDays                   INT NOT NULL CONSTRAINT DF_tblLeaveApp_Days DEFAULT (0),
        ReasonForLeave                 NVARCHAR(1000) NULL,
        TempResponsibleEmployeeID      INT NULL,
        PermanentResponsibleEmployeeID INT NULL,
        CreatedOn                      DATETIME NOT NULL CONSTRAINT DF_tblLeaveApp_CreatedOn DEFAULT (GETDATE()),
        ModifiedOn                     DATETIME NULL,
        CreatedByUserID                INT NULL,
        ModifiedByUserID               INT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.tblLeaveCategory', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.tblLeaveCategory)
BEGIN
    INSERT INTO dbo.tblLeaveCategory (LeaveCategoryName) VALUES
    (N'Casual'), (N'Annual'), (N'Maternity'), (N'Sick'), (N'Leave without pay');
END
GO

IF OBJECT_ID(N'dbo.tblAppForm', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.tblAppForm WHERE FormKey = N'LeaveMaster')
        INSERT INTO dbo.tblAppForm (FormKey, FormName, PagePath, Category, SortOrder, IsActive, CreatedOn)
        VALUES (N'LeaveMaster', N'Leave Management', N'/LeaveMaster.aspx', N'Transaction', 16, 1, GETDATE());
END
GO

PRINT N'Leave application schema patch completed.';
GO
