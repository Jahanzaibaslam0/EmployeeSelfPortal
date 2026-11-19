using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Text;
using System.Web;

namespace HRMS.Services
{
    public class AuditEntry
    {
        public DateTime ActionAt { get; set; }
        public string Username { get; set; } = "";
        public string ActionType { get; set; } = "";
        public string EntityType { get; set; } = "";
        public string EntityName { get; set; } = "";
        public string Details { get; set; } = "";
        public string HandlerName { get; set; } = "";
    }

    public class AuditService
    {
        private readonly string _conn;

        public AuditService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        private static int? SessionUserId
        {
            get
            {
                var session = HttpContext.Current?.Session;
                return session == null ? null : SessionHelper.GetInt32(session, "UserID");
            }
        }

        private static string SessionUsername
        {
            get
            {
                var session = HttpContext.Current?.Session;
                return session == null ? "" : SessionHelper.GetString(session, "Username");
            }
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
                    cmd.Parameters.AddWithValue("@UserID", (object)userId ?? (object)SessionUserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Username", (object)username ?? (object)(string.IsNullOrEmpty(SessionUsername) ? null : SessionUsername) ?? DBNull.Value);
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
            var userId = SessionUserId;
            var username = SessionUsername;
            Log(
                actionType: "Logout",
                entityType: "User",
                entityId: userId,
                entityName: username,
                formKey: "Login",
                pagePath: "/Login",
                handlerName: "OnGetLogout",
                userId: userId,
                username: username);
        }

        public List<AuditEntry> ListContactHub(int contactId, IList<int> addressIds)
        {
            var list = new List<AuditEntry>();
            if (contactId <= 0 || string.IsNullOrWhiteSpace(_conn)) return list;
            var ids = new List<int>();
            if (addressIds != null)
            {
                foreach (var id in addressIds)
                    if (id > 0 && !ids.Contains(id)) ids.Add(id);
            }

            var sql = new StringBuilder(@"
SELECT TOP 200 ActionAt, ISNULL(Username,''), ISNULL(ActionType,''), ISNULL(EntityType,''),
       ISNULL(EntityName,''), ISNULL(Details,''), ISNULL(HandlerName,'')
FROM tblAuditLog
WHERE FormKey=@FormKey AND (
    (ISNULL(EntityType,N'Contact') IN (N'Contact', N'ContactChange') AND EntityID=@ContactID)
    OR (Details LIKE @Token)
");
            if (ids.Count > 0)
            {
                sql.Append("    OR (EntityType=N'ContactAddress' AND EntityID IN (");
                for (var i = 0; i < ids.Count; i++)
                {
                    if (i > 0) sql.Append(',');
                    sql.Append("@A").Append(i);
                }
                sql.Append("))");
            }
            sql.Append(@"
)
ORDER BY ActionAt DESC;");

            try
            {
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(sql.ToString(), conn))
                {
                    cmd.Parameters.AddWithValue("@FormKey", "ContactMaster");
                    cmd.Parameters.AddWithValue("@ContactID", contactId);
                    cmd.Parameters.AddWithValue("@Token", "%ContactID=" + contactId + ";%");
                    for (var i = 0; i < ids.Count; i++)
                        cmd.Parameters.AddWithValue("@A" + i, ids[i]);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new AuditEntry
                            {
                                ActionAt = dr.GetDateTime(0),
                                Username = dr.GetString(1),
                                ActionType = dr.GetString(2),
                                EntityType = dr.GetString(3),
                                EntityName = dr.GetString(4),
                                Details = dr.GetString(5),
                                HandlerName = dr.GetString(6)
                            });
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return value.Length <= max ? value : value.Substring(0, max);
        }
    }
}
