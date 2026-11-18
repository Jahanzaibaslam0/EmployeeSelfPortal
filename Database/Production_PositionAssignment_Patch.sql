-- Production patch: Position worker assignment history (one active employee per position).
-- Safe to re-run. Does not rename or drop existing data.

IF OBJECT_ID(N'tblPositionWorkerAssignment', N'U') IS NULL
BEGIN
    CREATE TABLE tblPositionWorkerAssignment (
        PositionWorkerAssignmentID INT IDENTITY(1,1) PRIMARY KEY,
        PositionID                 INT NOT NULL,
        EmployeeID                 INT NOT NULL,
        AssignmentStartDate        DATE NULL,
        AssignmentEndDate          DATE NULL,
        Reason                     NVARCHAR(500) NULL,
        SortOrder                  INT NOT NULL CONSTRAINT DF_tblPosWA_Sort DEFAULT (1),
        CreatedOn                  DATETIME NOT NULL CONSTRAINT DF_tblPosWA_Created DEFAULT (GETDATE()),
        CreatedByUserID            INT NULL,
        ModifiedOn                 DATETIME NULL,
        ModifiedByUserID           INT NULL
    );
END
GO

IF COL_LENGTH(N'tblPositionWorkerAssignment', N'ModifiedOn') IS NULL
    ALTER TABLE tblPositionWorkerAssignment ADD ModifiedOn DATETIME NULL;
GO

IF COL_LENGTH(N'tblPositionWorkerAssignment', N'ModifiedByUserID') IS NULL
    ALTER TABLE tblPositionWorkerAssignment ADD ModifiedByUserID INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblPosWA_Position_Open' AND object_id = OBJECT_ID(N'tblPositionWorkerAssignment'))
    CREATE NONCLUSTERED INDEX IX_tblPosWA_Position_Open
        ON tblPositionWorkerAssignment (PositionID)
        INCLUDE (EmployeeID, AssignmentStartDate, AssignmentEndDate);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblPosWA_Employee_Open' AND object_id = OBJECT_ID(N'tblPositionWorkerAssignment'))
    CREATE NONCLUSTERED INDEX IX_tblPosWA_Employee_Open
        ON tblPositionWorkerAssignment (EmployeeID)
        INCLUDE (PositionID, AssignmentEndDate);
GO
