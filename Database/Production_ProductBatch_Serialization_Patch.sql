-- Product Hub: Batch Management + Product Serialization / GS1 DataMatrix.
-- Safe to re-run. Backup production before applying.
-- After deploy: grant ProductBatchMaster and ProductSerialization in User Rights Setup.

/* ---------- Product Master: GTIN ---------- */
IF COL_LENGTH(N'dbo.tblProduct', N'GTINNumber') IS NULL
    ALTER TABLE dbo.tblProduct ADD GTINNumber NVARCHAR(14) NULL;
GO
IF COL_LENGTH(N'dbo.tblProduct', N'SellingPrice') IS NULL
    ALTER TABLE dbo.tblProduct ADD SellingPrice DECIMAL(18,4) NULL;
GO

/* ---------- Batch Management ---------- */
IF OBJECT_ID(N'dbo.tblProductBatch', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblProductBatch (
        ProductBatchID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ProductID INT NOT NULL,
        BatchNumber NVARCHAR(50) NOT NULL,
        ManufacturingDate DATE NULL,
        ExpiryDate DATE NULL,
        BestBeforeDate DATE NULL,
        Manufacturer NVARCHAR(150) NULL,
        VendorID INT NULL,
        BatchStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_tblProductBatch_Status DEFAULT N'Registered',
        BatchMrp DECIMAL(18,4) NULL,
        AssessableRate DECIMAL(18,4) NULL,
        Remarks NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tblProductBatch_Active DEFAULT 1,
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblProductBatch_Created DEFAULT GETDATE(),
        CreatedByUserID INT NULL,
        ModifiedOn DATETIME NULL,
        ModifiedByUserID INT NULL
    );
END;
GO

IF EXISTS (
    SELECT 1 FROM sys.indexes i
    WHERE i.name = N'UX_tblProductBatch_Product_Batch'
      AND i.object_id = OBJECT_ID(N'dbo.tblProductBatch')
      AND i.has_filter = 0)
    DROP INDEX UX_tblProductBatch_Product_Batch ON dbo.tblProductBatch;
GO

IF OBJECT_ID(N'dbo.tblProductBatch', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    WHERE i.name = N'UX_tblProductBatch_Product_Batch'
      AND i.object_id = OBJECT_ID(N'dbo.tblProductBatch'))
    CREATE UNIQUE INDEX UX_tblProductBatch_Product_Batch
        ON dbo.tblProductBatch (ProductID, BatchNumber) WHERE IsActive = 1;
GO

IF OBJECT_ID(N'dbo.tblProductBatchStock', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblProductBatchStock (
        ProductBatchID INT NOT NULL PRIMARY KEY,
        QtyOnHand DECIMAL(18,4) NOT NULL CONSTRAINT DF_tblProductBatchStock_Qty DEFAULT 0,
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblProductBatchStock_Created DEFAULT GETDATE(),
        ModifiedOn DATETIME NULL,
        ModifiedByUserID INT NULL
    );
END;
GO

IF COL_LENGTH(N'dbo.tblInventoryTransaction', N'ProductBatchID') IS NULL
    ALTER TABLE dbo.tblInventoryTransaction ADD ProductBatchID INT NULL;
GO

/* ---------- Serialization runs ---------- */
IF OBJECT_ID(N'dbo.tblProductSerializationRun', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblProductSerializationRun (
        SerializationRunID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        GenerationKey NVARCHAR(40) NOT NULL,
        ProductID INT NOT NULL,
        ProductBatchID INT NOT NULL,
        GTINNumber NVARCHAR(14) NOT NULL,
        Quantity INT NOT NULL,
        CodeLength INT NOT NULL,
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblProductSerializationRun_Created DEFAULT GETDATE(),
        CreatedByUserID INT NULL
    );
    CREATE UNIQUE INDEX UX_tblProductSerializationRun_Key
        ON dbo.tblProductSerializationRun (GenerationKey);
END;
GO

IF COL_LENGTH(N'dbo.tblProductSerializationRun', N'ProductName') IS NULL
    ALTER TABLE dbo.tblProductSerializationRun ADD ProductName NVARCHAR(200) NULL;
GO
IF COL_LENGTH(N'dbo.tblProductSerializationRun', N'BatchNumber') IS NULL
    ALTER TABLE dbo.tblProductSerializationRun ADD BatchNumber NVARCHAR(50) NULL;
GO
IF COL_LENGTH(N'dbo.tblProductSerializationRun', N'ManufacturingDate') IS NULL
    ALTER TABLE dbo.tblProductSerializationRun ADD ManufacturingDate DATE NULL;
GO
IF COL_LENGTH(N'dbo.tblProductSerializationRun', N'ExpiryDate') IS NULL
    ALTER TABLE dbo.tblProductSerializationRun ADD ExpiryDate DATE NULL;
GO
IF COL_LENGTH(N'dbo.tblProductSerializationRun', N'GeneratedQuantity') IS NULL
    ALTER TABLE dbo.tblProductSerializationRun ADD GeneratedQuantity INT NULL;
GO
IF COL_LENGTH(N'dbo.tblProductSerializationRun', N'GenerationStatus') IS NULL
    ALTER TABLE dbo.tblProductSerializationRun ADD GenerationStatus NVARCHAR(20) NULL;
GO
IF COL_LENGTH(N'dbo.tblProductSerializationRun', N'GeneratedByName') IS NULL
    ALTER TABLE dbo.tblProductSerializationRun ADD GeneratedByName NVARCHAR(150) NULL;
GO

/* ---------- Serialization codes ---------- */
IF OBJECT_ID(N'dbo.tblProductSerial', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblProductSerial (
        ProductSerialID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SerializationRunID INT NOT NULL,
        GenerationKey NVARCHAR(40) NOT NULL,
        ProductID INT NOT NULL,
        ProductBatchID INT NOT NULL,
        GTINNumber NVARCHAR(14) NOT NULL,
        BatchNumber NVARCHAR(50) NOT NULL,
        Hexacode NVARCHAR(20) NOT NULL,
        CodeLength INT NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_tblProductSerial_Status DEFAULT N'Draft',
        Remarks NVARCHAR(500) NULL,
        Gs1ElementHint NVARCHAR(200) NULL,
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblProductSerial_Created DEFAULT GETDATE(),
        CreatedByUserID INT NULL
    );
    CREATE UNIQUE INDEX UX_tblProductSerial_Hexacode ON dbo.tblProductSerial (Hexacode);
END;
GO

IF COL_LENGTH(N'dbo.tblProductSerial', N'Gs1Payload') IS NULL
    ALTER TABLE dbo.tblProductSerial ADD Gs1Payload NVARCHAR(400) NULL;
GO
IF COL_LENGTH(N'dbo.tblProductSerial', N'Gs1HumanReadable') IS NULL
    ALTER TABLE dbo.tblProductSerial ADD Gs1HumanReadable NVARCHAR(400) NULL;
GO

IF OBJECT_ID(N'dbo.tblProductSerialStatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblProductSerialStatusHistory (
        SerialStatusHistoryID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ProductSerialID INT NOT NULL,
        OldStatus NVARCHAR(20) NULL,
        NewStatus NVARCHAR(20) NOT NULL,
        Remarks NVARCHAR(500) NULL,
        ChangedOn DATETIME NOT NULL CONSTRAINT DF_tblProductSerialHist_Changed DEFAULT GETDATE(),
        ChangedByUserID INT NULL
    );
    CREATE INDEX IX_tblProductSerialHist_Serial
        ON dbo.tblProductSerialStatusHistory (ProductSerialID, ChangedOn DESC);
END;
GO
