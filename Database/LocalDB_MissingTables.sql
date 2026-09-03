-- =============================================
-- HRMS – Tables / columns missing from Script.sql
-- Run AFTER Script.sql + UserSecurity_Script.sql
-- Safe to re-run (IF NOT EXISTS / COL_LENGTH checks)
-- Target: (LocalDB)\MSSQLLocalDB → HRMSDB
-- =============================================

SET NOCOUNT ON;
USE HRMSDB;
GO

-- ---------- Lookups & org structure ----------
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblBenefit' AND type = 'U')
CREATE TABLE tblBenefit (
    BenefitID    INT IDENTITY(1,1) PRIMARY KEY,
    BenefitCode  NVARCHAR(20)  NULL,
    BenefitName  NVARCHAR(150) NOT NULL,
    BenefitType  NVARCHAR(50)  NULL,
    Description  NVARCHAR(500) NULL,
    IsActive     BIT           NOT NULL DEFAULT 1,
    CreatedOn    DATETIME      NOT NULL DEFAULT GETDATE(),
    ModifiedOn   DATETIME      NULL,
    CreatedByUserID INT NULL,
    ModifiedByUserID INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblBenefitEntitlementDetail' AND type = 'U')
CREATE TABLE tblBenefitEntitlementDetail (
    DetailID             INT IDENTITY(1,1) PRIMARY KEY,
    BenefitEntitlementID INT NOT NULL,
    BenefitID            INT NOT NULL,
    BenefitLimit         NVARCHAR(100) NULL,
    Remarks              NVARCHAR(500) NULL,
    CreatedOn            DATETIME NOT NULL DEFAULT GETDATE()
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblWorkforceSegment' AND type = 'U')
CREATE TABLE tblWorkforceSegment (
    WorkforceSegmentID   INT IDENTITY(1,1) PRIMARY KEY,
    WorkforceSegmentName NVARCHAR(100) NOT NULL UNIQUE,
    AliasName            NVARCHAR(50)  NULL,
    IsActive             BIT NOT NULL DEFAULT 1,
    CreatedOn            DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedOn           DATETIME NULL,
    CreatedByUserID INT NULL,
    ModifiedByUserID INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblBusinessUnit' AND type = 'U')
CREATE TABLE tblBusinessUnit (
    BusinessUnitID   INT IDENTITY(1,1) PRIMARY KEY,
    BusinessUnitName NVARCHAR(100) NOT NULL UNIQUE,
    AliasName        NVARCHAR(50)  NULL,
    IsActive         BIT NOT NULL DEFAULT 1,
    CreatedOn        DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedOn       DATETIME NULL,
    CreatedByUserID INT NULL,
    ModifiedByUserID INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblTitle' AND type = 'U')
CREATE TABLE tblTitle (
    TitleID          INT IDENTITY(1,1) PRIMARY KEY,
    TitleName        NVARCHAR(150) NOT NULL,
    AliasName        NVARCHAR(50)  NULL,
    IsActive         BIT NOT NULL DEFAULT 1,
    CreatedOn        DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedOn       DATETIME NULL,
    CreatedByUserID INT NULL,
    ModifiedByUserID INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblJob' AND type = 'U')
CREATE TABLE tblJob (
    JobID            INT IDENTITY(1,1) PRIMARY KEY,
    JobCode          NVARCHAR(20)  NULL,
    JobName          NVARCHAR(150) NOT NULL,
    AliasName        NVARCHAR(50)  NULL,
    Description      NVARCHAR(500) NULL,
    IsActive         BIT NOT NULL DEFAULT 1,
    CreatedOn        DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedOn       DATETIME NULL,
    CreatedByUserID INT NULL,
    ModifiedByUserID INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblLeaveCategory' AND type = 'U')
CREATE TABLE tblLeaveCategory (
    LeaveCategoryID   INT IDENTITY(1,1) PRIMARY KEY,
    LeaveCategoryName NVARCHAR(150) NOT NULL,
    IsActive          BIT NOT NULL DEFAULT 1,
    CreatedOn         DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedOn        DATETIME NULL,
    CreatedByUserID INT NULL,
    ModifiedByUserID INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblLeaveApplication' AND type = 'U')
CREATE TABLE tblLeaveApplication (
    LeaveID                      INT IDENTITY(1,1) PRIMARY KEY,
    ApplyingDate                 DATE NOT NULL,
    EmployeeID                   INT NOT NULL,
    LeaveType                    NVARCHAR(50) NOT NULL,
    IsFutureUnplannedLeave       BIT NOT NULL DEFAULT 0,
    LeaveCategoryID              INT NULL,
    LeaveFromDate                DATE NOT NULL,
    LeaveToDate                  DATE NOT NULL,
    NumberOfDays                 INT NOT NULL DEFAULT 0,
    ReasonForLeave               NVARCHAR(1000) NULL,
    TempResponsibleEmployeeID    INT NULL,
    PermanentResponsibleEmployeeID INT NULL,
    CreatedOn                    DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedOn                   DATETIME NULL,
    CreatedByUserID INT NULL,
    ModifiedByUserID INT NULL
);
GO

-- ---------- Home / audit ----------
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblAuditLog' AND type = 'U')
CREATE TABLE tblAuditLog (
    AuditLogID   BIGINT IDENTITY(1,1) PRIMARY KEY,
    ActionAt     DATETIME      NOT NULL DEFAULT GETDATE(),
    UserID       INT           NULL,
    Username     NVARCHAR(100) NULL,
    FormKey      NVARCHAR(100) NULL,
    PagePath     NVARCHAR(200) NULL,
    HandlerName  NVARCHAR(100) NULL,
    ActionType   NVARCHAR(50)  NOT NULL,
    EntityType   NVARCHAR(100) NULL,
    EntityID     INT           NULL,
    EntityName   NVARCHAR(250) NULL,
    Details      NVARCHAR(MAX) NULL,
    IpAddress    NVARCHAR(64)  NULL,
    Success      BIT           NOT NULL DEFAULT 1
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblAuditLog_ActionAt' AND object_id = OBJECT_ID('tblAuditLog'))
    CREATE INDEX IX_tblAuditLog_ActionAt ON tblAuditLog(ActionAt DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblAuditLog_Username' AND object_id = OBJECT_ID('tblAuditLog'))
    CREATE INDEX IX_tblAuditLog_Username ON tblAuditLog(Username);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblNotification' AND type = 'U')
CREATE TABLE tblNotification (
    NotificationID   INT IDENTITY(1,1) PRIMARY KEY,
    NotificationName NVARCHAR(150) NOT NULL,
    Description      NVARCHAR(2000) NULL,
    DepartmentID     INT NULL,
    StartDate        DATE NOT NULL,
    ValidTillDate    DATE NOT NULL,
    IsActive         BIT NOT NULL DEFAULT 1,
    CreatedOn        DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedOn       DATETIME NULL,
    CreatedByUserID INT NULL,
    ModifiedByUserID INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblMemorandum' AND type = 'U')
CREATE TABLE tblMemorandum (
    MemorandumID     INT IDENTITY(1,1) PRIMARY KEY,
    MemorandumName   NVARCHAR(150) NOT NULL,
    Description      NVARCHAR(2000) NULL,
    DepartmentID     INT NULL,
    StartDate        DATE NOT NULL,
    ValidTillDate    DATE NOT NULL,
    IsActive         BIT NOT NULL DEFAULT 1,
    DocumentPath     NVARCHAR(500) NULL,
    OriginalFileName NVARCHAR(260) NULL,
    CreatedOn        DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedOn       DATETIME NULL,
    CreatedByUserID INT NULL,
    ModifiedByUserID INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblGalleryImage' AND type = 'U')
CREATE TABLE tblGalleryImage (
    GalleryImageID   INT IDENTITY(1,1) PRIMARY KEY,
    Title            NVARCHAR(150) NOT NULL,
    Description      NVARCHAR(500) NULL,
    ImagePath        NVARCHAR(500) NOT NULL,
    OriginalFileName NVARCHAR(260) NULL,
    SortOrder        INT NOT NULL DEFAULT 1,
    IsActive         BIT NOT NULL DEFAULT 1,
    CreatedOn        DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedOn       DATETIME NULL,
    CreatedByUserID INT NULL,
    ModifiedByUserID INT NULL
);
GO

-- ---------- Positions ----------
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblPosition' AND type = 'U')
CREATE TABLE tblPosition (
    PositionID          INT IDENTITY(1,1) PRIMARY KEY,
    PositionNo          NVARCHAR(20)  NOT NULL UNIQUE,
    Description         NVARCHAR(500) NULL,
    EmailEmployeeID     INT NULL,
    JobID               INT NULL,
    DepartmentID        INT NULL,
    ReportsToPositionID INT NULL,
    TitleID             INT NULL,
    PositionTypeID      INT NULL,
    PositionDuration    NVARCHAR(50) NULL,
    PositionStartDate   DATE NULL,
    PositionEndDate     DATE NULL,
    IsActive            BIT NOT NULL DEFAULT 1,
    CreatedOn           DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedOn          DATETIME NULL,
    CreatedByUserID INT NULL,
    ModifiedByUserID INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblPositionWorkerAssignment' AND type = 'U')
CREATE TABLE tblPositionWorkerAssignment (
    PositionWorkerAssignmentID INT IDENTITY(1,1) PRIMARY KEY,
    PositionID                 INT NOT NULL,
    EmployeeID                 INT NOT NULL,
    AssignmentStartDate        DATE NULL,
    AssignmentEndDate          DATE NULL,
    Reason                     NVARCHAR(500) NULL,
    SortOrder                  INT NOT NULL DEFAULT 1,
    CreatedOn                  DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedByUserID            INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblPositionBenefitEntitlement' AND type = 'U')
CREATE TABLE tblPositionBenefitEntitlement (
    PositionBenefitEntitlementID INT IDENTITY(1,1) PRIMARY KEY,
    PositionID                   INT NOT NULL,
    BenefitEntitlementID         INT NOT NULL,
    CreatedOn                    DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedByUserID              INT NULL
);
GO

-- ---------- Vendor / PO / SO / inventory ----------
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblVendor' AND type = 'U')
CREATE TABLE tblVendor (
    VendorID                   INT IDENTITY(1,1) PRIMARY KEY,
    VendorCode                 NVARCHAR(20) NOT NULL UNIQUE,
    Name                       NVARCHAR(200) NOT NULL,
    SearchName                 NVARCHAR(200) NULL,
    DealForBranchID            INT NULL,
    CityID                     INT NULL,
    ProvinceID                 INT NULL,
    ModeOfDeliveryID           INT NULL,
    CustomerGroupID            INT NULL,
    CustomerClassID            INT NULL,
    MethodOfPaymentID          INT NULL,
    TermsOfPaymentID           INT NULL,
    CurrencyID                 INT NULL,
    BillPreferenceID           INT NULL,
    FBRStatusID                INT NULL,
    TaxGroupID                 INT NULL,
    CNIC                       NVARCHAR(20) NULL,
    NTN                        NVARCHAR(50) NULL,
    IsCAP                      BIT NOT NULL DEFAULT 0,
    IsMandatoryCreditLimit     BIT NOT NULL DEFAULT 0,
    IsInvoiceHold              BIT NOT NULL DEFAULT 0,
    TotalBusinessPotential     INT NULL,
    TargetBusinessSharePercent DECIMAL(5,2) NULL,
    TargetBusinessAmount       INT NULL,
    CreditLimit                INT NULL,
    AHDCreditLimit             INT NULL,
    PHDCreditLimit             INT NULL,
    HHDCreditLimit             INT NULL,
    IsActive                   BIT NOT NULL DEFAULT 1,
    CreatedOn                  DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedOn                 DATETIME NULL,
    CreatedByUserID            INT NULL,
    ModifiedByUserID           INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblPurchaseOrder' AND type = 'U')
CREATE TABLE tblPurchaseOrder (
    PurchaseOrderID   INT IDENTITY(1,1) PRIMARY KEY,
    PurchaseOrderCode NVARCHAR(20) NOT NULL UNIQUE,
    PurchaseOrderDate DATE NOT NULL,
    VendorID          INT NULL,
    VendorName        NVARCHAR(200) NULL,
    Remarks           NVARCHAR(1000) NULL,
    OrderStatus       NVARCHAR(30) NOT NULL DEFAULT N'Draft',
    TotalQty          DECIMAL(18,4) NOT NULL DEFAULT 0,
    TotalTax          DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalDiscount     DECIMAL(18,2) NOT NULL DEFAULT 0,
    GrandTotal        DECIMAL(18,2) NOT NULL DEFAULT 0,
    CreatedOn         DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedOn        DATETIME NULL,
    CreatedByUserID   INT NULL,
    ModifiedByUserID  INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblPurchaseOrderItem' AND type = 'U')
CREATE TABLE tblPurchaseOrderItem (
    PurchaseOrderItemID INT IDENTITY(1,1) PRIMARY KEY,
    PurchaseOrderID     INT NOT NULL,
    ProductID           INT NULL,
    ProductCode         NVARCHAR(50) NULL,
    ProductDescription  NVARCHAR(300) NULL,
    Qty                 DECIMAL(18,4) NOT NULL DEFAULT 0,
    UnitPrice           DECIMAL(18,4) NOT NULL DEFAULT 0,
    TaxAmount           DECIMAL(18,2) NOT NULL DEFAULT 0,
    DiscountAmount      DECIMAL(18,2) NOT NULL DEFAULT 0,
    NetAmount           DECIMAL(18,2) NOT NULL DEFAULT 0,
    ReceivedQty         DECIMAL(18,4) NOT NULL DEFAULT 0,
    SortOrder           INT NOT NULL DEFAULT 0,
    CreatedOn           DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedByUserID     INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblSalesOrder' AND type = 'U')
CREATE TABLE tblSalesOrder (
    SalesOrderID      INT IDENTITY(1,1) PRIMARY KEY,
    SalesOrderCode    NVARCHAR(20) NOT NULL UNIQUE,
    SalesOrderDate    DATE NOT NULL,
    CustomerID        INT NULL,
    CustomerName      NVARCHAR(200) NULL,
    Remarks           NVARCHAR(1000) NULL,
    OrderStatus       NVARCHAR(30) NOT NULL DEFAULT N'Draft',
    TotalQty          DECIMAL(18,4) NOT NULL DEFAULT 0,
    TotalTax          DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalDiscount     DECIMAL(18,2) NOT NULL DEFAULT 0,
    GrandTotal        DECIMAL(18,2) NOT NULL DEFAULT 0,
    SubmittedOn       DATETIME NULL,
    SubmittedByUserID INT NULL,
    CreatedOn         DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedOn        DATETIME NULL,
    CreatedByUserID   INT NULL,
    ModifiedByUserID  INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblSalesOrderItem' AND type = 'U')
CREATE TABLE tblSalesOrderItem (
    SalesOrderItemID   INT IDENTITY(1,1) PRIMARY KEY,
    SalesOrderID       INT NOT NULL,
    ProductID          INT NULL,
    ProductCode        NVARCHAR(50) NULL,
    ProductDescription NVARCHAR(300) NULL,
    Qty                DECIMAL(18,4) NOT NULL DEFAULT 0,
    UnitPrice          DECIMAL(18,4) NOT NULL DEFAULT 0,
    TaxAmount          DECIMAL(18,2) NOT NULL DEFAULT 0,
    DiscountAmount     DECIMAL(18,2) NOT NULL DEFAULT 0,
    NetAmount          DECIMAL(18,2) NOT NULL DEFAULT 0,
    IssuedQty          DECIMAL(18,4) NOT NULL DEFAULT 0,
    SortOrder          INT NOT NULL DEFAULT 0,
    CreatedOn          DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedByUserID    INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblSalesOrderHistory' AND type = 'U')
CREATE TABLE tblSalesOrderHistory (
    SalesOrderHistoryID INT IDENTITY(1,1) PRIMARY KEY,
    SalesOrderID        INT NOT NULL,
    ActionType          NVARCHAR(50) NOT NULL,
    FromStatus          NVARCHAR(30) NULL,
    ToStatus            NVARCHAR(30) NULL,
    Remarks             NVARCHAR(500) NULL,
    ActionAt            DATETIME NOT NULL DEFAULT GETDATE(),
    ActionByUserID      INT NULL,
    ActionByUsername    NVARCHAR(100) NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblGoodsReceipt' AND type = 'U')
CREATE TABLE tblGoodsReceipt (
    GoodsReceiptID   INT IDENTITY(1,1) PRIMARY KEY,
    GoodsReceiptCode NVARCHAR(20) NOT NULL UNIQUE,
    ReceiptDate      DATE NOT NULL,
    PurchaseOrderID  INT NULL,
    VendorName       NVARCHAR(200) NULL,
    Remarks          NVARCHAR(1000) NULL,
    ReceiptStatus    NVARCHAR(30) NOT NULL DEFAULT N'Posted',
    CreatedOn        DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedByUserID  INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblGoodsReceiptItem' AND type = 'U')
CREATE TABLE tblGoodsReceiptItem (
    GoodsReceiptItemID  INT IDENTITY(1,1) PRIMARY KEY,
    GoodsReceiptID      INT NOT NULL,
    PurchaseOrderItemID INT NULL,
    ProductID           INT NULL,
    ProductCode         NVARCHAR(50) NULL,
    ProductDescription  NVARCHAR(300) NULL,
    ReceivedQty         DECIMAL(18,4) NOT NULL DEFAULT 0,
    UnitCost            DECIMAL(18,4) NOT NULL DEFAULT 0,
    SortOrder           INT NOT NULL DEFAULT 0,
    CreatedOn           DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedByUserID     INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblGoodsIssue' AND type = 'U')
CREATE TABLE tblGoodsIssue (
    GoodsIssueID    INT IDENTITY(1,1) PRIMARY KEY,
    GoodsIssueCode  NVARCHAR(20) NOT NULL UNIQUE,
    IssueDate       DATE NOT NULL,
    SalesOrderID    INT NULL,
    CustomerName    NVARCHAR(200) NULL,
    Remarks         NVARCHAR(1000) NULL,
    IssueStatus     NVARCHAR(30) NOT NULL DEFAULT N'Posted',
    CreatedOn       DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedByUserID INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblGoodsIssueItem' AND type = 'U')
CREATE TABLE tblGoodsIssueItem (
    GoodsIssueItemID   INT IDENTITY(1,1) PRIMARY KEY,
    GoodsIssueID       INT NOT NULL,
    SalesOrderItemID   INT NULL,
    ProductID          INT NULL,
    ProductCode        NVARCHAR(50) NULL,
    ProductDescription NVARCHAR(300) NULL,
    IssuedQty          DECIMAL(18,4) NOT NULL DEFAULT 0,
    UnitCost           DECIMAL(18,4) NOT NULL DEFAULT 0,
    SortOrder          INT NOT NULL DEFAULT 0,
    CreatedOn          DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedByUserID    INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblProductStock' AND type = 'U')
CREATE TABLE tblProductStock (
    ProductStockID   INT IDENTITY(1,1) PRIMARY KEY,
    ProductID        INT NOT NULL UNIQUE,
    QtyOnHand        DECIMAL(18,4) NOT NULL DEFAULT 0,
    AvgUnitCost      DECIMAL(18,4) NOT NULL DEFAULT 0,
    LastReceiptDate  DATETIME NULL,
    LastIssueDate    DATETIME NULL,
    CreatedOn        DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedOn       DATETIME NULL,
    CreatedByUserID  INT NULL,
    ModifiedByUserID INT NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblInventoryTransaction' AND type = 'U')
CREATE TABLE tblInventoryTransaction (
    InventoryTransactionID INT IDENTITY(1,1) PRIMARY KEY,
    TransactionDate        DATETIME NOT NULL DEFAULT GETDATE(),
    TransactionType        NVARCHAR(30) NOT NULL,
    ProductID              INT NOT NULL,
    Qty                    DECIMAL(18,4) NOT NULL,
    UnitCost               DECIMAL(18,4) NULL,
    ReferenceType          NVARCHAR(30) NULL,
    PurchaseOrderID        INT NULL,
    PurchaseOrderItemID    INT NULL,
    SalesOrderID           INT NULL,
    SalesOrderItemID       INT NULL,
    GoodsReceiptID         INT NULL,
    GoodsIssueID           INT NULL,
    Remarks                NVARCHAR(500) NULL,
    CreatedOn              DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedByUserID        INT NULL,
    CreatedByUsername      NVARCHAR(100) NULL
);
GO

-- ---------- Employee FK / profile columns used by the app ----------
IF OBJECT_ID('tblEmployee') IS NOT NULL
BEGIN
    IF COL_LENGTH('tblEmployee','UserID') IS NULL ALTER TABLE tblEmployee ADD UserID INT NULL;
    IF COL_LENGTH('tblEmployee','NationalityID') IS NULL ALTER TABLE tblEmployee ADD NationalityID INT NULL;
    IF COL_LENGTH('tblEmployee','ReligionID') IS NULL ALTER TABLE tblEmployee ADD ReligionID INT NULL;
    IF COL_LENGTH('tblEmployee','LanguageID') IS NULL ALTER TABLE tblEmployee ADD LanguageID INT NULL;
    IF COL_LENGTH('tblEmployee','WorkerCategoryID') IS NULL ALTER TABLE tblEmployee ADD WorkerCategoryID INT NULL;
    IF COL_LENGTH('tblEmployee','EmploymentTypeID') IS NULL ALTER TABLE tblEmployee ADD EmploymentTypeID INT NULL;
    IF COL_LENGTH('tblEmployee','EmploymentStatusID') IS NULL ALTER TABLE tblEmployee ADD EmploymentStatusID INT NULL;
    IF COL_LENGTH('tblEmployee','WorkforceSegmentID') IS NULL ALTER TABLE tblEmployee ADD WorkforceSegmentID INT NULL;
    IF COL_LENGTH('tblEmployee','LegalEntityID') IS NULL ALTER TABLE tblEmployee ADD LegalEntityID INT NULL;
    IF COL_LENGTH('tblEmployee','BusinessUnitID') IS NULL ALTER TABLE tblEmployee ADD BusinessUnitID INT NULL;
    IF COL_LENGTH('tblEmployee','DivisionID') IS NULL ALTER TABLE tblEmployee ADD DivisionID INT NULL;
    IF COL_LENGTH('tblEmployee','SalesTeamID') IS NULL ALTER TABLE tblEmployee ADD SalesTeamID INT NULL;
    IF COL_LENGTH('tblEmployee','CostCenterID') IS NULL ALTER TABLE tblEmployee ADD CostCenterID INT NULL;
    IF COL_LENGTH('tblEmployee','MaritalStatus') IS NULL ALTER TABLE tblEmployee ADD MaritalStatus NVARCHAR(20) NULL;
    IF COL_LENGTH('tblEmployee','TemporaryResponsibleEmployeeID') IS NULL ALTER TABLE tblEmployee ADD TemporaryResponsibleEmployeeID INT NULL;
    IF COL_LENGTH('tblEmployee','PermanentResponsibleEmployeeID') IS NULL ALTER TABLE tblEmployee ADD PermanentResponsibleEmployeeID INT NULL;
END
GO

IF OBJECT_ID('tblDepartment') IS NOT NULL
BEGIN
    IF COL_LENGTH('tblDepartment','WingID') IS NULL ALTER TABLE tblDepartment ADD WingID INT NULL;
    IF COL_LENGTH('tblDepartment','BusinessSegmentID') IS NULL ALTER TABLE tblDepartment ADD BusinessSegmentID INT NULL;
    IF COL_LENGTH('tblDepartment','BusinessUnitID') IS NULL ALTER TABLE tblDepartment ADD BusinessUnitID INT NULL;
END
GO

IF OBJECT_ID('tblUserPermission') IS NOT NULL
BEGIN
    IF COL_LENGTH('tblUserPermission','CanApprove') IS NULL
        ALTER TABLE tblUserPermission ADD CanApprove BIT NOT NULL CONSTRAINT DF_Miss_CanApprove DEFAULT 0;
    IF COL_LENGTH('tblUserPermission','CanExport') IS NULL
        ALTER TABLE tblUserPermission ADD CanExport BIT NOT NULL CONSTRAINT DF_Miss_CanExport DEFAULT 0;
END
GO

IF OBJECT_ID('tblPurchaseOrderItem') IS NOT NULL
   AND COL_LENGTH('tblPurchaseOrderItem','ReceivedQty') IS NULL
    ALTER TABLE tblPurchaseOrderItem ADD ReceivedQty DECIMAL(18,4) NOT NULL CONSTRAINT DF_POI_ReceivedQty DEFAULT 0;
GO

IF OBJECT_ID('tblSalesOrderItem') IS NOT NULL
   AND COL_LENGTH('tblSalesOrderItem','IssuedQty') IS NULL
    ALTER TABLE tblSalesOrderItem ADD IssuedQty DECIMAL(18,4) NOT NULL CONSTRAINT DF_SOI_IssuedQty DEFAULT 0;
GO

-- ---------- Seed lookups (idempotent) ----------
IF OBJECT_ID('tblLeaveCategory') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM tblLeaveCategory)
    INSERT INTO tblLeaveCategory (LeaveCategoryName) VALUES
        (N'Casual Leave'), (N'Annual Leave'), (N'Sick Leave'),
        (N'Maternity Leave'), (N'Unpaid Leave'), (N'Other');
GO

IF OBJECT_ID('tblUnitOfMeasure') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM tblUnitOfMeasure)
    INSERT INTO tblUnitOfMeasure (UnitOfMeasureName, AliasName, IsActive, CreatedOn) VALUES
        (N'Each', N'EA', 1, GETDATE()),
        (N'Kilogram', N'KG', 1, GETDATE()),
        (N'Liter', N'LTR', 1, GETDATE()),
        (N'Box', N'BOX', 1, GETDATE()),
        (N'Pack', N'PK', 1, GETDATE());
GO

IF OBJECT_ID('tblInventoryType') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM tblInventoryType)
    INSERT INTO tblInventoryType (InventoryTypeName, IsActive, CreatedOn) VALUES
        (N'Stock', 1, GETDATE()),
        (N'Non-Stock', 1, GETDATE()),
        (N'Service', 1, GETDATE());
GO

IF OBJECT_ID('tblDocumentType') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM tblDocumentType)
    INSERT INTO tblDocumentType (DocumentTypeName, IsActive, CreatedOn) VALUES
        (N'CNIC', 1, GETDATE()),
        (N'Passport', 1, GETDATE()),
        (N'Contract', 1, GETDATE()),
        (N'Certificate', 1, GETDATE()),
        (N'Other', 1, GETDATE());
GO

-- Sample employee for joins (admin link happens when app starts)
IF OBJECT_ID('tblEmployee') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM tblEmployee WHERE EmployeeCode = N'EMP-00001')
   AND EXISTS (SELECT 1 FROM tblDepartment)
BEGIN
    DECLARE @DeptId INT = (SELECT TOP 1 DepartmentID FROM tblDepartment ORDER BY DepartmentID);
    INSERT INTO tblEmployee
        (EmployeeCode, FirstName, LastName, Gender, Email, Phone, DepartmentID,
         Designation, DateOfJoining, BasicSalary, Status, CreatedOn)
    VALUES
        (N'EMP-00001', N'System', N'Admin', N'Male', N'admin@hrms.local', N'0300-0000000',
         @DeptId, N'System Administrator', CAST(GETDATE() AS DATE), 0, N'Active', GETDATE());
END
GO

PRINT 'LocalDB_MissingTables.sql completed.';
GO

-- ---------- Stored procedures used by legacy Employee Master paths ----------
IF OBJECT_ID('sp_GetDepartments', 'P') IS NOT NULL DROP PROCEDURE sp_GetDepartments;
GO
CREATE PROCEDURE sp_GetDepartments
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DepartmentID, DepartmentName
    FROM tblDepartment
    WHERE ISNULL(IsActive, 1) = 1
    ORDER BY DepartmentName;
END
GO

IF OBJECT_ID('sp_DeleteEmployee', 'P') IS NOT NULL DROP PROCEDURE sp_DeleteEmployee;
GO
CREATE PROCEDURE sp_DeleteEmployee
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM tblEmployeeContact WHERE EmployeeID = @EmployeeID;
    DELETE FROM tblEmployeeAddress WHERE EmployeeID = @EmployeeID;
    DELETE FROM tblEmployeeFamilyMember WHERE EmployeeID = @EmployeeID;
    DELETE FROM tblEmployeeBank WHERE EmployeeID = @EmployeeID;
    DELETE FROM tblEmployeeEducation WHERE EmployeeID = @EmployeeID;
    DELETE FROM tblEmployeeCertificate WHERE EmployeeID = @EmployeeID;
    DELETE FROM tblEmployeeDocument WHERE EmployeeID = @EmployeeID;
    DELETE FROM tblEmployee WHERE EmployeeID = @EmployeeID;
END
GO

-- Verification
SELECT COUNT(*) AS TableCount
FROM sys.tables
WHERE name LIKE N'tbl%';
GO
