using System;
using System.Data.SqlClient;

namespace HRMS.Services
{
/// <summary>
/// Ensures LocalDB / new databases have the core HRMS tables that Script.sql creates.
/// Program.cs migrations historically assumed tblEmployee already existed.
/// </summary>
public static class DatabaseBootstrap
{
    public static void EnsureCoreSchema(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            ConnectTimeout = 15
        };

        using var conn = new SqlConnection(builder.ConnectionString);
        conn.Open();

        foreach (var sql in CoreTableSql)
        {
            try
            {
                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 30;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[HRMS] Core schema step warning: " + ex.Message);
            }
        }

        SeedLookupsAndSampleEmployee(conn);
    }

    private static void SeedLookupsAndSampleEmployee(SqlConnection conn)
    {
        using (var cmd = new SqlCommand(@"
            IF NOT EXISTS (SELECT 1 FROM tblDivision)
                INSERT INTO tblDivision (DivisionName) VALUES
                    ('Corporate'), ('Operations'), ('Commercial'), ('Support Services');

            IF NOT EXISTS (SELECT 1 FROM tblDepartment)
                INSERT INTO tblDepartment (DepartmentName) VALUES
                    ('Human Resources'), ('Finance'), ('Information Technology'),
                    ('Operations'), ('Marketing'), ('Sales'), ('Administration');

            IF OBJECT_ID('tblGender') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM tblGender)
                INSERT INTO tblGender (GenderName) VALUES ('Male'), ('Female'), ('Other');

            IF OBJECT_ID('tblBloodGroup') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM tblBloodGroup)
                INSERT INTO tblBloodGroup (BloodGroupName) VALUES
                    ('A+'), ('A-'), ('B+'), ('B-'), ('AB+'), ('AB-'), ('O+'), ('O-');
            ", conn))
        {
            cmd.ExecuteNonQuery();
        }

        // Sample employee so pages that join tblEmployee have at least one row
        using (var cmd = new SqlCommand(@"
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

            -- Link sample employee to admin user when both exist
            IF OBJECT_ID('tblEmployee') IS NOT NULL AND OBJECT_ID('tblUser') IS NOT NULL
               AND COL_LENGTH('tblEmployee', 'UserID') IS NOT NULL
            BEGIN
                DECLARE @AdminUserId INT = (SELECT TOP 1 UserID FROM tblUser WHERE Username = N'admin');
                DECLARE @EmpId INT = (SELECT TOP 1 EmployeeID FROM tblEmployee WHERE EmployeeCode = N'EMP-00001');
                IF @AdminUserId IS NOT NULL AND @EmpId IS NOT NULL
                    UPDATE tblEmployee SET UserID = @AdminUserId WHERE EmployeeID = @EmpId AND (UserID IS NULL OR UserID = 0);
            END
            ", conn))
        {
            try { cmd.ExecuteNonQuery(); }
            catch (Exception ex) { Console.WriteLine("[HRMS] Sample employee seed: " + ex.Message); }
        }
    }

    private static readonly string[] CoreTableSql =
    {
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblDivision' AND type = 'U')
          CREATE TABLE tblDivision (
              DivisionID   INT IDENTITY(1,1) PRIMARY KEY,
              DivisionName NVARCHAR(100) NOT NULL UNIQUE,
              AliasName    NVARCHAR(50)  NULL,
              IsActive     BIT NOT NULL DEFAULT 1,
              CreatedOn    DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn   DATETIME NULL,
              CreatedByUserID INT NULL,
              ModifiedByUserID INT NULL
          );",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblDepartment' AND type = 'U')
          CREATE TABLE tblDepartment (
              DepartmentID   INT IDENTITY(1,1) PRIMARY KEY,
              DivisionID     INT NULL,
              DepartmentName NVARCHAR(100) NOT NULL,
              IsActive       BIT NOT NULL DEFAULT 1,
              CreatedOn      DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn     DATETIME NULL,
              CreatedByUserID INT NULL,
              ModifiedByUserID INT NULL
          );",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblGender' AND type = 'U')
          CREATE TABLE tblGender (
              GenderID   INT IDENTITY(1,1) PRIMARY KEY,
              GenderName NVARCHAR(50) NOT NULL UNIQUE,
              IsActive   BIT NOT NULL DEFAULT 1,
              CreatedOn  DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn DATETIME NULL
          );",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblBloodGroup' AND type = 'U')
          CREATE TABLE tblBloodGroup (
              BloodGroupID   INT IDENTITY(1,1) PRIMARY KEY,
              BloodGroupName NVARCHAR(50) NOT NULL UNIQUE,
              IsActive       BIT NOT NULL DEFAULT 1,
              CreatedOn      DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn     DATETIME NULL
          );",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblEmployee' AND type = 'U')
          CREATE TABLE tblEmployee (
              EmployeeID     INT IDENTITY(1,1) PRIMARY KEY,
              EmployeeCode   NVARCHAR(20)  NOT NULL UNIQUE,
              FirstName      NVARCHAR(100) NOT NULL,
              LastName       NVARCHAR(100) NOT NULL,
              Gender         NVARCHAR(10)  NOT NULL CONSTRAINT DF_tblEmployee_Gender DEFAULT N'Male',
              DateOfBirth    DATE NULL,
              Email          NVARCHAR(150) NULL,
              Phone          NVARCHAR(20)  NULL,
              DepartmentID   INT NULL,
              Designation    NVARCHAR(100) NOT NULL CONSTRAINT DF_tblEmployee_Desig DEFAULT N'Staff',
              DateOfJoining  DATE NOT NULL CONSTRAINT DF_tblEmployee_DOJ DEFAULT GETDATE(),
              BasicSalary    DECIMAL(12,2) NOT NULL DEFAULT 0,
              Address        NVARCHAR(300) NULL,
              PersonalEmail  NVARCHAR(150) NULL,
              OfficialEmail  NVARCHAR(150) NULL,
              PersonalMobile NVARCHAR(20) NULL,
              OfficialMobile NVARCHAR(20) NULL,
              WhatsAppNumber NVARCHAR(20) NULL,
              EmergencyContactName NVARCHAR(100) NULL,
              EmergencyContactRelationship NVARCHAR(50) NULL,
              EmergencyContactNumber NVARCHAR(20) NULL,
              CurrentAddress NVARCHAR(500) NULL,
              CurrentCity NVARCHAR(100) NULL,
              CurrentProvince NVARCHAR(100) NULL,
              PostalCode NVARCHAR(10) NULL,
              PermanentSameAsCurrent BIT NOT NULL DEFAULT 1,
              PermanentAddress NVARCHAR(500) NULL,
              Status NVARCHAR(20) NOT NULL DEFAULT N'Active',
              UserID INT NULL,
              GenderID INT NULL,
              BloodGroupID INT NULL,
              CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn DATETIME NULL,
              CreatedByUserID INT NULL,
              ModifiedByUserID INT NULL
          );",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblEmployeeContact' AND type = 'U')
          CREATE TABLE tblEmployeeContact (
              ContactID INT IDENTITY(1,1) PRIMARY KEY,
              EmployeeID INT NOT NULL,
              ContactType NVARCHAR(50) NOT NULL,
              ContactName NVARCHAR(100) NULL,
              Relationship NVARCHAR(50) NULL,
              ContactValue NVARCHAR(255) NULL,
              IsPrimary BIT NOT NULL DEFAULT 0,
              SortOrder INT NOT NULL DEFAULT 1,
              CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn DATETIME NULL
          );",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblEmployeeAddress' AND type = 'U')
          CREATE TABLE tblEmployeeAddress (
              AddressID INT IDENTITY(1,1) PRIMARY KEY,
              EmployeeID INT NOT NULL,
              AddressType NVARCHAR(50) NOT NULL,
              AddressLine NVARCHAR(500) NOT NULL,
              City NVARCHAR(100) NULL,
              ProvinceState NVARCHAR(100) NULL,
              PostalCode NVARCHAR(10) NULL,
              IsPrimary BIT NOT NULL DEFAULT 0,
              SortOrder INT NOT NULL DEFAULT 1,
              CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn DATETIME NULL
          );",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblEmployeeFamilyMember' AND type = 'U')
          CREATE TABLE tblEmployeeFamilyMember (
              FamilyMemberID INT IDENTITY(1,1) PRIMARY KEY,
              EmployeeID INT NOT NULL,
              MemberName NVARCHAR(150) NOT NULL,
              Relationship NVARCHAR(50) NULL,
              Gender NVARCHAR(20) NULL,
              DateOfBirth DATE NULL,
              ContactNumber NVARCHAR(20) NULL,
              IsDependent BIT NOT NULL DEFAULT 1,
              SortOrder INT NOT NULL DEFAULT 1,
              CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn DATETIME NULL
          );",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblBankMaster' AND type = 'U')
          CREATE TABLE tblBankMaster (
              BankID INT IDENTITY(1,1) PRIMARY KEY,
              BankName NVARCHAR(150) NOT NULL,
              BankCode NVARCHAR(50) NULL,
              LocationName NVARCHAR(150) NULL,
              AccountTitle NVARCHAR(150) NULL,
              BankGroupID INT NULL,
              IBAN NVARCHAR(50) NULL,
              SwiftBICCode NVARCHAR(50) NULL,
              CurrencyCode NVARCHAR(50) NULL,
              AccountVerificationStatus NVARCHAR(50) NOT NULL DEFAULT N'Pending',
              IsActive BIT NOT NULL DEFAULT 1,
              CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn DATETIME NULL
          );
          IF OBJECT_ID(N'tblBankMaster', N'U') IS NOT NULL AND COL_LENGTH(N'tblBankMaster', N'AccountTitle') IS NULL
              ALTER TABLE tblBankMaster ADD AccountTitle NVARCHAR(150) NULL;",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblEmployeeBank' AND type = 'U')
          CREATE TABLE tblEmployeeBank (
              EmployeeBankID INT IDENTITY(1,1) PRIMARY KEY,
              EmployeeID INT NOT NULL,
              BankID INT NULL,
              BankCode NVARCHAR(50) NULL,
              LocationName NVARCHAR(150) NULL,
              BankGroupID INT NULL,
              IBAN NVARCHAR(50) NULL,
              SwiftBICCode NVARCHAR(50) NULL,
              CurrencyCode NVARCHAR(50) NULL,
              AccountVerificationStatus NVARCHAR(50) NOT NULL DEFAULT N'Pending',
              IsPrimary BIT NOT NULL DEFAULT 0,
              SortOrder INT NOT NULL DEFAULT 1,
              CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn DATETIME NULL
          );",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblAppForm' AND type = 'U')
          CREATE TABLE tblAppForm (
              FormID INT IDENTITY(1,1) PRIMARY KEY,
              FormKey NVARCHAR(80) NOT NULL UNIQUE,
              FormName NVARCHAR(150) NOT NULL,
              PagePath NVARCHAR(200) NOT NULL,
              Category NVARCHAR(80) NOT NULL,
              SortOrder INT NOT NULL DEFAULT 0,
              IsActive BIT NOT NULL DEFAULT 1,
              CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn DATETIME NULL
          );",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblUser' AND type = 'U')
          CREATE TABLE tblUser (
              UserID INT IDENTITY(1,1) PRIMARY KEY,
              UserCode NVARCHAR(20) NULL,
              Username NVARCHAR(50) NOT NULL UNIQUE,
              PasswordHash NVARCHAR(200) NOT NULL,
              FullName NVARCHAR(100) NOT NULL,
              Email NVARCHAR(100) NULL,
              IsActive BIT NOT NULL DEFAULT 1,
              IsAdmin BIT NOT NULL DEFAULT 0,
              CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn DATETIME NULL
          );",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblUserPermission' AND type = 'U')
          CREATE TABLE tblUserPermission (
              UserPermissionID INT IDENTITY(1,1) PRIMARY KEY,
              UserID INT NOT NULL,
              FormKey NVARCHAR(80) NOT NULL,
              CanRead BIT NOT NULL DEFAULT 0,
              CanWrite BIT NOT NULL DEFAULT 0,
              CanDelete BIT NOT NULL DEFAULT 0,
              CanApprove BIT NOT NULL DEFAULT 0,
              CanExport BIT NOT NULL DEFAULT 0,
              CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn DATETIME NULL,
              CONSTRAINT UQ_UserForm UNIQUE (UserID, FormKey)
          );",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblGalleryImage' AND type = 'U')
          CREATE TABLE tblGalleryImage (
              GalleryImageID INT IDENTITY(1,1) PRIMARY KEY,
              Title NVARCHAR(200) NOT NULL,
              Description NVARCHAR(500) NULL,
              ImagePath NVARCHAR(500) NOT NULL,
              SortOrder INT NOT NULL DEFAULT 0,
              IsActive BIT NOT NULL DEFAULT 1,
              CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn DATETIME NULL
          );",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblNotification' AND type = 'U')
          CREATE TABLE tblNotification (
              NotificationID INT IDENTITY(1,1) PRIMARY KEY,
              NotificationName NVARCHAR(200) NOT NULL,
              Description NVARCHAR(MAX) NULL,
              DepartmentID INT NULL,
              StartDate DATE NOT NULL,
              ValidTillDate DATE NOT NULL,
              IsActive BIT NOT NULL DEFAULT 1,
              CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn DATETIME NULL
          );",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblMemorandum' AND type = 'U')
          CREATE TABLE tblMemorandum (
              MemorandumID INT IDENTITY(1,1) PRIMARY KEY,
              MemorandumName NVARCHAR(200) NOT NULL,
              Description NVARCHAR(MAX) NULL,
              DepartmentID INT NULL,
              StartDate DATE NOT NULL,
              ValidTillDate DATE NOT NULL,
              DocumentPath NVARCHAR(500) NULL,
              OriginalFileName NVARCHAR(255) NULL,
              IsActive BIT NOT NULL DEFAULT 1,
              CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn DATETIME NULL
          );",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblAuditLog' AND type = 'U')
          CREATE TABLE tblAuditLog (
              AuditLogID INT IDENTITY(1,1) PRIMARY KEY,
              ActionAt DATETIME NOT NULL DEFAULT GETDATE(),
              UserID INT NULL,
              Username NVARCHAR(100) NULL,
              FormKey NVARCHAR(80) NULL,
              PagePath NVARCHAR(200) NULL,
              HandlerName NVARCHAR(100) NULL,
              ActionType NVARCHAR(50) NOT NULL,
              EntityType NVARCHAR(100) NULL,
              EntityID INT NULL,
              EntityName NVARCHAR(250) NULL,
              Details NVARCHAR(MAX) NULL,
              IpAddress NVARCHAR(50) NULL,
              Success BIT NOT NULL DEFAULT 1
          );",
    };
}
}
