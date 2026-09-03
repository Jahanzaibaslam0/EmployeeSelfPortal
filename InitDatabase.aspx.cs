using System;
using System.Data.SqlClient;
using System.Text;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class InitDatabasePage : AppBasePage
    {
        protected override bool IsPublicPage => true;

        public string Message { get; private set; } = "";
        public string AlertType { get; private set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                RunInit();
                return;
            }

            var run = QueryInt("run");
            var skip = Request.QueryString["skip"];
            if (run == 1 || string.IsNullOrEmpty(skip))
                RunInit();
        }

        private void RunInit()
        {
            var log = new StringBuilder();
            try
            {
                log.AppendLine("1) Connecting to database...");
                using (var probe = new SqlConnection(Conn))
                {
                    probe.Open();
                    log.AppendLine("   Connected OK.");
                }

                log.AppendLine("2) Creating core tables (tblEmployee, tblDepartment, ...)");
                DatabaseBootstrap.EnsureCoreSchema(Conn);
                log.AppendLine("   Core schema done.");

                log.AppendLine("3) Creating / resetting admin user...");
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    var hash = PasswordHelper.HashPassword("Admin@123");
                    using (var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT 1 FROM tblUser WHERE Username = N'admin')
                    INSERT INTO tblUser (UserCode, Username, PasswordHash, FullName, Email, IsActive, IsAdmin, CreatedOn)
                    VALUES (N'GB-US-00001', N'admin', @Hash, N'System Administrator', N'admin@hrms.local', 1, 1, GETDATE());
                ELSE
                    UPDATE tblUser SET PasswordHash=@Hash, IsActive=1, IsAdmin=1, ModifiedOn=GETDATE()
                    WHERE Username=N'admin';", conn))
                    {
                        cmd.CommandTimeout = 30;
                        cmd.Parameters.AddWithValue("@Hash", hash);
                        cmd.ExecuteNonQuery();
                    }
                    log.AppendLine("   Admin ready.");

                    int empCount = 0, userCount = 0, deptCount = 0;
                    using (var c1 = new SqlCommand("SELECT COUNT(*) FROM tblEmployee", conn))
                    { c1.CommandTimeout = 15; empCount = Convert.ToInt32(c1.ExecuteScalar()); }
                    using (var c2 = new SqlCommand("SELECT COUNT(*) FROM tblUser", conn))
                    { c2.CommandTimeout = 15; userCount = Convert.ToInt32(c2.ExecuteScalar()); }
                    using (var c3 = new SqlCommand("SELECT COUNT(*) FROM tblDepartment", conn))
                    { c3.CommandTimeout = 15; deptCount = Convert.ToInt32(c3.ExecuteScalar()); }

                    log.AppendLine();
                    log.AppendLine("SUCCESS");
                    log.AppendLine("Users: " + userCount);
                    log.AppendLine("Departments: " + deptCount);
                    log.AppendLine("Employees: " + empCount);
                    log.AppendLine();
                    log.AppendLine("Login: admin / Admin@123");
                }

                Message = log.ToString();
                AlertType = "success";
            }
            catch (Exception ex)
            {
                log.AppendLine();
                log.AppendLine("FAILED");
                log.AppendLine(ex.Message);
                if (ex.InnerException != null)
                    log.AppendLine(ex.InnerException.Message);
                Message = log.ToString();
                AlertType = "error";
            }
        }
    }
}
