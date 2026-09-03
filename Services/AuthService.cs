using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;
using System.Web.SessionState;

namespace HRMS.Services
{
    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    public class AuthService
    {
        private readonly string _conn;

        public AuthService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        private HttpSessionState Session => HttpContext.Current.Session;

        public bool IsLoggedIn => CurrentUserId.HasValue;
        public int? CurrentUserId => SessionHelper.GetInt32(Session, "UserID");
        public string CurrentUsername => SessionHelper.GetString(Session, "Username");
        public string CurrentFullName => SessionHelper.GetString(Session, "FullName");
        public bool IsAdmin => SessionHelper.GetInt32(Session, "IsAdmin") == 1;
        public int? LinkedEmployeeId => SessionHelper.GetInt32(Session, "LinkedEmployeeID");

        public LoginResult Login(string username, string password)
        {
            username = (username ?? "").Trim();
            password = password ?? "";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return new LoginResult { Success = false, Message = "Username and password are required." };

            try
            {
                EnsureUserTableAndAdmin();

                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
                    SELECT UserID, Username, PasswordHash, FullName, IsActive, IsAdmin
                    FROM tblUser WHERE Username = @Username;", conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    conn.Open();

                    using (var dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                            return new LoginResult { Success = false, Message = "Invalid username or password." };

                        if (!dr.GetBoolean(dr.GetOrdinal("IsActive")))
                            return new LoginResult { Success = false, Message = "This account is inactive." };

                        var hash = dr.GetString(dr.GetOrdinal("PasswordHash"));
                        if (!PasswordHelper.VerifyPassword(password, hash))
                            return new LoginResult { Success = false, Message = "Invalid username or password." };

                        var userId = dr.GetInt32(dr.GetOrdinal("UserID"));
                        var isAdmin = dr.GetBoolean(dr.GetOrdinal("IsAdmin"));
                        var fullName = dr.IsDBNull(dr.GetOrdinal("FullName")) ? username : dr.GetString(dr.GetOrdinal("FullName"));
                        dr.Close();

                        int? linkedEmployeeId = null;
                        try
                        {
                            using (var empCmd = new SqlCommand(@"
                                SELECT TOP 1 EmployeeID FROM tblEmployee
                                WHERE UserID = @UserID ORDER BY EmployeeID;", conn))
                            {
                                empCmd.Parameters.AddWithValue("@UserID", userId);
                                var result = empCmd.ExecuteScalar();
                                if (result != null && result != DBNull.Value)
                                    linkedEmployeeId = Convert.ToInt32(result);
                            }
                        }
                        catch { }

                        SessionHelper.SetInt32(Session, "UserID", userId);
                        SessionHelper.SetString(Session, "Username", username);
                        SessionHelper.SetString(Session, "FullName", fullName);
                        SessionHelper.SetInt32(Session, "IsAdmin", isAdmin ? 1 : 0);
                        if (linkedEmployeeId.HasValue)
                            SessionHelper.SetInt32(Session, "LinkedEmployeeID", linkedEmployeeId.Value);
                        else
                            Session.Remove("LinkedEmployeeID");

                        return new LoginResult { Success = true };
                    }
                }
            }
            catch (SqlException ex)
            {
                return new LoginResult { Success = false, Message = "Database connection failed: " + ex.Message };
            }
            catch (Exception ex)
            {
                return new LoginResult { Success = false, Message = "Login error: " + ex.Message };
            }
        }

        public void EnsureUserTableAndAdmin()
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var create = new SqlCommand(@"
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
                    create.ExecuteNonQuery();
                }

                var hash = PasswordHelper.HashPassword("Admin@123");
                using (var upsert = new SqlCommand(@"
                    IF NOT EXISTS (SELECT 1 FROM tblUser WHERE Username = N'admin')
                        INSERT INTO tblUser (UserCode, Username, PasswordHash, FullName, Email, IsActive, IsAdmin, CreatedOn)
                        VALUES (N'GB-US-00001', N'admin', @Hash, N'System Administrator', N'admin@hrms.local', 1, 1, GETDATE());
                    ELSE
                        UPDATE tblUser SET PasswordHash = @Hash, IsActive = 1, IsAdmin = 1, ModifiedOn = GETDATE()
                        WHERE Username = N'admin';", conn))
                {
                    upsert.Parameters.AddWithValue("@Hash", hash);
                    upsert.ExecuteNonQuery();
                }
            }
        }

        public void Logout()
        {
            Session.Clear();
        }
    }
}
