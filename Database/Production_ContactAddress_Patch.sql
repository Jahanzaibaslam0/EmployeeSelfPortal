-- Production patch: Country / State / Postal / Street masters for contact addresses.
-- Safe to re-run. Does not drop tblContactLocationSetup (legacy combined lookup).

IF OBJECT_ID(N'dbo.tblContactCountry', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblContactCountry (
        CountryID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblContactCountry PRIMARY KEY,
        CountryName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tblContactCountry_Active DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblContactCountry_Created DEFAULT (GETDATE()),
        CreatedByUserID INT NULL,
        ModifiedOn DATETIME NULL,
        ModifiedByUserID INT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.tblContactState', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblContactState (
        StateID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblContactState PRIMARY KEY,
        CountryID INT NOT NULL,
        StateName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tblContactState_Active DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblContactState_Created DEFAULT (GETDATE()),
        CreatedByUserID INT NULL,
        ModifiedOn DATETIME NULL,
        ModifiedByUserID INT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.tblContactPostal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblContactPostal (
        PostalID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblContactPostal PRIMARY KEY,
        StateID INT NOT NULL,
        PostalCode NVARCHAR(20) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tblContactPostal_Active DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblContactPostal_Created DEFAULT (GETDATE()),
        CreatedByUserID INT NULL,
        ModifiedOn DATETIME NULL,
        ModifiedByUserID INT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.tblContactStreet', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblContactStreet (
        StreetID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblContactStreet PRIMARY KEY,
        PostalID INT NOT NULL,
        StreetName NVARCHAR(200) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tblContactStreet_Active DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblContactStreet_Created DEFAULT (GETDATE()),
        CreatedByUserID INT NULL,
        ModifiedOn DATETIME NULL,
        ModifiedByUserID INT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.tblContactAddress', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblContactAddress (
        AddressID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblContactAddress PRIMARY KEY,
        ContactID INT NOT NULL,
        CountryID INT NULL,
        StateID INT NULL,
        PostalID INT NULL,
        StreetID INT NULL,
        AddressLine NVARCHAR(500) NULL,
        Latitude DECIMAL(10,7) NULL,
        Longitude DECIMAL(11,7) NULL,
        IsBilling BIT NOT NULL CONSTRAINT DF_tblContactAddress_Billing DEFAULT (0),
        IsShipping BIT NOT NULL CONSTRAINT DF_tblContactAddress_Shipping DEFAULT (0),
        IsPrimary BIT NOT NULL CONSTRAINT DF_tblContactAddress_Primary DEFAULT (0),
        SortOrder INT NOT NULL CONSTRAINT DF_tblContactAddress_Sort DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblContactAddress_Created DEFAULT (GETDATE()),
        CreatedByUserID INT NULL,
        ModifiedOn DATETIME NULL,
        ModifiedByUserID INT NULL
    );
END
GO

IF COL_LENGTH(N'dbo.tblContactAddress', N'CountryID') IS NULL ALTER TABLE dbo.tblContactAddress ADD CountryID INT NULL;
IF COL_LENGTH(N'dbo.tblContactAddress', N'StateID') IS NULL ALTER TABLE dbo.tblContactAddress ADD StateID INT NULL;
IF COL_LENGTH(N'dbo.tblContactAddress', N'PostalID') IS NULL ALTER TABLE dbo.tblContactAddress ADD PostalID INT NULL;
IF COL_LENGTH(N'dbo.tblContactAddress', N'StreetID') IS NULL ALTER TABLE dbo.tblContactAddress ADD StreetID INT NULL;
IF COL_LENGTH(N'dbo.tblContactAddress', N'Latitude') IS NULL ALTER TABLE dbo.tblContactAddress ADD Latitude DECIMAL(10,7) NULL;
IF COL_LENGTH(N'dbo.tblContactAddress', N'Longitude') IS NULL ALTER TABLE dbo.tblContactAddress ADD Longitude DECIMAL(11,7) NULL;
IF COL_LENGTH(N'dbo.tblContactAddress', N'IsPrimary') IS NULL ALTER TABLE dbo.tblContactAddress ADD IsPrimary BIT NOT NULL CONSTRAINT DF_tblContactAddress_Primary DEFAULT (0);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_tblContactAddress_Primary' AND object_id = OBJECT_ID(N'dbo.tblContactAddress'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_tblContactAddress_Primary ON dbo.tblContactAddress (ContactID) WHERE IsPrimary = 1;
GO

IF COL_LENGTH(N'dbo.tblContactAddress', N'LocationSetupID') IS NOT NULL
AND EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblContactAddress') AND name = N'LocationSetupID' AND is_nullable = 0
)
    ALTER TABLE dbo.tblContactAddress ALTER COLUMN LocationSetupID INT NULL;
GO
