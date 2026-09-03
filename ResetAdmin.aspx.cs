using System;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class ResetAdminPage : AppBasePage
    {
        protected override bool IsPublicPage => true;

        public string Message { get; private set; } = "";
        public string AlertType { get; private set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                Reset();
                return;
            }
            if (QueryInt("run") == 1)
                Reset();
        }

        private void Reset()
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();

                    using (var ensure = new SqlCommand(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblUser' AND type = 'U')
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
                );", conn))
                    {
                        ensure.CommandTimeout = 30;
                        ensure.ExecuteNonQuery();
                    }

                    var hash = PasswordHelper.HashPassword("Admin@123");
                    using (var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT 1 FROM tblUser WHERE Username = 'admin')
                    INSERT INTO tblUser (UserCode, Username, PasswordHash, FullName, Email, IsActive, IsAdmin, CreatedOn)
                    VALUES ('GB-US-00001', 'admin', @Hash, 'System Administrator', 'admin@hrms.local', 1, 1, GETDATE());
                ELSE
                    UPDATE tblUser
                    SET PasswordHash = @Hash, IsActive = 1, IsAdmin = 1, ModifiedOn = GETDATE()
                    WHERE Username = 'admin';", conn))
                    {
                        cmd.CommandTimeout = 30;
                        cmd.Parameters.AddWithValue("@Hash", hash);
                        cmd.ExecuteNonQuery();
                    }
                }

                Message = "Admin ready. Username: admin / Password: Admin@123";
                AlertType = "success";
            }
            catch (Exception ex)
            {
                Message = "Failed: " + ex.Message;
                AlertType = "error";
            }
        }
    }
}
