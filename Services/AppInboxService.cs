using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
namespace HRMS.Services
{
    public class InboxMessage
    {
        public int InboxID { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string LinkUrl { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? RelatedID { get; set; }
        public string RelatedType { get; set; } = "";
    }

    public class AppInboxService
    {
        public const string TypeProfileChange = "ProfileChange";
        public const string TypeLeave = "Leave";
        public const string TypeEmployeeRequest = "EmployeeRequest";
        public const string TypeDiscipline = "Discipline";
        public const string TypeExpense = "Expense";
        public const string TypeContactChange = "ContactChange";

        private readonly string _conn;
        private readonly AuthService _auth = new AuthService();

        public AppInboxService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public void EnsureSchema()
        {
            if (string.IsNullOrWhiteSpace(_conn)) return;
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
IF OBJECT_ID('tblAppInbox','U') IS NULL CREATE TABLE tblAppInbox (
    InboxID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(2000) NULL,
    LinkUrl NVARCHAR(300) NULL,
    RelatedType NVARCHAR(40) NULL,
    RelatedID INT NULL,
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
    ReadOn DATETIME NULL
);", conn))
                    cmd.ExecuteNonQuery();
            }
        }

        public string ActorName()
        {
            var name = (_auth.CurrentFullName ?? "").Trim();
            if (!string.IsNullOrEmpty(name)) return name;
            name = (_auth.CurrentUsername ?? "").Trim();
            return string.IsNullOrEmpty(name) ? "User" : name;
        }

        public string FormatBody(string module, string requestRef, string employeeName,
            string action, string status, string actorName, DateTime? when = null)
        {
            var at = (when ?? DateTime.Now).ToString("dd-MMM-yyyy HH:mm");
            return "Module: " + (module ?? "")
                 + "\nRequest: " + (requestRef ?? "")
                 + "\nEmployee: " + (employeeName ?? "")
                 + "\nAction: " + (action ?? "")
                 + "\nStatus: " + (status ?? "")
                 + "\nBy: " + (actorName ?? ActorName())
                 + "\nWhen: " + at;
        }

        public void Notify(int userId, string title, string message, string link, string relatedType, int relatedId)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(_conn)) return;
            EnsureSchema();
            var text = message ?? "";
            if (text.Length > 2000) text = text.Substring(0, 2000);
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
INSERT INTO tblAppInbox (UserID, Title, Message, LinkUrl, RelatedType, RelatedID, IsRead, CreatedOn)
VALUES (@UserID, @Title, @Message, @Link, @Type, @RelatedID, 0, GETDATE());", conn))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@Title", (title ?? "").Length > 200 ? title.Substring(0, 200) : (title ?? ""));
                cmd.Parameters.AddWithValue("@Message", text);
                cmd.Parameters.AddWithValue("@Link", (object)link ?? "");
                cmd.Parameters.AddWithValue("@Type", relatedType ?? "");
                cmd.Parameters.AddWithValue("@RelatedID", relatedId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void NotifyUsers(IEnumerable<int> userIds, string title, string message, string link, string relatedType, int relatedId)
        {
            if (userIds == null) return;
            var seen = new HashSet<int>();
            foreach (var id in userIds)
            {
                if (id <= 0 || !seen.Add(id)) continue;
                Notify(id, title, message, link, relatedType, relatedId);
            }
        }

        public string EmployeeLabel(int employeeId)
        {
            if (employeeId <= 0 || string.IsNullOrWhiteSpace(_conn)) return "";
            try
            {
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
SELECT LTRIM(RTRIM(ISNULL(EmployeeCode,'') + ' ' + ISNULL(FirstName,'') + ' ' + ISNULL(LastName,'')))
FROM tblEmployee WHERE EmployeeID=@ID;", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", employeeId);
                    conn.Open();
                    return (cmd.ExecuteScalar() as string ?? "").Trim();
                }
            }
            catch { return ""; }
        }

        public List<int> UserIdsForEmployee(int employeeId)
        {
            var ids = new List<int>();
            if (employeeId <= 0 || string.IsNullOrWhiteSpace(_conn)) return ids;
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT UserID FROM tblEmployee WHERE EmployeeID=@Id AND ISNULL(UserID,0)>0;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", employeeId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        if (!dr.IsDBNull(0)) ids.Add(Convert.ToInt32(dr[0]));
            }
            return ids;
        }

        public List<int> UserIdsWithForm(string formKey)
        {
            var ids = new List<int>();
            if (string.IsNullOrWhiteSpace(formKey) || string.IsNullOrWhiteSpace(_conn)) return ids;
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT DISTINCT u.UserID
FROM tblUser u
LEFT JOIN tblUserPermission p ON p.UserID = u.UserID AND p.FormKey = @FormKey
WHERE u.IsActive = 1 AND (u.IsAdmin = 1 OR ISNULL(p.CanRead,0)=1 OR ISNULL(p.CanWrite,0)=1 OR ISNULL(p.CanApprove,0)=1);", conn))
            {
                cmd.Parameters.AddWithValue("@FormKey", formKey);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        ids.Add(Convert.ToInt32(dr[0]));
            }
            return ids;
        }

        public int UnreadCount(int userId)
        {
            if (userId <= 0) return 0;
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM tblAppInbox WHERE UserID=@UserID AND IsRead=0;", conn))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
            }
        }

        public List<InboxMessage> List(int userId, int take = 50)
        {
            EnsureSchema();
            var list = new List<InboxMessage>();
            if (userId <= 0) return list;
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT TOP (@Take) InboxID, Title, Message, LinkUrl, IsRead, CreatedOn, RelatedID, RelatedType
FROM tblAppInbox WHERE UserID=@UserID
ORDER BY CreatedOn DESC;", conn))
            {
                cmd.Parameters.Add("@Take", SqlDbType.Int).Value = take;
                cmd.Parameters.AddWithValue("@UserID", userId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new InboxMessage
                        {
                            InboxID = Convert.ToInt32(dr["InboxID"]),
                            Title = dr["Title"].ToString() ?? "",
                            Message = dr["Message"] == DBNull.Value ? "" : dr["Message"].ToString(),
                            LinkUrl = dr["LinkUrl"] == DBNull.Value ? "" : dr["LinkUrl"].ToString(),
                            IsRead = Convert.ToBoolean(dr["IsRead"]),
                            CreatedOn = Convert.ToDateTime(dr["CreatedOn"]),
                            RelatedID = dr["RelatedID"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["RelatedID"]),
                            RelatedType = dr["RelatedType"] == DBNull.Value ? "" : dr["RelatedType"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public void MarkRead(int inboxId, int userId)
        {
            if (inboxId <= 0 || userId <= 0) return;
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("UPDATE tblAppInbox SET IsRead=1, ReadOn=GETDATE() WHERE InboxID=@ID AND UserID=@UserID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", inboxId);
                cmd.Parameters.AddWithValue("@UserID", userId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void MarkRelatedRead(string relatedType, int relatedId, int userId)
        {
            if (userId <= 0 || relatedId <= 0) return;
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
UPDATE tblAppInbox SET IsRead=1, ReadOn=GETDATE()
WHERE RelatedType=@Type AND RelatedID=@ID AND UserID=@UserID AND IsRead=0;", conn))
            {
                cmd.Parameters.AddWithValue("@Type", relatedType ?? "");
                cmd.Parameters.AddWithValue("@ID", relatedId);
                cmd.Parameters.AddWithValue("@UserID", userId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
