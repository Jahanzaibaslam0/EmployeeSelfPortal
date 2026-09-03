/*==============================================================================
  Production patch: tblEmployeeTraining columns for Training Master
  Safe to re-run.
==============================================================================*/

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.tblEmployeeTraining', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblEmployeeTraining (
        EmployeeTrainingID           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmployeeID                   INT NOT NULL,
        MandatoryTrainingStatus      NVARCHAR(50)  NULL,
        SafetyTrainingValidTill      DATE          NULL,
        GMPTrainingValidTill         DATE          NULL,
        TrainingHoursYTD             DECIMAL(8,2)  NULL,
        TrainingHoursRequiredAnnual  DECIMAL(8,2)  NULL,
        LastTrainingDate             DATE          NULL,
        NextTrainingDue              DATE          NULL,
        TrainingName                 NVARCHAR(200) NULL,
        TrainingCode                 NVARCHAR(50)  NULL,
        TrainingDepartment           NVARCHAR(150) NOT NULL
            CONSTRAINT DF_tblEmployeeTraining_Dept DEFAULT (N'All'),
        CreatedOn                    DATETIME NOT NULL
            CONSTRAINT DF_tblEmployeeTraining_CreatedOn DEFAULT (GETDATE()),
        ModifiedOn                   DATETIME NULL,
        CreatedByUserID              INT NULL,
        ModifiedByUserID             INT NULL
    );
END
GO

IF COL_LENGTH(N'dbo.tblEmployeeTraining', N'MandatoryTrainingStatus') IS NULL
    ALTER TABLE dbo.tblEmployeeTraining ADD MandatoryTrainingStatus NVARCHAR(50) NULL;
IF COL_LENGTH(N'dbo.tblEmployeeTraining', N'SafetyTrainingValidTill') IS NULL
    ALTER TABLE dbo.tblEmployeeTraining ADD SafetyTrainingValidTill DATE NULL;
IF COL_LENGTH(N'dbo.tblEmployeeTraining', N'GMPTrainingValidTill') IS NULL
    ALTER TABLE dbo.tblEmployeeTraining ADD GMPTrainingValidTill DATE NULL;
IF COL_LENGTH(N'dbo.tblEmployeeTraining', N'TrainingHoursYTD') IS NULL
    ALTER TABLE dbo.tblEmployeeTraining ADD TrainingHoursYTD DECIMAL(8,2) NULL;
IF COL_LENGTH(N'dbo.tblEmployeeTraining', N'TrainingHoursRequiredAnnual') IS NULL
    ALTER TABLE dbo.tblEmployeeTraining ADD TrainingHoursRequiredAnnual DECIMAL(8,2) NULL;
IF COL_LENGTH(N'dbo.tblEmployeeTraining', N'LastTrainingDate') IS NULL
    ALTER TABLE dbo.tblEmployeeTraining ADD LastTrainingDate DATE NULL;
IF COL_LENGTH(N'dbo.tblEmployeeTraining', N'NextTrainingDue') IS NULL
    ALTER TABLE dbo.tblEmployeeTraining ADD NextTrainingDue DATE NULL;
IF COL_LENGTH(N'dbo.tblEmployeeTraining', N'TrainingName') IS NULL
    ALTER TABLE dbo.tblEmployeeTraining ADD TrainingName NVARCHAR(200) NULL;
IF COL_LENGTH(N'dbo.tblEmployeeTraining', N'TrainingCode') IS NULL
    ALTER TABLE dbo.tblEmployeeTraining ADD TrainingCode NVARCHAR(50) NULL;
IF COL_LENGTH(N'dbo.tblEmployeeTraining', N'TrainingDepartment') IS NULL
    ALTER TABLE dbo.tblEmployeeTraining ADD TrainingDepartment NVARCHAR(150) NOT NULL
        CONSTRAINT DF_tblEmployeeTraining_Dept2 DEFAULT (N'All');
IF COL_LENGTH(N'dbo.tblEmployeeTraining', N'CreatedOn') IS NULL
    ALTER TABLE dbo.tblEmployeeTraining ADD CreatedOn DATETIME NOT NULL
        CONSTRAINT DF_tblEmployeeTraining_CreatedOn2 DEFAULT (GETDATE());
IF COL_LENGTH(N'dbo.tblEmployeeTraining', N'ModifiedOn') IS NULL
    ALTER TABLE dbo.tblEmployeeTraining ADD ModifiedOn DATETIME NULL;
IF COL_LENGTH(N'dbo.tblEmployeeTraining', N'CreatedByUserID') IS NULL
    ALTER TABLE dbo.tblEmployeeTraining ADD CreatedByUserID INT NULL;
IF COL_LENGTH(N'dbo.tblEmployeeTraining', N'ModifiedByUserID') IS NULL
    ALTER TABLE dbo.tblEmployeeTraining ADD ModifiedByUserID INT NULL;
GO

PRINT N'tblEmployeeTraining patch completed.';
GO
