/*==============================================================================
  Production patch: tblBankMaster for Bank Setup
  Fixes: Invalid column name 'AccountTitle'
  Safe to re-run.
==============================================================================*/

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.tblBankMaster', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblBankMaster (
        BankID                    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BankName                  NVARCHAR(150) NOT NULL,
        BankCode                  NVARCHAR(50)  NULL,
        LocationName              NVARCHAR(150) NULL,
        AccountTitle              NVARCHAR(150) NULL,
        BankGroupID               INT NULL,
        IBAN                      NVARCHAR(50)  NULL,
        SwiftBICCode              NVARCHAR(50)  NULL,
        CurrencyCode              NVARCHAR(50)  NULL,
        AccountVerificationStatus NVARCHAR(50)  NOT NULL
            CONSTRAINT DF_tblBankMaster_Verify DEFAULT (N'Pending'),
        IsActive                  BIT NOT NULL
            CONSTRAINT DF_tblBankMaster_IsActive DEFAULT (1),
        CreatedOn                 DATETIME NOT NULL
            CONSTRAINT DF_tblBankMaster_CreatedOn DEFAULT (GETDATE()),
        ModifiedOn                DATETIME NULL,
        CreatedByUserID           INT NULL,
        ModifiedByUserID          INT NULL
    );
END
GO

IF COL_LENGTH(N'dbo.tblBankMaster', N'AccountTitle') IS NULL
    ALTER TABLE dbo.tblBankMaster ADD AccountTitle NVARCHAR(150) NULL;
GO

IF COL_LENGTH(N'dbo.tblBankMaster', N'BankCode') IS NULL
    ALTER TABLE dbo.tblBankMaster ADD BankCode NVARCHAR(50) NULL;
IF COL_LENGTH(N'dbo.tblBankMaster', N'LocationName') IS NULL
    ALTER TABLE dbo.tblBankMaster ADD LocationName NVARCHAR(150) NULL;
IF COL_LENGTH(N'dbo.tblBankMaster', N'BankGroupID') IS NULL
    ALTER TABLE dbo.tblBankMaster ADD BankGroupID INT NULL;
IF COL_LENGTH(N'dbo.tblBankMaster', N'IBAN') IS NULL
    ALTER TABLE dbo.tblBankMaster ADD IBAN NVARCHAR(50) NULL;
IF COL_LENGTH(N'dbo.tblBankMaster', N'SwiftBICCode') IS NULL
    ALTER TABLE dbo.tblBankMaster ADD SwiftBICCode NVARCHAR(50) NULL;
IF COL_LENGTH(N'dbo.tblBankMaster', N'CurrencyCode') IS NULL
    ALTER TABLE dbo.tblBankMaster ADD CurrencyCode NVARCHAR(50) NULL;
IF COL_LENGTH(N'dbo.tblBankMaster', N'AccountVerificationStatus') IS NULL
    ALTER TABLE dbo.tblBankMaster ADD AccountVerificationStatus NVARCHAR(50) NOT NULL
        CONSTRAINT DF_tblBankMaster_Verify2 DEFAULT (N'Pending');
IF COL_LENGTH(N'dbo.tblBankMaster', N'IsActive') IS NULL
    ALTER TABLE dbo.tblBankMaster ADD IsActive BIT NOT NULL
        CONSTRAINT DF_tblBankMaster_IsActive2 DEFAULT (1);
IF COL_LENGTH(N'dbo.tblBankMaster', N'CreatedOn') IS NULL
    ALTER TABLE dbo.tblBankMaster ADD CreatedOn DATETIME NOT NULL
        CONSTRAINT DF_tblBankMaster_CreatedOn2 DEFAULT (GETDATE());
IF COL_LENGTH(N'dbo.tblBankMaster', N'ModifiedOn') IS NULL
    ALTER TABLE dbo.tblBankMaster ADD ModifiedOn DATETIME NULL;
IF COL_LENGTH(N'dbo.tblBankMaster', N'CreatedByUserID') IS NULL
    ALTER TABLE dbo.tblBankMaster ADD CreatedByUserID INT NULL;
IF COL_LENGTH(N'dbo.tblBankMaster', N'ModifiedByUserID') IS NULL
    ALTER TABLE dbo.tblBankMaster ADD ModifiedByUserID INT NULL;
GO

PRINT N'tblBankMaster patch completed (AccountTitle + related columns).';
GO
