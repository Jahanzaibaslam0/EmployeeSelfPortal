using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

[IgnoreAntiforgeryToken]
public class ResetAdminModel : PageModel
{
    private readonly string _conn;

    public ResetAdminModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public string Message { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? run)
    {
        if (run == 1) Reset();
    }

    private void Reset()
    {
        try
        {
            using var conn = new SqlConnection(_conn);
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
            using var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT 1 FROM tblUser WHERE Username = 'admin')
                    INSERT INTO tblUser (UserCode, Username, PasswordHash, FullName, Email, IsActive, IsAdmin, CreatedOn)
                    VALUES ('GB-US-00001', 'admin', @Hash, 'System Administrator', 'admin@hrms.local', 1, 1, GETDATE());
                ELSE
                    UPDATE tblUser
                    SET PasswordHash = @Hash, IsActive = 1, IsAdmin = 1, ModifiedOn = GETDATE()
                    WHERE Username = 'admin';", conn);
            cmd.CommandTimeout = 30;
            cmd.Parameters.AddWithValue("@Hash", hash);
            cmd.ExecuteNonQuery();

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
