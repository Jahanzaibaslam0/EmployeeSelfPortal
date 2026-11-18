-- Contact Hub: master columns, geo lookups, addresses, change workflow.
-- Safe to re-run. Backup production before applying.
-- After deploy: grant Contact Master (View / Add-Edit / Approve) in User Rights Setup.
-- Supercedes Database/Production_ContactMasterType_Patch.sql and
-- Database/Production_ContactAddress_Patch.sql (those remain valid if already applied).

/* ---------- tblContactMaster columns ---------- */
IF COL_LENGTH(N'dbo.tblContactMaster', N'CreatedByUserID') IS NULL
    ALTER TABLE dbo.tblContactMaster ADD CreatedByUserID INT NULL;
GO
IF COL_LENGTH(N'dbo.tblContactMaster', N'RecordType') IS NULL
    ALTER TABLE dbo.tblContactMaster ADD RecordType NVARCHAR(20) NOT NULL CONSTRAINT DF_tblContact_RecordType DEFAULT N'People';
GO
IF COL_LENGTH(N'dbo.tblContactMaster', N'VerificationStatus') IS NULL
    ALTER TABLE dbo.tblContactMaster ADD VerificationStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_tblContact_Verify DEFAULT N'Verified';
GO
IF COL_LENGTH(N'dbo.tblContactMaster', N'AccessStatus') IS NULL
    ALTER TABLE dbo.tblContactMaster ADD AccessStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_tblContact_Access DEFAULT N'Open';
GO
IF COL_LENGTH(N'dbo.tblContactMaster', N'WorkflowStatus') IS NULL
    ALTER TABLE dbo.tblContactMaster ADD WorkflowStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_tblContact_Workflow DEFAULT N'Approved';
GO
IF COL_LENGTH(N'dbo.tblContactMaster', N'VendorID') IS NULL
    ALTER TABLE dbo.tblContactMaster ADD VendorID INT NULL;
GO
IF COL_LENGTH(N'dbo.tblContactMaster', N'EmployeeID') IS NULL
    ALTER TABLE dbo.tblContactMaster ADD EmployeeID INT NULL;
GO
IF COL_LENGTH(N'dbo.tblContactMaster', N'GuestName') IS NULL
    ALTER TABLE dbo.tblContactMaster ADD GuestName NVARCHAR(200) NULL;
GO

IF OBJECT_ID(N'dbo.tblVendor', N'U') IS NOT NULL
AND OBJECT_ID(N'dbo.FK_tblContactMaster_Vendor', N'F') IS NULL
    ALTER TABLE dbo.tblContactMaster ADD CONSTRAINT FK_tblContactMaster_Vendor
        FOREIGN KEY (VendorID) REFERENCES dbo.tblVendor(VendorID);
GO
IF OBJECT_ID(N'dbo.tblEmployee', N'U') IS NOT NULL
AND OBJECT_ID(N'dbo.FK_tblContactMaster_Employee', N'F') IS NULL
    ALTER TABLE dbo.tblContactMaster ADD CONSTRAINT FK_tblContactMaster_Employee
        FOREIGN KEY (EmployeeID) REFERENCES dbo.tblEmployee(EmployeeID);
GO

UPDATE dbo.tblContactMaster
SET ContactType = N'Guest/Others'
WHERE ContactType IN (N'Guest', N'Others', N'Prospect', N'Consultant');
GO

/* ---------- Geo masters: Country → State → City → Postal → Street ---------- */
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
    CREATE UNIQUE INDEX UX_tblContactCountry_Name ON dbo.tblContactCountry (CountryName) WHERE IsActive = 1;
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
        ModifiedByUserID INT NULL,
        CONSTRAINT FK_tblContactState_Country FOREIGN KEY (CountryID) REFERENCES dbo.tblContactCountry(CountryID)
    );
    CREATE UNIQUE INDEX UX_tblContactState_Name ON dbo.tblContactState (CountryID, StateName) WHERE IsActive = 1;
END
GO

