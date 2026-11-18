-- HRMS – Account security (profile, password policy, TOTP 2FA, recovery, lockout)
-- Run on production SQL if the app login cannot ALTER TABLE / CREATE TABLE.
-- Fully internal: no email, SMS, or cloud MFA.

SET NOCOUNT ON;
GO

IF COL_LENGTH('tblUser','TwoFactorEnabled') IS NULL
    ALTER TABLE tblUser ADD TwoFactorEnabled BIT NOT NULL CONSTRAINT DF_tblUser_TwoFactor DEFAULT 0;
GO
IF COL_LENGTH('tblUser','TwoFactorSecret') IS NULL
    ALTER TABLE tblUser ADD TwoFactorSecret NVARCHAR(64) NULL;
GO
IF COL_LENGTH('tblUser','TwoFactorConfirmedOn') IS NULL
    ALTER TABLE tblUser ADD TwoFactorConfirmedOn DATETIME NULL;
GO
IF COL_LENGTH('tblUser','MustChangePassword') IS NULL
    ALTER TABLE tblUser ADD MustChangePassword BIT NOT NULL CONSTRAINT DF_tblUser_MustChange DEFAULT 0;
GO
IF COL_LENGTH('tblUser','FailedLoginCount') IS NULL
    ALTER TABLE tblUser ADD FailedLoginCount INT NOT NULL CONSTRAINT DF_tblUser_FailCount DEFAULT 0;
GO
IF COL_LENGTH('tblUser','LockoutUntil') IS NULL
    ALTER TABLE tblUser ADD LockoutUntil DATETIME NULL;
GO
IF COL_LENGTH('tblUser','PasswordChangedOn') IS NULL
    ALTER TABLE tblUser ADD PasswordChangedOn DATETIME NULL;
GO
IF COL_LENGTH('tblUser','SecurityStamp') IS NULL
    ALTER TABLE tblUser ADD SecurityStamp NVARCHAR(36) NULL;
GO

IF OBJECT_ID('tblUserRecoveryCode','U') IS NULL
CREATE TABLE tblUserRecoveryCode (
    RecoveryCodeID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    CodeHash NVARCHAR(200) NOT NULL,
    UsedOn DATETIME NULL,
    CreatedOn DATETIME NOT NULL DEFAULT GETDATE()
);
GO

IF OBJECT_ID('tblUserSecurityQuestion','U') IS NULL
CREATE TABLE tblUserSecurityQuestion (
    UserQuestionID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    QuestionKey NVARCHAR(80) NOT NULL,
    AnswerHash NVARCHAR(200) NOT NULL,
    CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_UserQuestion UNIQUE (UserID, QuestionKey)
);
GO

IF OBJECT_ID('tblAuthThrottle','U') IS NULL
CREATE TABLE tblAuthThrottle (
    ThrottleID INT IDENTITY(1,1) PRIMARY KEY,
    ThrottleKey NVARCHAR(200) NOT NULL UNIQUE,
    FailedCount INT NOT NULL DEFAULT 0,
    WindowStart DATETIME NOT NULL DEFAULT GETDATE(),
    LockoutUntil DATETIME NULL
);
GO

IF OBJECT_ID('tblAppForm','U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM tblAppForm WHERE FormKey = N'AccountSettings')
    INSERT INTO tblAppForm (FormKey, FormName, PagePath, Category, SortOrder)
    VALUES (N'AccountSettings', N'Account Settings', N'/AccountSettings.aspx', N'Transactions', 2);
GO

PRINT 'Production_AccountSecurity_Patch.sql completed.';
GO
