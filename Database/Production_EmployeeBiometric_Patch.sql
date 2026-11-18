/*==============================================================================
  Employee Master – BiometricID column (safe to re-run)
==============================================================================*/
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.tblEmployee', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.tblEmployee', N'BiometricID') IS NULL
BEGIN
    ALTER TABLE dbo.tblEmployee ADD BiometricID INT NULL;
    PRINT N'Added tblEmployee.BiometricID';
END
ELSE
    PRINT N'tblEmployee.BiometricID already present or tblEmployee missing.';
GO

IF OBJECT_ID(N'dbo.tblEmployee', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.tblEmployee', N'BiometricID') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_tblEmployee_BiometricID'
          AND object_id = OBJECT_ID(N'dbo.tblEmployee')
   )
BEGIN
    CREATE NONCLUSTERED INDEX IX_tblEmployee_BiometricID
        ON dbo.tblEmployee(BiometricID)
        WHERE BiometricID IS NOT NULL;
END
GO