IF OBJECT_ID(N'dbo.tblContactCity', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblContactCity (
        CityID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblContactCity PRIMARY KEY,
        StateID INT NOT NULL,
        CityName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tblContactCity_Active DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblContactCity_Created DEFAULT (GETDATE()),
        CreatedByUserID INT NULL,
        ModifiedOn DATETIME NULL,
        ModifiedByUserID INT NULL,
        CONSTRAINT FK_tblContactCity_State FOREIGN KEY (StateID) REFERENCES dbo.tblContactState(StateID)
    );
    CREATE UNIQUE INDEX UX_tblContactCity_Name ON dbo.tblContactCity (StateID, CityName) WHERE IsActive = 1;
END
GO

IF OBJECT_ID(N'dbo.tblContactPostal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblContactPostal (
        PostalID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblContactPostal PRIMARY KEY,
        CityID INT NOT NULL,
        StateID INT NOT NULL,
        PostalCode NVARCHAR(20) NOT NULL,
        AreaName NVARCHAR(150) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tblContactPostal_Active DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblContactPostal_Created DEFAULT (GETDATE()),
        CreatedByUserID INT NULL,
        ModifiedOn DATETIME NULL,
        ModifiedByUserID INT NULL,
        CONSTRAINT FK_tblContactPostal_City FOREIGN KEY (CityID) REFERENCES dbo.tblContactCity(CityID),
        CONSTRAINT FK_tblContactPostal_State FOREIGN KEY (StateID) REFERENCES dbo.tblContactState(StateID)
    );
    CREATE UNIQUE INDEX UX_tblContactPostal_CityCode ON dbo.tblContactPostal (CityID, PostalCode) WHERE IsActive = 1;
END
GO

IF COL_LENGTH(N'dbo.tblContactPostal', N'CityID') IS NULL
    ALTER TABLE dbo.tblContactPostal ADD CityID INT NULL;
GO
IF COL_LENGTH(N'dbo.tblContactPostal', N'AreaName') IS NULL
    ALTER TABLE dbo.tblContactPostal ADD AreaName NVARCHAR(150) NULL;
GO
IF OBJECT_ID(N'dbo.FK_tblContactPostal_City', N'F') IS NULL
AND COL_LENGTH(N'dbo.tblContactPostal', N'CityID') IS NOT NULL
    ALTER TABLE dbo.tblContactPostal ADD CONSTRAINT FK_tblContactPostal_City
        FOREIGN KEY (CityID) REFERENCES dbo.tblContactCity(CityID);
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_tblContactPostal_Code' AND object_id = OBJECT_ID(N'dbo.tblContactPostal'))
    DROP INDEX UX_tblContactPostal_Code ON dbo.tblContactPostal;
GO
IF COL_LENGTH(N'dbo.tblContactPostal', N'CityID') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_tblContactPostal_CityCode' AND object_id = OBJECT_ID(N'dbo.tblContactPostal'))
    CREATE UNIQUE INDEX UX_tblContactPostal_CityCode
        ON dbo.tblContactPostal (CityID, PostalCode) WHERE IsActive = 1 AND CityID IS NOT NULL;
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
        ModifiedByUserID INT NULL,
        CONSTRAINT FK_tblContactStreet_Postal FOREIGN KEY (PostalID) REFERENCES dbo.tblContactPostal(PostalID)
    );
    CREATE UNIQUE INDEX UX_tblContactStreet_Name ON dbo.tblContactStreet (PostalID, StreetName) WHERE IsActive = 1;
END
GO

