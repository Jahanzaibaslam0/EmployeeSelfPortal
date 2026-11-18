/*==============================================================================
  Production patch: Employee Identification child table
  Safe to re-run.
==============================================================================*/

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.tblEmployeeIdentification', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblEmployeeIdentification (
        EmployeeIdentificationID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmployeeID               INT NOT NULL,
        DocumentType             NVARCHAR(50)  NOT NULL,
        RegistrationNo           NVARCHAR(100) NOT NULL,
        IssueDate                DATE NULL,
        ExpiryDate               DATE NULL,
        SortOrder                INT NOT NULL CONSTRAINT DF_EmpIdent_SortOrder DEFAULT (1),
        CreatedOn                DATETIME NOT NULL CONSTRAINT DF_EmpIdent_CreatedOn DEFAULT (GETDATE()),
        ModifiedOn               DATETIME NULL,
        CreatedByUserID          INT NULL,
        ModifiedByUserID         INT NULL
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_tblEmployeeIdentification_Employee'
      AND object_id = OBJECT_ID(N'dbo.tblEmployeeIdentification')
)
    CREATE NONCLUSTERED INDEX IX_tblEmployeeIdentification_Employee
        ON dbo.tblEmployeeIdentification (EmployeeID, SortOrder);
GO

IF OBJECT_ID(N'dbo.tblEmployeeIdentification', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.tblEmployeeIdentification', N'BlacklistComment') IS NULL
    ALTER TABLE dbo.tblEmployeeIdentification ADD BlacklistComment NVARCHAR(200) NULL;
GO
