-- HRMS – Profile change HR approval workflow (internal only, no email).
SET NOCOUNT ON;
GO

IF OBJECT_ID('tblProfileChangeRequest','U') IS NULL
CREATE TABLE tblProfileChangeRequest (
    RequestID INT IDENTITY(1,1) PRIMARY KEY,
    RequestCode NVARCHAR(20) NOT NULL UNIQUE,
    EmployeeID INT NOT NULL,
    SubmittedByUserID INT NOT NULL,
    Status NVARCHAR(30) NOT NULL,
    SubmittedOn DATETIME NOT NULL DEFAULT GETDATE(),
    DecidedOn DATETIME NULL,
    DecidedByUserID INT NULL,
    DecisionComments NVARCHAR(1000) NULL,
    CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedOn DATETIME NULL
);
GO

IF OBJECT_ID('tblProfileChangeLine','U') IS NULL
CREATE TABLE tblProfileChangeLine (
    LineID INT IDENTITY(1,1) PRIMARY KEY,
    RequestID INT NOT NULL,
    FieldKey NVARCHAR(80) NOT NULL,
    FieldLabel NVARCHAR(150) NOT NULL,
    Section NVARCHAR(40) NOT NULL,
    OldValue NVARCHAR(MAX) NULL,
    NewValue NVARCHAR(MAX) NULL,
    OldJson NVARCHAR(MAX) NULL,
    NewJson NVARCHAR(MAX) NULL,
    IsSnapshot BIT NOT NULL DEFAULT 0
);
GO

IF OBJECT_ID('tblProfileChangeHistory','U') IS NULL
CREATE TABLE tblProfileChangeHistory (
    HistoryID INT IDENTITY(1,1) PRIMARY KEY,
    RequestID INT NOT NULL,
    Status NVARCHAR(30) NOT NULL,
    ActorUserID INT NOT NULL,
    Comments NVARCHAR(1000) NULL,
    ActionOn DATETIME NOT NULL DEFAULT GETDATE()
);
GO

IF OBJECT_ID('tblAppInbox','U') IS NULL
CREATE TABLE tblAppInbox (
    InboxID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(2000) NULL,
    LinkUrl NVARCHAR(300) NULL,
    RelatedType NVARCHAR(40) NULL,
    RelatedID INT NULL,
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
    ReadOn DATETIME NULL
);
GO

IF OBJECT_ID('tblAppForm','U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM tblAppForm WHERE FormKey = N'ProfileChange')
    INSERT INTO tblAppForm (FormKey, FormName, PagePath, Category, SortOrder)
    VALUES (N'ProfileChange', N'Profile Update Requests', N'/ProfileChangeReview.aspx', N'Transactions', 2);
GO

PRINT 'Production_ProfileChange_Patch.sql completed.';
GO
