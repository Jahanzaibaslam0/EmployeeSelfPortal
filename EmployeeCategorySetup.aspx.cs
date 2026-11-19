using System;
using System.Data.SqlClient;
using HRMS.Services;

namespace HRMS
{
    public partial class EmployeeCategorySetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblEmployeeCategory";
        protected override string IdColumn => "EmployeeCategoryID";
        protected override string NameColumn => "EmployeeCategoryName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Employee Category Setup";
        public override string ItemLabel => "Employee Category";
        public override string PagePath => "/EmployeeCategorySetup";

        protected override void OnInit(EventArgs e)
        {
            EnsureTable();
            base.OnInit(e);
        }

        internal static void EnsureTable(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return;
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'dbo.tblEmployeeCategory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblEmployeeCategory (
        EmployeeCategoryID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmployeeCategoryName NVARCHAR(150) NOT NULL,
        AliasName NVARCHAR(50) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tblEmployeeCategory_IsActive DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblEmployeeCategory_CreatedOn DEFAULT (GETDATE()),
        ModifiedOn DATETIME NULL,
        CreatedByUserID INT NULL,
        ModifiedByUserID INT NULL
    );
END;", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
            }
        }

        private void EnsureTable()
        {
            EnsureTable(Conn);
        }
    }
}
