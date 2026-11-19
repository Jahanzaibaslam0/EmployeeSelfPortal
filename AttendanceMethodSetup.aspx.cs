using System;
using System.Data.SqlClient;
using HRMS.Services;

namespace HRMS
{
    public partial class AttendanceMethodSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblAttendanceMethod";
        protected override string IdColumn => "AttendanceMethodID";
        protected override string NameColumn => "AttendanceMethodName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Attendance Method Setup";
        public override string ItemLabel => "Attendance Method";
        public override string PagePath => "/AttendanceMethodSetup";

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
IF OBJECT_ID(N'dbo.tblAttendanceMethod', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAttendanceMethod (
        AttendanceMethodID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AttendanceMethodName NVARCHAR(100) NOT NULL UNIQUE,
        AliasName NVARCHAR(50) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tblAttendanceMethod_IsActive DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblAttendanceMethod_CreatedOn DEFAULT (GETDATE()),
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
