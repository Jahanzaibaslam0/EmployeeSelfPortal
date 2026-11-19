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
        public bool RequiresTwoFactor { get; set; }
        public string Message { get; set; } = "";
    }

    public class AuthService
    {
        public const string GenericLoginError = "Invalid username or password.";
        private const int PendingTwoFactorMinutes = 5;

        private readonly string _conn;
        private AccountSecurityService _security;

        private AccountSecurityService Security =>
            _security ?? (_security = new AccountSecurityService());

        public AuthService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        private HttpSessionState Session => HttpContext.Current.Session;
        private HttpRequest Request => HttpContext.Current.Request;

        public bool IsLoggedIn => CurrentUserId.HasValue;
        public int? CurrentUserId => SessionHelper.GetInt32(Session, "UserID");
        public string CurrentUsername => SessionHelper.GetString(Session, "Username");
        public string CurrentFullName => SessionHelper.GetString(Session, "FullName");
        public bool IsAdmin => SessionHelper.GetInt32(Session, "IsAdmin") == 1;
        public int? LinkedEmployeeId => SessionHelper.GetInt32(Session, "LinkedEmployeeID");
        public bool MustChangePassword => SessionHelper.GetInt32(Session, "MustChangePassword") == 1;
        public string SecurityStamp => SessionHelper.GetString(Session, "SecurityStamp");

        public LoginResult Login(string username, string password)
        {
            username = (username ?? "").Trim();
            password = password ?? "";
            var ip = Request?.UserHostAddress ?? "";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return new LoginResult { Success = false, Message = "Username and password are required." };

            try
            {
                EnsureUserTableAndAdmin();
                Security.EnsureSchema();

                if (Security.IsThrottled(username, ip))
                    return new LoginResult { Success = false, Message = GenericLoginError };

                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
                    SELECT UserID, Username, PasswordHash, FullName, IsActive, IsAdmin,
                           ISNULL(TwoFactorEnabled,0) AS TwoFactorEnabled,
                           ISNULL(MustChangePassword,0) AS MustChangePassword,
                           ISNULL(SecurityStamp,'') AS SecurityStamp,
                           LockoutUntil
                    FROM tblUser WHERE Username = @Username;", conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    conn.Open();

                    using (var dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                        {
                            Security.RecordFailedAttempt(username, ip);
                            return new LoginResult { Success = false, Message = GenericLoginError };
                        }

                        if (!dr.GetBoolean(dr.GetOrdinal("IsActive")))
                        {
                            Security.RecordFailedAttempt(username, ip);
                            return new LoginResult { Success = false, Message = GenericLoginError };
                        }

                        var lockoutOrd = dr.GetOrdinal("LockoutUntil");
                        if (!dr.IsDBNull(lockoutOrd) && dr.GetDateTime(lockoutOrd) > DateTime.Now)
                            return new LoginResult { Success = false, Message = GenericLoginError };

                        var hash = dr.GetString(dr.GetOrdinal("PasswordHash"));
                        if (!PasswordHelper.VerifyPassword(password, hash))
                        {
                            Security.RecordFailedAttempt(username, ip);
                            return new LoginResult { Success = false, Message = GenericLoginError };
                        }

                        var userId = dr.GetInt32(dr.GetOrdinal("UserID"));
                        var isAdmin = dr.GetBoolean(dr.GetOrdinal("IsAdmin"));
                        var fullName = dr.IsDBNull(dr.GetOrdinal("FullName")) ? username : dr.GetString(dr.GetOrdinal("FullName"));
                        var twoFactor = dr.GetBoolean(dr.GetOrdinal("TwoFactorEnabled"));
                        var mustChange = dr.GetBoolean(dr.GetOrdinal("MustChangePassword"));
                        var stamp = dr.IsDBNull(dr.GetOrdinal("SecurityStamp")) ? "" : dr.GetString(dr.GetOrdinal("SecurityStamp"));
                        dr.Close();

                        if (twoFactor)
                        {
                            ClearPendingTwoFactor();
                            SessionHelper.SetInt32(Session, "Pending2FAUserID", userId);
                            Session["Pending2FAUntil"] = DateTime.UtcNow.AddMinutes(PendingTwoFactorMinutes);
                            SessionHelper.SetString(Session, "Pending2FAUsername", username);
                            return new LoginResult { Success = false, RequiresTwoFactor = true, Message = "Enter your authenticator code." };
                        }

                        EstablishSession(conn, userId, username, fullName, isAdmin, mustChange, stamp);
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

        public LoginResult CompleteTwoFactor(string code)
        {
            var pendingId = SessionHelper.GetInt32(Session, "Pending2FAUserID");
            var until = Session["Pending2FAUntil"] as DateTime?;
            if (!pendingId.HasValue || !until.HasValue || until.Value < DateTime.UtcNow)
            {
                ClearPendingTwoFactor();
                return new LoginResult { Success = false, Message = "Your sign-in expired. Sign in again." };
            }

            var ip = Request?.UserHostAddress ?? "";
            var username = SessionHelper.GetString(Session, "Pending2FAUsername") ?? "";
            if (Security.IsThrottled(username, ip))
                return new LoginResult { Success = false, Message = GenericLoginError };

            var secret = Security.GetTwoFactorSecret(pendingId.Value);
            var totpOk = TotpHelper.Verify(secret, code);
            var recoveryOk = !totpOk && Security.ConsumeRecoveryCode(pendingId.Value, code);
            if (!totpOk && !recoveryOk)
            {
                Security.RecordFailedAttempt(username, ip);
                return new LoginResult { Success = false, RequiresTwoFactor = true, Message = "That code is not valid." };
            }

            try
            {
                using (var conn = new SqlConnection(_conn))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(@"
SELECT Username, FullName, IsAdmin, ISNULL(MustChangePassword,0) AS MustChangePassword,
       ISNULL(SecurityStamp,'') AS SecurityStamp, IsActive
FROM tblUser WHERE UserID=@ID;", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", pendingId.Value);
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (!dr.Read() || !dr.GetBoolean(dr.GetOrdinal("IsActive")))
                            {
                                ClearPendingTwoFactor();
                                return new LoginResult { Success = false, Message = GenericLoginError };
                            }
                            var un = dr.GetString(dr.GetOrdinal("Username"));
                            var fullName = dr.IsDBNull(dr.GetOrdinal("FullName")) ? un : dr.GetString(dr.GetOrdinal("FullName"));
                            var isAdmin = dr.GetBoolean(dr.GetOrdinal("IsAdmin"));
                            var mustChange = dr.GetBoolean(dr.GetOrdinal("MustChangePassword"));
                            var stamp = dr.GetString(dr.GetOrdinal("SecurityStamp"));
                            dr.Close();
                            EstablishSession(conn, pendingId.Value, un, fullName, isAdmin, mustChange, stamp);
                        }
                    }
                }
                ClearPendingTwoFactor();
                return new LoginResult { Success = true };
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

                using (var exists = new SqlCommand("SELECT COUNT(*) FROM tblUser WHERE Username = N'admin';", conn))
                {
                    var count = Convert.ToInt32(exists.ExecuteScalar());
                    if (count > 0) return;
                }

                var hash = PasswordHelper.HashPassword("Admin@123");
                using (var insert = new SqlCommand(@"
                    INSERT INTO tblUser (UserCode, Username, PasswordHash, FullName, Email, IsActive, IsAdmin, CreatedOn)
                    VALUES (N'GB-US-00001', N'admin', @Hash, N'System Administrator', N'admin@hrms.local', 1, 1, GETDATE());", conn))
                {
                    insert.Parameters.AddWithValue("@Hash", hash);
                    insert.ExecuteNonQuery();
                }
            }
        }

        public void Logout()
        {
            ClearPendingTwoFactor();
            Session.Clear();
        }

        private void EstablishSession(SqlConnection conn, int userId, string username, string fullName, bool isAdmin, bool mustChange, string stamp)
        {
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

            if (string.IsNullOrEmpty(stamp))
            {
                stamp = AccountSecurityService.NewStamp();
                try
                {
                    using (var stampCmd = new SqlCommand("UPDATE tblUser SET SecurityStamp=@Stamp WHERE UserID=@ID;", conn))
                    {
                        stampCmd.Parameters.AddWithValue("@Stamp", stamp);
                        stampCmd.Parameters.AddWithValue("@ID", userId);
                        stampCmd.ExecuteNonQuery();
                    }
                }
                catch { }
            }

            Session.Clear();
            SessionHelper.SetInt32(Session, "UserID", userId);
            SessionHelper.SetString(Session, "Username", username);
            SessionHelper.SetString(Session, "FullName", fullName);
            SessionHelper.SetInt32(Session, "IsAdmin", isAdmin ? 1 : 0);
            SessionHelper.SetInt32(Session, "MustChangePassword", mustChange ? 1 : 0);
            SessionHelper.SetString(Session, "SecurityStamp", stamp);
            SessionHelper.SetString(Session, "AuthUserAgent", Request?.UserAgent ?? "");
            if (linkedEmployeeId.HasValue)
                SessionHelper.SetInt32(Session, "LinkedEmployeeID", linkedEmployeeId.Value);

            Security.RecordSuccessfulLogin(userId);
        }

        private void ClearPendingTwoFactor()
        {
            try
            {
                Session.Remove("Pending2FAUserID");
                Session.Remove("Pending2FAUntil");
                Session.Remove("Pending2FAUsername");
            }
            catch { }
        }
    }
}
