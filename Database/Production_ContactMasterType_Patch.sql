-- Contact Master type links: VendorID, EmployeeID, GuestName.
-- Safe to re-run.

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
