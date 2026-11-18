using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

[IgnoreAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class InitDatabaseModel : PageModel
{
    private readonly string _conn;

    public InitDatabaseModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")
            ?? throw new InvalidOperationException("HRMSConnection is missing in appsettings.json");
    }

    public string Message { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? run)
    {
        // Auto-run on first visit, or when ?run=1
        if (run == 1 || string.IsNullOrEmpty(Request.Query["skip"]))
            RunInit();
    }

    public IActionResult OnPost()
    {
        RunInit();
        return Page();
    }

    private void RunInit()
    {
        var log = new System.Text.StringBuilder();
        try
        {
            log.AppendLine("1) Connecting to database...");
            using (var probe = new SqlConnection(_conn))
            {
                probe.Open();
                log.AppendLine("   Connected OK.");
            }

            log.AppendLine("2) Creating core tables (tblEmployee, tblDepartment, ...)");
            DatabaseBootstrap.EnsureCoreSchema(_conn);
            log.AppendLine("   Core schema done.");

            log.AppendLine("3) Creating / resetting admin user...");
            using var conn = new SqlConnection(_conn);
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
            using (var c1 = new SqlCommand("SELECT COUNT(*) FROM tblEmployee", conn)) { c1.CommandTimeout = 15; empCount = Convert.ToInt32(c1.ExecuteScalar()); }
            using (var c2 = new SqlCommand("SELECT COUNT(*) FROM tblUser", conn)) { c2.CommandTimeout = 15; userCount = Convert.ToInt32(c2.ExecuteScalar()); }
            using (var c3 = new SqlCommand("SELECT COUNT(*) FROM tblDepartment", conn)) { c3.CommandTimeout = 15; deptCount = Convert.ToInt32(c3.ExecuteScalar()); }

            log.AppendLine();
            log.AppendLine("SUCCESS");
            log.AppendLine($"Users: {userCount}");
            log.AppendLine($"Departments: {deptCount}");
            log.AppendLine($"Employees: {empCount}");
            log.AppendLine();
            log.AppendLine("Login: admin / Admin@123");

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
