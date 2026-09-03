using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;

namespace HRMS.Services
{
    public class AuditService
    {
        private readonly string _conn;
        private readonly AuthService _auth = new AuthService();

        public AuditService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public void Log(
            string actionType,
            string entityType = null,
            int? entityId = null,
            string entityName = null,
            string formKey = null,
            string pagePath = null,
            string handlerName = null,
            string details = null,
            int? userId = null,
            string username = null,
            bool success = true)
        {
            try
            {
                var ip = HttpContext.Current?.Request.UserHostAddress;

                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
                INSERT INTO tblAuditLog
                    (ActionAt, UserID, Username, FormKey, PagePath, HandlerName,
                     ActionType, EntityType, EntityID, EntityName, Details, IpAddress, Success)
                VALUES
                    (GETDATE(), @UserID, @Username, @FormKey, @PagePath, @HandlerName,
                     @ActionType, @EntityType, @EntityID, @EntityName, @Details, @IpAddress, @Success);", conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", (object)userId ?? (object)_auth.CurrentUserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Username", (object)username ?? (object)_auth.CurrentUsername ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FormKey", (object)formKey ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PagePath", (object)pagePath ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@HandlerName", (object)handlerName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ActionType", actionType);
                    cmd.Parameters.AddWithValue("@EntityType", (object)entityType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EntityID", (object)entityId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EntityName", (object)Truncate(entityName, 250) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Details", (object)Truncate(details, 3800) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IpAddress", (object)ip ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Success", success);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Audit failures must not break business operations
            }
        }

        public void LogLogin(string username, bool success, int? userId = null, string message = null)
        {
            Log(
                actionType: success ? "Login" : "LoginFailed",
                entityType: "User",
                entityId: userId,
                entityName: username,
                formKey: "Login",
                pagePath: "/Login",
                handlerName: "OnPost",
                details: message,
                userId: userId,
                username: username,
                success: success);
        }

        public void LogLogout()
        {
            Log(
                actionType: "Logout",
                entityType: "User",
                entityId: _auth.CurrentUserId,
                entityName: _auth.CurrentUsername,
                formKey: "Login",
                pagePath: "/Login",
                handlerName: "OnGetLogout",
                userId: _auth.CurrentUserId,
                username: _auth.CurrentUsername);
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return value.Length <= max ? value : value.Substring(0, max);
        }
    }
}