/* ---------- Contact addresses ---------- */
IF OBJECT_ID(N'dbo.tblContactAddress', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblContactAddress (
        AddressID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblContactAddress PRIMARY KEY,
        ContactID INT NOT NULL,
        CountryID INT NULL,
        StateID INT NULL,
        CityID INT NULL,
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
GO
IF COL_LENGTH(N'dbo.tblContactAddress', N'StateID') IS NULL ALTER TABLE dbo.tblContactAddress ADD StateID INT NULL;
GO
IF COL_LENGTH(N'dbo.tblContactAddress', N'CityID') IS NULL ALTER TABLE dbo.tblContactAddress ADD CityID INT NULL;
GO
IF COL_LENGTH(N'dbo.tblContactAddress', N'PostalID') IS NULL ALTER TABLE dbo.tblContactAddress ADD PostalID INT NULL;
GO
IF COL_LENGTH(N'dbo.tblContactAddress', N'StreetID') IS NULL ALTER TABLE dbo.tblContactAddress ADD StreetID INT NULL;
GO
IF COL_LENGTH(N'dbo.tblContactAddress', N'Latitude') IS NULL ALTER TABLE dbo.tblContactAddress ADD Latitude DECIMAL(10,7) NULL;
GO
IF COL_LENGTH(N'dbo.tblContactAddress', N'Longitude') IS NULL ALTER TABLE dbo.tblContactAddress ADD Longitude DECIMAL(11,7) NULL;
GO
IF COL_LENGTH(N'dbo.tblContactAddress', N'IsPrimary') IS NULL
    ALTER TABLE dbo.tblContactAddress ADD IsPrimary BIT NOT NULL CONSTRAINT DF_tblContactAddress_Primary DEFAULT (0);
GO
IF COL_LENGTH(N'dbo.tblContactAddress', N'WorkflowStatus') IS NULL
    ALTER TABLE dbo.tblContactAddress ADD WorkflowStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_tblContactAddr_Workflow DEFAULT N'Approved';
GO
IF COL_LENGTH(N'dbo.tblContactAddress', N'VerificationStatus') IS NULL
    ALTER TABLE dbo.tblContactAddress ADD VerificationStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_tblContactAddr_Verify DEFAULT N'Verified';
GO

IF COL_LENGTH(N'dbo.tblContactAddress', N'LocationSetupID') IS NOT NULL
AND EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblContactAddress') AND name = N'LocationSetupID' AND is_nullable = 0)
    ALTER TABLE dbo.tblContactAddress ALTER COLUMN LocationSetupID INT NULL;
GO

IF OBJECT_ID(N'dbo.tblContactMaster', N'U') IS NOT NULL
AND OBJECT_ID(N'dbo.FK_tblContactAddress_Contact', N'F') IS NULL
    ALTER TABLE dbo.tblContactAddress ADD CONSTRAINT FK_tblContactAddress_Contact
        FOREIGN KEY (ContactID) REFERENCES dbo.tblContactMaster(ContactID) ON DELETE CASCADE;
GO
IF OBJECT_ID(N'dbo.FK_tblContactAddress_Country', N'F') IS NULL
AND OBJECT_ID(N'dbo.tblContactCountry', N'U') IS NOT NULL
    ALTER TABLE dbo.tblContactAddress ADD CONSTRAINT FK_tblContactAddress_Country
        FOREIGN KEY (CountryID) REFERENCES dbo.tblContactCountry(CountryID);
GO
IF OBJECT_ID(N'dbo.FK_tblContactAddress_State', N'F') IS NULL
AND OBJECT_ID(N'dbo.tblContactState', N'U') IS NOT NULL
    ALTER TABLE dbo.tblContactAddress ADD CONSTRAINT FK_tblContactAddress_State
        FOREIGN KEY (StateID) REFERENCES dbo.tblContactState(StateID);
GO
IF OBJECT_ID(N'dbo.FK_tblContactAddress_City', N'F') IS NULL
AND OBJECT_ID(N'dbo.tblContactCity', N'U') IS NOT NULL
    ALTER TABLE dbo.tblContactAddress ADD CONSTRAINT FK_tblContactAddress_City
        FOREIGN KEY (CityID) REFERENCES dbo.tblContactCity(CityID);
GO
IF OBJECT_ID(N'dbo.FK_tblContactAddress_Postal', N'F') IS NULL
AND OBJECT_ID(N'dbo.tblContactPostal', N'U') IS NOT NULL
    ALTER TABLE dbo.tblContactAddress ADD CONSTRAINT FK_tblContactAddress_Postal
        FOREIGN KEY (PostalID) REFERENCES dbo.tblContactPostal(PostalID);
GO
IF OBJECT_ID(N'dbo.FK_tblContactAddress_Street', N'F') IS NULL
AND OBJECT_ID(N'dbo.tblContactStreet', N'U') IS NOT NULL
    ALTER TABLE dbo.tblContactAddress ADD CONSTRAINT FK_tblContactAddress_Street
        FOREIGN KEY (StreetID) REFERENCES dbo.tblContactStreet(StreetID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblContactAddress_Contact' AND object_id = OBJECT_ID(N'dbo.tblContactAddress'))
    CREATE NONCLUSTERED INDEX IX_tblContactAddress_Contact ON dbo.tblContactAddress (ContactID, SortOrder);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_tblContactAddress_Billing' AND object_id = OBJECT_ID(N'dbo.tblContactAddress'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_tblContactAddress_Billing ON dbo.tblContactAddress (ContactID) WHERE IsBilling = 1;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_tblContactAddress_Shipping' AND object_id = OBJECT_ID(N'dbo.tblContactAddress'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_tblContactAddress_Shipping ON dbo.tblContactAddress (ContactID) WHERE IsShipping = 1;
GO
IF COL_LENGTH(N'dbo.tblContactAddress', N'IsPrimary') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_tblContactAddress_Primary' AND object_id = OBJECT_ID(N'dbo.tblContactAddress'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_tblContactAddress_Primary ON dbo.tblContactAddress (ContactID) WHERE IsPrimary = 1;
GO

/* ---------- Contact change workflow ---------- */
IF OBJECT_ID(N'dbo.tblContactChangeRequest', N'U') IS NULL
CREATE TABLE dbo.tblContactChangeRequest (
    RequestID INT IDENTITY(1,1) PRIMARY KEY,
    RequestCode NVARCHAR(20) NOT NULL,
    ContactID INT NOT NULL,
    ActionType NVARCHAR(20) NOT NULL,
    SubmittedByUserID INT NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    SubmittedOn DATETIME NOT NULL DEFAULT GETDATE(),
    DecidedOn DATETIME NULL,
    DecidedByUserID INT NULL,
    DecisionComments NVARCHAR(1000) NULL,
    OldJson NVARCHAR(MAX) NULL,
    NewJson NVARCHAR(MAX) NULL,
    CreatedOn DATETIME NOT NULL DEFAULT GETDATE()
);
GO
IF COL_LENGTH(N'dbo.tblContactChangeRequest', N'EntityType') IS NULL
    ALTER TABLE dbo.tblContactChangeRequest ADD EntityType NVARCHAR(20) NOT NULL CONSTRAINT DF_tblContactChange_Entity DEFAULT N'Contact';
GO
IF COL_LENGTH(N'dbo.tblContactChangeRequest', N'AddressID') IS NULL
    ALTER TABLE dbo.tblContactChangeRequest ADD AddressID INT NULL;
GO

IF OBJECT_ID(N'dbo.tblContactChangeLine', N'U') IS NULL
CREATE TABLE dbo.tblContactChangeLine (
    LineID INT IDENTITY(1,1) PRIMARY KEY,
    RequestID INT NOT NULL,
    FieldLabel NVARCHAR(80) NOT NULL,
    OldValue NVARCHAR(500) NULL,
    NewValue NVARCHAR(500) NULL
);
GO

IF OBJECT_ID(N'dbo.tblContactChangeHistory', N'U') IS NULL
CREATE TABLE dbo.tblContactChangeHistory (
    HistoryID INT IDENTITY(1,1) PRIMARY KEY,
    RequestID INT NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    ActorUserID INT NOT NULL,
    Comments NVARCHAR(1000) NULL,
    ActionOn DATETIME NOT NULL DEFAULT GETDATE()
);
GO
