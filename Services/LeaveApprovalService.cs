using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace HRMS.Services
{
    public static class LeaveApprovalStatuses
    {
        public const string Draft = "Draft";
        public const string PendingDepartmentHead = "PendingDepartmentHead";
        public const string PendingHR = "PendingHR";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";

        public static string DisplayName(string status)
        {
            switch (status)
            {
                case Draft: return "Draft";
                case PendingDepartmentHead: return "Pending Department Head";
                case PendingHR: return "Pending HR Manager";
                case Approved: return "Approved";
                case Rejected: return "Rejected";
                default: return status ?? "";
            }
        }

        public static string BadgeClass(string status)
        {
            switch (status)
            {
                case PendingDepartmentHead: return "leave-badge leave-pending-dh";
                case PendingHR: return "leave-badge leave-pending-hr";
                case Approved: return "leave-badge leave-approved";
                case Rejected: return "leave-badge leave-rejected";
                default: return "leave-badge";
            }
        }
    }

    public static class LeaveApprovalStages
    {
        public const string DepartmentHead = "DepartmentHead";
        public const string HR = "HR";
        public const string Applicant = "Applicant";
    }

    public class LeaveApprovalActionItem
    {
        public int ActionID { get; set; }
        public string Stage { get; set; } = "";
        public string Action { get; set; } = "";
        public string ActorName { get; set; } = "";
        public string Comments { get; set; } = "";
        public DateTime ActionOn { get; set; }
        public string StageDisplay
        {
            get
            {
                if (Stage == LeaveApprovalStages.DepartmentHead) return "Department Head";
                if (Stage == LeaveApprovalStages.HR) return "HR Manager";
                if (Stage == LeaveApprovalStages.Applicant) return "Applicant";
                return Stage ?? "";
            }
        }
    }

    public class LeaveAlertItem
    {
        public int AlertID { get; set; }
        public int LeaveID { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string LinkUrl { get; set; } = "";
        public DateTime CreatedOn { get; set; }
    }

    public class LeaveApprovalService
    {
        public const string FormKey = "LeaveMaster";
        public const string HrFormKey = "LeaveHR";

        private readonly string _conn;
        private readonly AuthService _auth = new AuthService();
        private readonly PermissionService _perms = new PermissionService();
        private readonly EmployeeProfileAccessService _profile = new EmployeeProfileAccessService();
        private readonly AppInboxService _inbox = new AppInboxService();

        public LeaveApprovalService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString
                    ?? throw new InvalidOperationException("HRMSConnection is not configured.");
        }

        public void EnsureSchema()
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                ExecIgnore(conn, @"
IF COL_LENGTH(N'tblDepartment', N'DepartmentManagerID') IS NULL
    ALTER TABLE tblDepartment ADD DepartmentManagerID INT NULL;");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'tblLeaveApplication', N'U') IS NOT NULL AND COL_LENGTH(N'tblLeaveApplication', N'Status') IS NULL
    ALTER TABLE tblLeaveApplication ADD Status NVARCHAR(50) NULL;");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'tblLeaveApplication', N'U') IS NOT NULL AND COL_LENGTH(N'tblLeaveApplication', N'Status') IS NOT NULL
    UPDATE tblLeaveApplication SET Status = N'Approved' WHERE Status IS NULL OR LTRIM(RTRIM(Status)) = N'';");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'tblLeaveApplication', N'U') IS NOT NULL AND COL_LENGTH(N'tblLeaveApplication', N'DepartmentHeadEmployeeID') IS NULL
    ALTER TABLE tblLeaveApplication ADD DepartmentHeadEmployeeID INT NULL;");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'tblLeaveApprovalAction', N'U') IS NULL
CREATE TABLE tblLeaveApprovalAction (
    ActionID INT IDENTITY(1,1) PRIMARY KEY,
    LeaveID INT NOT NULL,
    Stage NVARCHAR(50) NOT NULL,
    Action NVARCHAR(50) NOT NULL,
    ActorUserID INT NULL,
    ActorEmployeeID INT NULL,
    Comments NVARCHAR(1000) NULL,
    ActionOn DATETIME NOT NULL CONSTRAINT DF_tblLeaveAct_On DEFAULT (GETDATE())
);");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'tblLeaveAlert', N'U') IS NULL
CREATE TABLE tblLeaveAlert (
    AlertID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    LeaveID INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(1000) NULL,
    LinkUrl NVARCHAR(300) NULL,
    IsRead BIT NOT NULL CONSTRAINT DF_tblLeaveAlert_Read DEFAULT (0),
    CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblLeaveAlert_Created DEFAULT (GETDATE())
);");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'tblAppForm', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM tblAppForm WHERE FormKey = N'LeaveHR')
    INSERT INTO tblAppForm (FormKey, FormName, PagePath, Category, SortOrder, IsActive, CreatedOn)
    VALUES (N'LeaveHR', N'Leave Management - HR Manager', N'/LeaveMaster', N'Transactions', 16, 1, GETDATE());");
            }
        }

        private static void ExecIgnore(SqlConnection conn, string sql)
        {
            try
            {
                using (var cmd = new SqlCommand(sql, conn))
                    cmd.ExecuteNonQuery();
            }
            catch
            {
            }
        }

        public bool CanAccessModule()
            => _auth.IsAdmin
               || _perms.CanRead(FormKey)
               || _perms.CanRead(HrFormKey)
               || _profile.IsEmployeeProfileSynchronized();

        public bool CanActAsHr()
            => _auth.IsAdmin || _perms.CanRead(HrFormKey) || _perms.CanWrite(HrFormKey);

        public int LinkedEmployeeId => _profile.GetLinkedEmployeeId() ?? 0;

        public bool CanActAsDepartmentHead(int departmentHeadEmployeeId)
            => _auth.IsAdmin
               || (departmentHeadEmployeeId > 0 && LinkedEmployeeId == departmentHeadEmployeeId);

        public bool CanViewLeave(int leaveEmployeeId, int departmentHeadEmployeeId, string status)
        {
            if (_auth.IsAdmin || _perms.CanRead(FormKey)) return true;
            if (CanActAsHr()) return true;
            if (LinkedEmployeeId > 0 && LinkedEmployeeId == leaveEmployeeId) return true;
            if (CanActAsDepartmentHead(departmentHeadEmployeeId)) return true;
            return false;
        }

        public bool CanApprove(string status, int departmentHeadEmployeeId)
        {
            if (status == LeaveApprovalStatuses.PendingDepartmentHead)
                return CanActAsDepartmentHead(departmentHeadEmployeeId);
            if (status == LeaveApprovalStatuses.PendingHR)
                return CanActAsHr();
            return false;
        }

        public int ResolveDepartmentHeadEmployeeId(int employeeId)
        {
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var deptHead = ScalarInt(conn, @"
SELECT ISNULL(d.DepartmentManagerID, 0)
FROM tblEmployee e
LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
WHERE e.EmployeeID = @EmployeeID;", employeeId);
                if (deptHead > 0) return deptHead;

                var jobHead = ScalarInt(conn, @"
SELECT ISNULL(j.ReportsToEmployeeID, 0)
FROM tblEmployee e
LEFT JOIN tblJob j ON j.JobID = e.JobID
WHERE e.EmployeeID = @EmployeeID;", employeeId);
                if (jobHead > 0) return jobHead;

                return ScalarInt(conn, @"
SELECT ISNULL(PermanentResponsibleEmployeeID, 0)
FROM tblEmployee WHERE EmployeeID = @EmployeeID;", employeeId);
            }
        }

        private static int ScalarInt(SqlConnection conn, string sql, int employeeId)
        {
            try
            {
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                    var o = cmd.ExecuteScalar();
                    return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
                }
            }
            catch
            {
                return 0;
            }
        }

        public void OnSubmitted(SqlConnection conn, int leaveId, int employeeId, int departmentHeadEmployeeId)
        {
            LogAction(conn, leaveId, LeaveApprovalStages.Applicant, "Submitted", null);
            var number = "LV-" + leaveId.ToString("D7");
            var emp = _inbox.EmployeeLabel(employeeId);
            var status = LeaveApprovalStatuses.DisplayName(LeaveApprovalStatuses.PendingDepartmentHead);
            var bodyPending = _inbox.FormatBody("Leave Management", number, emp, "Submitted", status, _inbox.ActorName());
            NotifyEmployeeUsers(conn, departmentHeadEmployeeId, leaveId,
                "Leave approval required",
                bodyPending);
            NotifyEmployeeUsers(conn, employeeId, leaveId,
                "Leave submitted",
                bodyPending);
        }

        public void Approve(int leaveId, string comments)
        {
            EnsureSchema();
            comments = (comments ?? "").Trim();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                string status;
                int employeeId;
                int dhId;
                LoadLeaveApprovalRow(conn, leaveId, out status, out employeeId, out dhId);

                if (status == LeaveApprovalStatuses.PendingDepartmentHead)
                {
                    if (!CanActAsDepartmentHead(dhId))
                        throw new InvalidOperationException("Only the Department Head can approve this leave request.");
                    UpdateStatus(conn, leaveId, LeaveApprovalStatuses.PendingHR);
                    LogAction(conn, leaveId, LeaveApprovalStages.DepartmentHead, "Approved", comments);
                    var number = "LV-" + leaveId.ToString("D7");
                    var emp = _inbox.EmployeeLabel(employeeId);
                    var statusDisplay = LeaveApprovalStatuses.DisplayName(LeaveApprovalStatuses.PendingHR);
                    var body = _inbox.FormatBody("Leave Management", number, emp, "Approved (Department Head)", statusDisplay, _inbox.ActorName());
                    NotifyHrUsers(conn, leaveId, number, body);
                    NotifyEmployeeUsers(conn, employeeId, leaveId, "Leave forwarded to HR", body);
                    NotifyCurrentActor("Leave forwarded to HR", body, leaveId);
                    return;
                }

                if (status == LeaveApprovalStatuses.PendingHR)
                {
                    if (!CanActAsHr())
                        throw new InvalidOperationException("Only the HR Manager can give final leave approval.");
                    UpdateStatus(conn, leaveId, LeaveApprovalStatuses.Approved);
                    LogAction(conn, leaveId, LeaveApprovalStages.HR, "Approved", comments);
                    var number = "LV-" + leaveId.ToString("D7");
                    var emp = _inbox.EmployeeLabel(employeeId);
                    var body = _inbox.FormatBody("Leave Management", number, emp, "Approved",
                        LeaveApprovalStatuses.DisplayName(LeaveApprovalStatuses.Approved), _inbox.ActorName());
                    NotifyEmployeeUsers(conn, employeeId, leaveId, "Leave approved", body);
                    NotifyCurrentActor("Leave approved", body, leaveId);
                    return;
                }

                throw new InvalidOperationException("This leave request is not waiting for approval.");
            }
        }

        public void Reject(int leaveId, string comments)
        {
            EnsureSchema();
            comments = (comments ?? "").Trim();
            if (string.IsNullOrEmpty(comments))
                throw new InvalidOperationException("Comments are required when rejecting a leave request.");

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                string status;
                int employeeId;
                int dhId;
                LoadLeaveApprovalRow(conn, leaveId, out status, out employeeId, out dhId);

                string stage;
                if (status == LeaveApprovalStatuses.PendingDepartmentHead)
                {
                    if (!CanActAsDepartmentHead(dhId))
                        throw new InvalidOperationException("Only the Department Head can reject this leave request.");
                    stage = LeaveApprovalStages.DepartmentHead;
                }
                else if (status == LeaveApprovalStatuses.PendingHR)
                {
                    if (!CanActAsHr())
                        throw new InvalidOperationException("Only the HR Manager can reject this leave request.");
                    stage = LeaveApprovalStages.HR;
                }
                else
                    throw new InvalidOperationException("This leave request is not waiting for approval.");

                UpdateStatus(conn, leaveId, LeaveApprovalStatuses.Rejected);
                LogAction(conn, leaveId, stage, "Rejected", comments);
                var number = "LV-" + leaveId.ToString("D7");
                var emp = _inbox.EmployeeLabel(employeeId);
                var body = _inbox.FormatBody("Leave Management", number, emp, "Rejected",
                    LeaveApprovalStatuses.DisplayName(LeaveApprovalStatuses.Rejected), _inbox.ActorName())
                    + (string.IsNullOrEmpty(comments) ? "" : "\nComments: " + comments);
                NotifyEmployeeUsers(conn, employeeId, leaveId, "Leave rejected", body);
                NotifyCurrentActor("Leave rejected", body, leaveId);
            }
        }

        public List<LeaveApprovalActionItem> GetHistory(int leaveId)
        {
            EnsureSchema();
            var list = new List<LeaveApprovalActionItem>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT a.ActionID, a.Stage, a.Action,
       COALESCE(
           NULLIF(LTRIM(RTRIM(ISNULL(e.FirstName,'') + N' ' + ISNULL(e.LastName,''))), ''),
           NULLIF(LTRIM(RTRIM(u.FullName)), ''),
           ISNULL(u.Username, N'System')
       ) AS ActorName,
       ISNULL(a.Comments, ''), a.ActionOn
FROM tblLeaveApprovalAction a
LEFT JOIN tblUser u ON u.UserID = a.ActorUserID
LEFT JOIN tblEmployee e ON e.EmployeeID = a.ActorEmployeeID
WHERE a.LeaveID = @LeaveID
ORDER BY a.ActionOn, a.ActionID;", conn))
            {
                cmd.Parameters.AddWithValue("@LeaveID", leaveId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new LeaveApprovalActionItem
                        {
                            ActionID = Convert.ToInt32(dr[0]),
                            Stage = dr.GetString(1),
                            Action = dr.GetString(2),
                            ActorName = dr.IsDBNull(3) ? "" : dr.GetString(3),
                            Comments = dr.IsDBNull(4) ? "" : dr.GetString(4),
                            ActionOn = Convert.ToDateTime(dr[5])
                        });
                    }
                }
            }
            return list;
        }

        public List<LeaveAlertItem> GetUnreadAlerts(int take = 10)
        {
            EnsureSchema();
            var list = new List<LeaveAlertItem>();
            var uid = _auth.CurrentUserId ?? 0;
            if (uid <= 0) return list;
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT TOP (@Take) AlertID, LeaveID, Title, ISNULL(Message,''), ISNULL(LinkUrl,''), CreatedOn
FROM tblLeaveAlert
WHERE UserID = @UserID AND IsRead = 0
ORDER BY CreatedOn DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@Take", take);
                cmd.Parameters.AddWithValue("@UserID", uid);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new LeaveAlertItem
                        {
                            AlertID = Convert.ToInt32(dr[0]),
                            LeaveID = Convert.ToInt32(dr[1]),
                            Title = dr.GetString(2),
                            Message = dr.GetString(3),
                            LinkUrl = dr.GetString(4),
                            CreatedOn = Convert.ToDateTime(dr[5])
                        });
                    }
                }
            }
            return list;
        }

        public void MarkAlertsRead(int leaveId)
        {
            var uid = _auth.CurrentUserId ?? 0;
            if (uid <= 0 || leaveId <= 0) return;
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
UPDATE tblLeaveAlert SET IsRead = 1 WHERE UserID = @UserID AND LeaveID = @LeaveID AND IsRead = 0;", conn))
            {
                cmd.Parameters.AddWithValue("@UserID", uid);
                cmd.Parameters.AddWithValue("@LeaveID", leaveId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public string StatusSqlFilter(string inbox, int linkedEmployeeId, bool isHr)
        {
            if (string.IsNullOrWhiteSpace(inbox) || inbox.Equals("all", StringComparison.OrdinalIgnoreCase))
                return "";
            if (inbox.Equals("mine", StringComparison.OrdinalIgnoreCase))
                return " AND l.EmployeeID = " + linkedEmployeeId + " ";
            if (inbox.Equals("departmentHead", StringComparison.OrdinalIgnoreCase))
                return " AND l.DepartmentHeadEmployeeID = " + linkedEmployeeId
                     + " AND l.Status = N'" + LeaveApprovalStatuses.PendingDepartmentHead + "' ";
            if (inbox.Equals("hr", StringComparison.OrdinalIgnoreCase) && isHr)
                return " AND l.Status = N'" + LeaveApprovalStatuses.PendingHR + "' ";
            return "";
        }

        private void LoadLeaveApprovalRow(SqlConnection conn, int leaveId, out string status, out int employeeId, out int dhId)
        {
            using (var cmd = new SqlCommand(@"
SELECT ISNULL(Status, N'Approved'), EmployeeID, ISNULL(DepartmentHeadEmployeeID, 0)
FROM tblLeaveApplication WHERE LeaveID = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", leaveId);
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read())
                        throw new InvalidOperationException("Leave application was not found.");
                    status = dr.GetString(0);
                    employeeId = Convert.ToInt32(dr[1]);
                    dhId = Convert.ToInt32(dr[2]);
                }
            }
        }

        private static void UpdateStatus(SqlConnection conn, int leaveId, string status)
        {
            using (var cmd = new SqlCommand(@"
UPDATE tblLeaveApplication SET Status = @Status, ModifiedOn = GETDATE() WHERE LeaveID = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@Id", leaveId);
                cmd.ExecuteNonQuery();
            }
        }

        private void LogAction(SqlConnection conn, int leaveId, string stage, string action, string comments)
        {
            using (var cmd = new SqlCommand(@"
INSERT INTO tblLeaveApprovalAction (LeaveID, Stage, Action, ActorUserID, ActorEmployeeID, Comments, ActionOn)
VALUES (@LeaveID, @Stage, @Action, @UserID, @EmployeeID, @Comments, GETDATE());", conn))
            {
                cmd.Parameters.AddWithValue("@LeaveID", leaveId);
                cmd.Parameters.AddWithValue("@Stage", stage);
                cmd.Parameters.AddWithValue("@Action", action);
                cmd.Parameters.AddWithValue("@UserID", _auth.CurrentUserId.HasValue ? (object)_auth.CurrentUserId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@EmployeeID", LinkedEmployeeId > 0 ? (object)LinkedEmployeeId : DBNull.Value);
                cmd.Parameters.AddWithValue("@Comments", string.IsNullOrWhiteSpace(comments) ? (object)DBNull.Value : comments);
                cmd.ExecuteNonQuery();
            }
        }

        private void NotifyEmployeeUsers(SqlConnection conn, int employeeId, int leaveId, string title, string message)
        {
            if (employeeId <= 0) return;
            foreach (var uid in GetUserIdsForEmployee(conn, employeeId))
                InsertAlert(conn, uid, leaveId, title, message);
        }

        private void NotifyHrUsers(SqlConnection conn, int leaveId, string number, string body)
        {
            foreach (var uid in GetUsersWithFormRight(conn, HrFormKey))
                InsertAlert(conn, uid, leaveId, "HR leave approval required", body);
        }

        private void NotifyCurrentActor(string title, string body, int leaveId)
        {
            if (!_auth.CurrentUserId.HasValue) return;
            _inbox.Notify(_auth.CurrentUserId.Value, title, body, "/LeaveMaster.aspx?editId=" + leaveId,
                AppInboxService.TypeLeave, leaveId);
        }

        private void InsertAlert(SqlConnection conn, int userId, int leaveId, string title, string message)
        {
            using (var cmd = new SqlCommand(@"
INSERT INTO tblLeaveAlert (UserID, LeaveID, Title, Message, LinkUrl, IsRead, CreatedOn)
VALUES (@UserID, @LeaveID, @Title, @Message, @LinkUrl, 0, GETDATE());", conn))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@LeaveID", leaveId);
                cmd.Parameters.AddWithValue("@Title", title);
                cmd.Parameters.AddWithValue("@Message", message ?? "");
                cmd.Parameters.AddWithValue("@LinkUrl", "/LeaveMaster.aspx?editId=" + leaveId);
                cmd.ExecuteNonQuery();
            }
            _inbox.Notify(userId, title, message, "/LeaveMaster.aspx?editId=" + leaveId, AppInboxService.TypeLeave, leaveId);
        }

        private static List<int> GetUserIdsForEmployee(SqlConnection conn, int employeeId)
        {
            var ids = new List<int>();
            using (var cmd = new SqlCommand("SELECT UserID FROM tblEmployee WHERE EmployeeID=@Id AND ISNULL(UserID,0)>0;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", employeeId);
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        if (!dr.IsDBNull(0)) ids.Add(Convert.ToInt32(dr[0]));
            }
            return ids;
        }

        private static List<int> GetUsersWithFormRight(SqlConnection conn, string formKey)
        {
            var ids = new List<int>();
            using (var cmd = new SqlCommand(@"
SELECT DISTINCT u.UserID
FROM tblUser u
LEFT JOIN tblUserPermission p ON p.UserID = u.UserID AND p.FormKey = @FormKey
WHERE u.IsActive = 1 AND (u.IsAdmin = 1 OR ISNULL(p.CanRead,0)=1 OR ISNULL(p.CanWrite,0)=1);", conn))
            {
                cmd.Parameters.AddWithValue("@FormKey", formKey);
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        ids.Add(Convert.ToInt32(dr[0]));
            }
            return ids;
        }
    }
}
