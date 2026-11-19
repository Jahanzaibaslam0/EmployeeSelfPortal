using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace HRMS.Services
{
    public class AccountProfile
    {
        public int UserID { get; set; }
        public string UserCode { get; set; } = "";
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? TwoFactorConfirmedOn { get; set; }
        public bool MustChangePassword { get; set; }
        public int UnusedRecoveryCodes { get; set; }
        public int SecurityQuestionCount { get; set; }
        public DateTime? PasswordChangedOn { get; set; }
        public DateTime? LockoutUntil { get; set; }
        public string SecurityStamp { get; set; } = "";
    }

    public class SecurityQuestionOption
    {
        public string Key { get; set; } = "";
        public string Text { get; set; } = "";
    }

    public class SecurityQuestionRow
    {
        public string QuestionKey { get; set; } = "";
        public string QuestionText { get; set; } = "";
    }

    /// <summary>
    /// Local account security: profile, passwords, TOTP, recovery codes, questions, lockout.
    /// No cloud or email dependency.
    /// </summary>
    public class AccountSecurityService
    {
        public const string FormKey = "AccountSettings";
        public const int MaxFailedAttempts = 5;
        public const int LockoutMinutes = 15;
        public const int RecoveryCodeCount = 10;
        public const int RequiredQuestions = 2;

        public static readonly SecurityQuestionOption[] QuestionCatalog =
        {
            new SecurityQuestionOption { Key = "school", Text = "What is the name of your first school?" },
            new SecurityQuestionOption { Key = "city", Text = "In which city were you born?" },
            new SecurityQuestionOption { Key = "pet", Text = "What was the name of your first pet?" },
            new SecurityQuestionOption { Key = "teacher", Text = "What is the name of your favorite teacher?" },
            new SecurityQuestionOption { Key = "street", Text = "What street did you grow up on?" }
        };

        private readonly string _conn;
        private AuditService _audit;

        private AuditService Audit => _audit ?? (_audit = new AuditService());

        public AccountSecurityService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public void EnsureSchema()
        {
            if (string.IsNullOrWhiteSpace(_conn)) return;
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                foreach (var sql in SchemaStatements)
                {
                    try
                    {
                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.CommandTimeout = 30;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch
                    {
                        // Column/table may already exist on production with limited ALTER rights.
                    }
                }
            }
        }

        public AccountProfile GetProfile(int userId)
        {
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT u.UserID, u.UserCode, u.Username, u.FullName, u.Email, u.IsAdmin, u.IsActive,
       ISNULL(u.TwoFactorEnabled, 0) AS TwoFactorEnabled, u.TwoFactorConfirmedOn,
       ISNULL(u.MustChangePassword, 0) AS MustChangePassword, u.PasswordChangedOn,
       u.LockoutUntil, ISNULL(u.SecurityStamp, '') AS SecurityStamp,
       (SELECT COUNT(*) FROM tblUserRecoveryCode rc WHERE rc.UserID = u.UserID AND rc.UsedOn IS NULL) AS UnusedCodes,
       (SELECT COUNT(*) FROM tblUserSecurityQuestion q WHERE q.UserID = u.UserID) AS QCount
FROM tblUser u WHERE u.UserID = @ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    return new AccountProfile
                    {
                        UserID = userId,
                        UserCode = dr["UserCode"] == DBNull.Value ? "" : dr["UserCode"].ToString(),
                        Username = dr["Username"].ToString() ?? "",
                        FullName = dr["FullName"] == DBNull.Value ? "" : dr["FullName"].ToString(),
                        Email = dr["Email"] == DBNull.Value ? "" : dr["Email"].ToString(),
                        IsAdmin = Convert.ToBoolean(dr["IsAdmin"]),
                        IsActive = Convert.ToBoolean(dr["IsActive"]),
                        TwoFactorEnabled = Convert.ToBoolean(dr["TwoFactorEnabled"]),
                        TwoFactorConfirmedOn = dr["TwoFactorConfirmedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["TwoFactorConfirmedOn"]),
                        MustChangePassword = Convert.ToBoolean(dr["MustChangePassword"]),
                        PasswordChangedOn = dr["PasswordChangedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["PasswordChangedOn"]),
                        LockoutUntil = dr["LockoutUntil"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["LockoutUntil"]),
                        SecurityStamp = dr["SecurityStamp"].ToString() ?? "",
                        UnusedRecoveryCodes = Convert.ToInt32(dr["UnusedCodes"]),
                        SecurityQuestionCount = Convert.ToInt32(dr["QCount"])
                    };
                }
            }
        }

        public string UpdateProfile(int userId, string fullName, string email)
        {
            EnsureSchema();
            fullName = (fullName ?? "").Trim();
            email = (email ?? "").Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                return "Full name is required.";
            if (!string.IsNullOrWhiteSpace(email) && !InputValidators.IsValidEmail(email))
                return InputValidators.InvalidEmailMessage;

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
UPDATE tblUser SET FullName=@FullName, Email=@Email, ModifiedOn=GETDATE()
WHERE UserID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(email) ? (object)DBNull.Value : email);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            var session = HttpContext.Current?.Session;
            if (session != null) SessionHelper.SetString(session, "FullName", fullName);

            Audit.Log("AccountProfileUpdate", "User", userId, null, FormKey, "/AccountSettings", "UpdateProfile", "Display name / email updated");
            return null;
        }

        public string ChangePassword(int userId, string currentPassword, string newPassword, string confirm)
        {
            EnsureSchema();
            if (!PasswordHelper.ConstantTimeEquals(newPassword ?? "", confirm ?? ""))
                return "New password and confirmation do not match.";

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                string hash;
                string username;
                using (var cmd = new SqlCommand("SELECT Username, PasswordHash FROM tblUser WHERE UserID=@ID;", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", userId);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) return "Account not found.";
                        username = dr["Username"].ToString() ?? "";
                        hash = dr["PasswordHash"].ToString() ?? "";
                    }
                }
                if (!PasswordHelper.VerifyPassword(currentPassword ?? "", hash))
                {
                    Audit.Log("PasswordChangeFailed", "User", userId, username, FormKey, "/AccountSettings", "ChangePassword", "Current password incorrect", success: false);
                    return "Current password is incorrect.";
                }
                if (!PasswordHelper.TryValidatePolicy(newPassword, username, out var error))
                    return error;
                if (PasswordHelper.VerifyPassword(newPassword, hash))
                    return "New password must be different from the current password.";

                SetPasswordInternal(conn, userId, newPassword, mustChange: false);
            }
            Audit.Log("PasswordChanged", "User", userId, null, FormKey, "/AccountSettings", "ChangePassword", "User changed own password");
            return null;
        }

        public void AdminSetPassword(int userId, string newPassword, bool mustChange, int actorUserId)
        {
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                SetPasswordInternal(conn, userId, newPassword, mustChange);
                ClearLockout(conn, userId);
            }
            Audit.Log("AdminPasswordReset", "User", userId, null, "UserSetup", "/UserSetup", "AdminSetPassword", "Administrator reset password", userId: actorUserId);
        }

        public string ResetPasswordAfterRecovery(int userId, string newPassword, string confirm)
        {
            EnsureSchema();
            if (!PasswordHelper.ConstantTimeEquals(newPassword ?? "", confirm ?? ""))
                return "New password and confirmation do not match.";
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                string username;
                using (var cmd = new SqlCommand("SELECT Username FROM tblUser WHERE UserID=@ID;", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", userId);
                    username = cmd.ExecuteScalar() as string ?? "";
                }
                if (!PasswordHelper.TryValidatePolicy(newPassword, username, out var error))
                    return error;
                SetPasswordInternal(conn, userId, newPassword, mustChange: false);
                ClearLockout(conn, userId);
            }
            Audit.Log("PasswordRecovered", "User", userId, null, "ForgotPassword", "/ForgotPassword", "ResetPassword", "Password reset via recovery");
            return null;
        }

        public string BeginTwoFactorSetup(int userId, string currentPassword, out string secret, out string otpauthUri)
        {
            secret = "";
            otpauthUri = "";
            var profile = GetProfile(userId);
            if (profile == null) return "Account not found.";
            if (!VerifyCurrentPassword(userId, currentPassword))
                return "Current password is required to change security settings.";

            secret = TotpHelper.GenerateSecret();
            otpauthUri = TotpHelper.BuildOtpAuthUri("HRMS", profile.Username, secret);
            var session = HttpContext.Current.Session;
            session["PendingTotpSecret"] = secret;
            session["PendingTotpUserID"] = userId;
            return null;
        }

        public string ConfirmTwoFactorSetup(int userId, string code, out List<string> recoveryCodes)
        {
            recoveryCodes = new List<string>();
            var session = HttpContext.Current.Session;
            var pendingUser = SessionHelper.GetInt32(session, "PendingTotpUserID");
            var secret = SessionHelper.GetString(session, "PendingTotpSecret");
            if (pendingUser != userId || string.IsNullOrEmpty(secret))
                return "Two-factor setup expired. Start again.";
            if (!TotpHelper.Verify(secret, code))
                return "That verification code is not valid. Check the authenticator app and try again.";

            recoveryCodes = GeneratePlainRecoveryCodes();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
UPDATE tblUser SET TwoFactorEnabled=1, TwoFactorSecret=@Secret, TwoFactorConfirmedOn=GETDATE(),
  SecurityStamp=@Stamp, ModifiedOn=GETDATE() WHERE UserID=@ID;", conn))
                {
                    cmd.Parameters.AddWithValue("@Secret", secret);
                    cmd.Parameters.AddWithValue("@Stamp", NewStamp());
                    cmd.Parameters.AddWithValue("@ID", userId);
                    cmd.ExecuteNonQuery();
                }
                ReplaceRecoveryCodes(conn, userId, recoveryCodes);
            }
            session.Remove("PendingTotpSecret");
            session.Remove("PendingTotpUserID");
            RefreshSessionStamp(userId);
            Audit.Log("TwoFactorEnabled", "User", userId, null, FormKey, "/AccountSettings", "ConfirmTwoFactor", "TOTP enabled");
            return null;
        }

        public string DisableTwoFactor(int userId, string currentPassword, string codeOrRecovery)
        {
            var profile = GetProfile(userId);
            if (profile == null) return "Account not found.";
            if (!profile.TwoFactorEnabled) return "Two-factor authentication is not enabled.";
            if (!VerifyCurrentPassword(userId, currentPassword))
                return "Current password is required to change security settings.";

            var secret = GetTwoFactorSecret(userId);
            var totpOk = TotpHelper.Verify(secret, codeOrRecovery);
            var recoveryOk = !totpOk && ConsumeRecoveryCode(userId, codeOrRecovery);
            if (!totpOk && !recoveryOk)
                return "Enter a valid authenticator code or unused backup code.";

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
UPDATE tblUser SET TwoFactorEnabled=0, TwoFactorSecret=NULL, TwoFactorConfirmedOn=NULL,
  SecurityStamp=@Stamp, ModifiedOn=GETDATE() WHERE UserID=@ID;", conn))
                {
                    cmd.Parameters.AddWithValue("@Stamp", NewStamp());
                    cmd.Parameters.AddWithValue("@ID", userId);
                    cmd.ExecuteNonQuery();
                }
                using (var del = new SqlCommand("DELETE FROM tblUserRecoveryCode WHERE UserID=@ID;", conn))
                {
                    del.Parameters.AddWithValue("@ID", userId);
                    del.ExecuteNonQuery();
                }
            }
            RefreshSessionStamp(userId);
            Audit.Log("TwoFactorDisabled", "User", userId, null, FormKey, "/AccountSettings", "DisableTwoFactor", "TOTP disabled");
            return null;
        }

        public void AdminDisableTwoFactor(int userId, int actorUserId)
        {
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
UPDATE tblUser SET TwoFactorEnabled=0, TwoFactorSecret=NULL, TwoFactorConfirmedOn=NULL,
  SecurityStamp=@Stamp, ModifiedOn=GETDATE() WHERE UserID=@ID;", conn))
                {
                    cmd.Parameters.AddWithValue("@Stamp", NewStamp());
                    cmd.Parameters.AddWithValue("@ID", userId);
                    cmd.ExecuteNonQuery();
                }
                using (var del = new SqlCommand("DELETE FROM tblUserRecoveryCode WHERE UserID=@ID;", conn))
                {
                    del.Parameters.AddWithValue("@ID", userId);
                    del.ExecuteNonQuery();
                }
            }
            Audit.Log("AdminTwoFactorDisabled", "User", userId, null, "UserSetup", "/UserSetup", "AdminDisable2FA", "Administrator disabled 2FA", userId: actorUserId);
        }

        public string RegenerateRecoveryCodes(int userId, string currentPassword, out List<string> codes)
        {
            codes = new List<string>();
            if (!VerifyCurrentPassword(userId, currentPassword))
                return "Current password is required to change security settings.";
            var profile = GetProfile(userId);
            if (profile == null || !profile.TwoFactorEnabled)
                return "Enable two-factor authentication first.";
            codes = GeneratePlainRecoveryCodes();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                ReplaceRecoveryCodes(conn, userId, codes);
            }
            Audit.Log("RecoveryCodesRegenerated", "User", userId, null, FormKey, "/AccountSettings", "RegenerateCodes", "Backup codes replaced");
            return null;
        }

        public string SaveSecurityQuestions(int userId, string currentPassword, IList<Tuple<string, string>> answers)
        {
            if (!VerifyCurrentPassword(userId, currentPassword))
                return "Current password is required to change security settings.";
            if (answers == null || answers.Count < RequiredQuestions)
                return "Set at least " + RequiredQuestions + " security questions.";

            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var a in answers)
            {
                if (string.IsNullOrWhiteSpace(a.Item1) || string.IsNullOrWhiteSpace(a.Item2))
                    return "Each selected question needs an answer.";
                if (!QuestionCatalog.Any(q => q.Key.Equals(a.Item1, StringComparison.OrdinalIgnoreCase)))
                    return "Invalid security question.";
                if (!keys.Add(a.Item1))
                    return "Choose different questions.";
                if (a.Item2.Trim().Length < 2)
                    return "Answers must be at least 2 characters.";
            }

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var del = new SqlCommand("DELETE FROM tblUserSecurityQuestion WHERE UserID=@ID;", conn))
                {
                    del.Parameters.AddWithValue("@ID", userId);
                    del.ExecuteNonQuery();
                }
                foreach (var a in answers)
                {
                    using (var ins = new SqlCommand(@"
INSERT INTO tblUserSecurityQuestion (UserID, QuestionKey, AnswerHash, CreatedOn)
VALUES (@UserID, @Key, @Hash, GETDATE());", conn))
                    {
                        ins.Parameters.AddWithValue("@UserID", userId);
                        ins.Parameters.AddWithValue("@Key", a.Item1.Trim().ToLowerInvariant());
                        ins.Parameters.AddWithValue("@Hash", PasswordHelper.HashPassword(NormalizeAnswer(a.Item2)));
                        ins.ExecuteNonQuery();
                    }
                }
            }
            Audit.Log("SecurityQuestionsSaved", "User", userId, null, FormKey, "/AccountSettings", "SaveQuestions", "Recovery questions updated");
            return null;
        }

        public List<SecurityQuestionRow> GetQuestionsForUser(int userId)
        {
            var list = new List<SecurityQuestionRow>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT QuestionKey FROM tblUserSecurityQuestion WHERE UserID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var key = dr["QuestionKey"].ToString() ?? "";
                        var opt = QuestionCatalog.FirstOrDefault(q => q.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
                        list.Add(new SecurityQuestionRow
                        {
                            QuestionKey = key,
                            QuestionText = opt != null ? opt.Text : key
                        });
                    }
                }
            }
            return list;
        }

        public List<SecurityQuestionRow> GetDummyQuestions()
        {
            return QuestionCatalog.Take(RequiredQuestions)
                .Select(q => new SecurityQuestionRow { QuestionKey = q.Key, QuestionText = q.Text })
                .ToList();
        }

        public bool VerifySecurityAnswers(int userId, IList<Tuple<string, string>> answers)
        {
            if (answers == null || answers.Count == 0) return false;
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT QuestionKey, AnswerHash FROM tblUserSecurityQuestion WHERE UserID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                conn.Open();
                var stored = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        stored[dr["QuestionKey"].ToString()] = dr["AnswerHash"].ToString();
                }
                if (stored.Count == 0) return false;
                var matched = 0;
                foreach (var a in answers)
                {
                    if (!stored.TryGetValue(a.Item1 ?? "", out var hash)) continue;
                    if (PasswordHelper.VerifyPassword(NormalizeAnswer(a.Item2), hash))
                        matched++;
                }
                return matched >= RequiredQuestions && matched == answers.Count;
            }
        }

        public bool ConsumeRecoveryCode(int userId, string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            var normalized = NormalizeRecoveryCode(code);
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT RecoveryCodeID, CodeHash FROM tblUserRecoveryCode
WHERE UserID=@ID AND UsedOn IS NULL;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var id = Convert.ToInt32(dr["RecoveryCodeID"]);
                        var hash = dr["CodeHash"].ToString();
                        if (!PasswordHelper.VerifyPassword(normalized, hash)) continue;
                        dr.Close();
                        using (var upd = new SqlCommand("UPDATE tblUserRecoveryCode SET UsedOn=GETDATE() WHERE RecoveryCodeID=@RID;", conn))
                        {
                            upd.Parameters.AddWithValue("@RID", id);
                            upd.ExecuteNonQuery();
                        }
                        Audit.Log("RecoveryCodeUsed", "User", userId, null, FormKey, "/AccountSettings", "ConsumeRecoveryCode", "Backup code consumed");
                        return true;
                    }
                }
            }
            return false;
        }

        public int? FindUserIdByUsername(string username)
        {
            username = (username ?? "").Trim();
            if (string.IsNullOrEmpty(username)) return null;
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT UserID FROM tblUser WHERE Username=@Username AND IsActive=1;", conn))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                conn.Open();
                var result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value) return null;
                return Convert.ToInt32(result);
            }
        }

        public bool IsTwoFactorEnabled(int userId)
        {
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT ISNULL(TwoFactorEnabled,0) FROM tblUser WHERE UserID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                conn.Open();
                return Convert.ToBoolean(cmd.ExecuteScalar() ?? false);
            }
        }

        public string GetTwoFactorSecret(int userId)
        {
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT TwoFactorSecret FROM tblUser WHERE UserID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                conn.Open();
                return cmd.ExecuteScalar() as string ?? "";
            }
        }

        public bool VerifyCurrentPassword(int userId, string password)
        {
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT PasswordHash FROM tblUser WHERE UserID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                conn.Open();
                var hash = cmd.ExecuteScalar() as string ?? "";
                return PasswordHelper.VerifyPassword(password ?? "", hash);
            }
        }

        public bool IsThrottled(string username, string ip)
        {
            EnsureSchema();
            var untilUser = GetThrottleUntil("U:" + NormalizeUserKey(username));
            var untilIp = GetThrottleUntil("IP:" + (ip ?? "unknown"));
            var now = DateTime.Now;
            if (untilUser.HasValue && untilUser.Value > now) return true;
            if (untilIp.HasValue && untilIp.Value > now) return true;
            return UserLockoutActive(username);
        }

        public void RecordFailedAttempt(string username, string ip)
        {
            EnsureSchema();
            BumpThrottle("U:" + NormalizeUserKey(username), MaxFailedAttempts);
            BumpThrottle("IP:" + (ip ?? "unknown"), MaxFailedAttempts * 4);
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
UPDATE tblUser
SET FailedLoginCount = ISNULL(FailedLoginCount,0) + 1,
    LockoutUntil = CASE WHEN ISNULL(FailedLoginCount,0) + 1 >= @Max
                        THEN DATEADD(MINUTE, @Mins, GETDATE()) ELSE LockoutUntil END
WHERE Username=@Username;", conn))
            {
                cmd.Parameters.AddWithValue("@Username", (username ?? "").Trim());
                cmd.Parameters.AddWithValue("@Max", MaxFailedAttempts);
                cmd.Parameters.AddWithValue("@Mins", LockoutMinutes);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void RecordSuccessfulLogin(int userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                ClearLockout(conn, userId);
                string username;
                using (var cmd = new SqlCommand("SELECT Username FROM tblUser WHERE UserID=@ID;", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", userId);
                    username = cmd.ExecuteScalar() as string ?? "";
                }
                ClearThrottle("U:" + NormalizeUserKey(username));
            }
        }

        public bool ValidateActiveSession(int userId, string sessionStamp, out bool mustChangePassword, out bool forceLogout)
        {
            mustChangePassword = false;
            forceLogout = false;
            try
            {
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
SELECT IsActive, ISNULL(MustChangePassword,0) AS MustChangePassword,
       ISNULL(SecurityStamp,'') AS SecurityStamp
FROM tblUser WHERE UserID=@ID;", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", userId);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                        {
                            forceLogout = true;
                            return false;
                        }
                        if (!Convert.ToBoolean(dr["IsActive"]))
                        {
                            forceLogout = true;
                            return false;
                        }
                        mustChangePassword = Convert.ToBoolean(dr["MustChangePassword"]);
                        var stamp = dr["SecurityStamp"].ToString() ?? "";
                        if (!string.IsNullOrEmpty(stamp) && !string.IsNullOrEmpty(sessionStamp)
                            && !PasswordHelper.ConstantTimeEquals(stamp, sessionStamp))
                        {
                            forceLogout = true;
                            return false;
                        }
                        return true;
                    }
                }
            }
            catch
            {
                return true;
            }
        }

        public static string NewStamp() => Guid.NewGuid().ToString("N");

        public static string NormalizeAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer)) return "";
            var parts = answer.Trim().ToUpperInvariant().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        public static string NormalizeRecoveryCode(string code)
        {
            return (code ?? "").Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "");
        }

        private void SetPasswordInternal(SqlConnection conn, int userId, string newPassword, bool mustChange)
        {
            using (var cmd = new SqlCommand(@"
UPDATE tblUser SET PasswordHash=@Hash, MustChangePassword=@Must, PasswordChangedOn=GETDATE(),
  SecurityStamp=@Stamp, FailedLoginCount=0, LockoutUntil=NULL, ModifiedOn=GETDATE()
WHERE UserID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@Hash", PasswordHelper.HashPassword(newPassword));
                cmd.Parameters.AddWithValue("@Must", mustChange);
                cmd.Parameters.AddWithValue("@Stamp", NewStamp());
                cmd.Parameters.AddWithValue("@ID", userId);
                cmd.ExecuteNonQuery();
            }
            RefreshSessionStamp(userId);
        }

        private static void ClearLockout(SqlConnection conn, int userId)
        {
            using (var cmd = new SqlCommand("UPDATE tblUser SET FailedLoginCount=0, LockoutUntil=NULL WHERE UserID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                cmd.ExecuteNonQuery();
            }
        }

        private void ReplaceRecoveryCodes(SqlConnection conn, int userId, List<string> plainCodes)
        {
            using (var del = new SqlCommand("DELETE FROM tblUserRecoveryCode WHERE UserID=@ID;", conn))
            {
                del.Parameters.AddWithValue("@ID", userId);
                del.ExecuteNonQuery();
            }
            foreach (var code in plainCodes)
            {
                using (var ins = new SqlCommand(@"
INSERT INTO tblUserRecoveryCode (UserID, CodeHash, CreatedOn) VALUES (@UserID, @Hash, GETDATE());", conn))
                {
                    ins.Parameters.AddWithValue("@UserID", userId);
                    ins.Parameters.AddWithValue("@Hash", PasswordHelper.HashPassword(NormalizeRecoveryCode(code)));
                    ins.ExecuteNonQuery();
                }
            }
        }

        private static List<string> GeneratePlainRecoveryCodes()
        {
            var list = new List<string>();
            var bytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
            {
                while (list.Count < RecoveryCodeCount)
                {
                    rng.GetBytes(bytes);
                    var n = BitConverter.ToUInt32(bytes, 0) % 100000000;
                    var raw = n.ToString("D8");
                    list.Add(raw.Substring(0, 4) + "-" + raw.Substring(4, 4));
                }
            }
            return list;
        }

        private DateTime? GetThrottleUntil(string key)
        {
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT LockoutUntil FROM tblAuthThrottle WHERE ThrottleKey=@Key;", conn))
            {
                cmd.Parameters.AddWithValue("@Key", key);
                conn.Open();
                var v = cmd.ExecuteScalar();
                if (v == null || v == DBNull.Value) return null;
                return Convert.ToDateTime(v);
            }
        }

        private void BumpThrottle(string key, int maxFails)
        {
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM tblAuthThrottle WHERE ThrottleKey=@Key)
    INSERT INTO tblAuthThrottle (ThrottleKey, FailedCount, WindowStart, LockoutUntil)
    VALUES (@Key, 1, GETDATE(), NULL);
ELSE
BEGIN
    UPDATE tblAuthThrottle
    SET FailedCount = CASE WHEN WindowStart < DATEADD(MINUTE, -@Mins, GETDATE()) THEN 1 ELSE FailedCount + 1 END,
        WindowStart = CASE WHEN WindowStart < DATEADD(MINUTE, -@Mins, GETDATE()) THEN GETDATE() ELSE WindowStart END
    WHERE ThrottleKey=@Key;
    UPDATE tblAuthThrottle
    SET LockoutUntil = DATEADD(MINUTE, @Mins, GETDATE())
    WHERE ThrottleKey=@Key AND FailedCount >= @Max;
END", conn))
            {
                cmd.Parameters.AddWithValue("@Key", key);
                cmd.Parameters.AddWithValue("@Mins", LockoutMinutes);
                cmd.Parameters.AddWithValue("@Max", maxFails);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private void ClearThrottle(string key)
        {
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("DELETE FROM tblAuthThrottle WHERE ThrottleKey=@Key;", conn))
            {
                cmd.Parameters.AddWithValue("@Key", key);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private bool UserLockoutActive(string username)
        {
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT LockoutUntil FROM tblUser WHERE Username=@Username;", conn))
            {
                cmd.Parameters.AddWithValue("@Username", (username ?? "").Trim());
                conn.Open();
                var v = cmd.ExecuteScalar();
                if (v == null || v == DBNull.Value) return false;
                return Convert.ToDateTime(v) > DateTime.Now;
            }
        }

        private static string NormalizeUserKey(string username) => (username ?? "").Trim().ToLowerInvariant();

        private void RefreshSessionStamp(int userId)
        {
            var session = HttpContext.Current?.Session;
            if (session == null) return;
            var current = SessionHelper.GetInt32(session, "UserID");
            if (current != userId) return;
            var profile = GetProfile(userId);
            if (profile != null)
                SessionHelper.SetString(session, "SecurityStamp", profile.SecurityStamp);
        }

        public static readonly string[] SchemaStatements =
        {
            @"IF COL_LENGTH('tblUser','TwoFactorEnabled') IS NULL ALTER TABLE tblUser ADD TwoFactorEnabled BIT NOT NULL CONSTRAINT DF_tblUser_TwoFactor DEFAULT 0;",
            @"IF COL_LENGTH('tblUser','TwoFactorSecret') IS NULL ALTER TABLE tblUser ADD TwoFactorSecret NVARCHAR(64) NULL;",
            @"IF COL_LENGTH('tblUser','TwoFactorConfirmedOn') IS NULL ALTER TABLE tblUser ADD TwoFactorConfirmedOn DATETIME NULL;",
            @"IF COL_LENGTH('tblUser','MustChangePassword') IS NULL ALTER TABLE tblUser ADD MustChangePassword BIT NOT NULL CONSTRAINT DF_tblUser_MustChange DEFAULT 0;",
            @"IF COL_LENGTH('tblUser','FailedLoginCount') IS NULL ALTER TABLE tblUser ADD FailedLoginCount INT NOT NULL CONSTRAINT DF_tblUser_FailCount DEFAULT 0;",
            @"IF COL_LENGTH('tblUser','LockoutUntil') IS NULL ALTER TABLE tblUser ADD LockoutUntil DATETIME NULL;",
            @"IF COL_LENGTH('tblUser','PasswordChangedOn') IS NULL ALTER TABLE tblUser ADD PasswordChangedOn DATETIME NULL;",
            @"IF COL_LENGTH('tblUser','SecurityStamp') IS NULL ALTER TABLE tblUser ADD SecurityStamp NVARCHAR(36) NULL;",
            @"IF OBJECT_ID('tblUserRecoveryCode','U') IS NULL CREATE TABLE tblUserRecoveryCode (
    RecoveryCodeID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    CodeHash NVARCHAR(200) NOT NULL,
    UsedOn DATETIME NULL,
    CreatedOn DATETIME NOT NULL DEFAULT GETDATE());",
            @"IF OBJECT_ID('tblUserSecurityQuestion','U') IS NULL CREATE TABLE tblUserSecurityQuestion (
    UserQuestionID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    QuestionKey NVARCHAR(80) NOT NULL,
    AnswerHash NVARCHAR(200) NOT NULL,
    CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_UserQuestion UNIQUE (UserID, QuestionKey));",
            @"IF OBJECT_ID('tblAuthThrottle','U') IS NULL CREATE TABLE tblAuthThrottle (
    ThrottleID INT IDENTITY(1,1) PRIMARY KEY,
    ThrottleKey NVARCHAR(200) NOT NULL UNIQUE,
    FailedCount INT NOT NULL DEFAULT 0,
    WindowStart DATETIME NOT NULL DEFAULT GETDATE(),
    LockoutUntil DATETIME NULL);"
        };
    }
}
